using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Common.Contracts.Gateway;
using Common.Contracts.Orders;
using Common.Contracts.Products;
using Common.Contracts.Users;
using Gateway.Api.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gateway API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "gateway:";
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
    })
    .AddPolicyHandler(FallbackPolicy())
    .AddPolicyHandler(RetryPolicy())
    .AddPolicyHandler(CircuitBreakerPolicy());

builder.Services.AddHttpClient<OrderServiceClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Services:Orders"]!);
        c.Timeout = TimeSpan.FromSeconds(2);
    })
    .AddPolicyHandler(FallbackPolicy())
    .AddPolicyHandler(RetryPolicy())
    .AddPolicyHandler(CircuitBreakerPolicy());

builder.Services.AddHttpClient<ProductServiceClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Services:Products"]!);
        c.Timeout = TimeSpan.FromSeconds(2);
    })
    .AddPolicyHandler(FallbackPolicy())
    .AddPolicyHandler(RetryPolicy())
    .AddPolicyHandler(CircuitBreakerPolicy());

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
            return Results.Ok(JsonSerializer.Deserialize<ProfileResponseDto>(cached)!);
        }

        Log.Information("Не найден кэш для профиля {UserId}", userId);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));
        var token = timeoutCts.Token;

        UserDto? user;
        try
        {
            user = await users.GetByIdAsync(userId, token);
        }
        catch (Exception ex) when (IsDownstreamUnavailable(ex))
        {
            Log.Warning(ex, "UserService недоступен для userId={UserId}", userId);
            return Results.Problem(
                title: "User service unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (user is null)
        {
            Log.Warning("User {UserId} не найден", userId);
            return Results.NotFound(new ProblemDetails { Title = "User not found", Status = 404 });
        }

        IReadOnlyList<OrderDto> userOrders;
        try
        {
            userOrders = await orders.GetByUserIdAsync(userId, token);
        }
        catch (Exception ex) when (IsDownstreamUnavailable(ex))
        {
            Log.Warning(ex, "OrderService недоступен для userId={UserId}. Fallback: []", userId);
            userOrders = [];
        }

        var productIds = userOrders
            .SelectMany(o => o.OrderItems)
            .Select(i => i.ProductId.Value)
            .Distinct()
            .ToArray();

        Log.Information("Найдено {Count} продуктов для пользователя {UserId}", productIds.Length, userId);

        IReadOnlyList<ProductDto> productList;
        if (productIds.Length == 0)
        {
            productList = [];
        }
        else
        {
            try
            {
                productList = await products.GetByIdsAsync(productIds, token);
            }
            catch (Exception ex) when (IsDownstreamUnavailable(ex))
            {
                Log.Warning(ex, "ProductService недоступен. Fallback: []");
                productList = [];
            }
        }

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
    .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();
return;

static IAsyncPolicy<HttpResponseMessage> FallbackPolicy() =>
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .Or<TimeoutException>()
        .Or<Polly.CircuitBreaker.BrokenCircuitException>()
        .Or<Polly.CircuitBreaker.BrokenCircuitException<HttpResponseMessage>>()
        .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
        .FallbackAsync(
            fallbackAction: static ct =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            onFallbackAsync: static outcome =>
            {
                Log.Error(
                    outcome.Exception,
                    "Fallback выполнен. Возвращаем 503. Reason={Reason}",
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                );

                return Task.CompletedTask;
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
            onReset: () => { Log.Information("Circuit breaker перезагружен"); });

static bool IsDownstreamUnavailable(Exception ex) =>
    ex is Polly.CircuitBreaker.BrokenCircuitException
        or Polly.CircuitBreaker.BrokenCircuitException<HttpResponseMessage>
        or HttpRequestException
        or TaskCanceledException
        or TimeoutException;

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