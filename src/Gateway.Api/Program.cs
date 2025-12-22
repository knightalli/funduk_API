using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Net;
using Common.Contracts.Gateway;
using Gateway.Api.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using Polly;
using Polly.Extensions.Http;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

Log.Information("Gateway запускается");

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "gateway:";
});

static IAsyncPolicy<HttpResponseMessage> RetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retry => TimeSpan.FromMilliseconds(200 * retry),
            onRetry: (outcome, delay, retry, _) =>
            {
                Log.Warning(
                    "Повтор {Retry} после {Delay}мс из-за {Reason}",
                    retry,
                    delay.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                );
            });

static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                Log.Error(
                    "Circuit breaker начат на {Seconds}с из-за {Reason}",
                    breakDelay.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                );
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker перезагружен");
            });

static IAsyncPolicy<HttpResponseMessage> FallbackPolicy() =>
    Policy<HttpResponseMessage>
        .Handle<Exception>()
        .OrResult(r => !r.IsSuccessStatusCode)
        .FallbackAsync(
            fallbackAction: _ =>
            {
                Log.Error("Fallback выполнен. Возвращаем 503");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            });


builder.Services.AddOpenTelemetry()
    .WithMetrics(mb =>
    {
        mb
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtKey))
{
    Log.Fatal("JWT конфигурация потеряна");
    throw new InvalidOperationException("JWT config is missing. Please set Jwt:Issuer, Jwt:Audience, Jwt:Key.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.SaveToken = false;

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("profile", context =>
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddHttpClient<UserServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:User"]!);
    c.Timeout = TimeSpan.FromSeconds(2);
}).AddPolicyHandler(RetryPolicy()).AddPolicyHandler(CircuitBreakerPolicy()).AddPolicyHandler(FallbackPolicy());

builder.Services.AddHttpClient<OrderServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:Orders"]!);
    c.Timeout = TimeSpan.FromSeconds(2);
}).AddPolicyHandler(RetryPolicy()).AddPolicyHandler(CircuitBreakerPolicy()).AddPolicyHandler(FallbackPolicy());;

builder.Services.AddHttpClient<ProductServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:Products"]!);
    c.Timeout = TimeSpan.FromSeconds(2);
}).AddPolicyHandler(RetryPolicy()).AddPolicyHandler(CircuitBreakerPolicy()).AddPolicyHandler(FallbackPolicy());;

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint("/metrics");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/profile/{userId:guid}", async (
        Guid userId,
        ClaimsPrincipal principal,
        UserServiceClient users,
        OrderServiceClient orders,
        ProductServiceClient products,
        IDistributedCache cache,
        CancellationToken ct) =>
    {
        Log.Information("Запрос на профиль пользователя. UserId={UserId}", userId);
        
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var tokenUserId) || tokenUserId != userId)
        {
            Log.Warning(
                "Запрещен доступ к профилю. TokenUserId={TokenUserId}, RequestedUserId={UserId}",
                subject,
                userId
            );
            return Results.Forbid();
        }

        var cacheKey = $"profile:{userId}";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            Log.Information("Найден кэш для профиля {UserId}", userId);
            var cachedResponse = JsonSerializer.Deserialize<ProfileResponseDto>(cached)!;
            return Results.Ok(cachedResponse);
        }
        
        Log.Information("Не найден кэш для профиля {UserId}", userId);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));
        var token = timeoutCts.Token;

        var userTask = users.GetByIdAsync(userId, token);
        var ordersTask = orders.GetByUserIdAsync(userId, token);

        await Task.WhenAll(userTask, ordersTask);

        var user = await userTask;
        if (user is null)
        {
            Log.Warning("User {UserId} не найден", userId);
            return Results.NotFound(new ProblemDetails { Title = "User not found", Status = 404 }); 
        }

        var userOrders = await ordersTask;

        var productIds = userOrders
            .SelectMany(o => o.OrderItems)
            .Select(i => i.ProductId.Value)
            .Distinct()
            .ToArray();
        
        Log.Information("Найдено {Count} продуктов для пользователя {UserId}", productIds.Length, userId);

        var productList = productIds.Length == 0
            ? []
            : await products.GetByIdsAsync(productIds, token);

        var productById = productList.ToDictionary(p => p.Id, p => p);

        var response = new ProfileResponseDto(
            new ProfileUserDto(user.Id, user.FirstName, user.LastName, user.Email),
            [
                .. userOrders.Select(o => new ProfileOrderDto(
                    o.Id,
                    [
                        .. o.OrderItems.Select(oi =>
                        {
                            var pid = oi.ProductId.Value;
                            productById.TryGetValue(pid, out var p);

                            return new ProfileOrderItemDto(
                                pid,
                                oi.Quantity,
                                p is null ? null : new ProfileProductDto(p.Id, p.Name, p.Category, p.Price)
                            );
                        })
                    ]
                ))
            ]
        );

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            },
            ct
        );

        Log.Information("Профиль успешно открыт для пользователя {UserId}", userId);
        
        return Results.Ok(response);
    })
    .WithName("GetProfile")
    .RequireAuthorization()
    .RequireRateLimiting("profile")
    .Produces<ProfileResponseDto>()
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();
