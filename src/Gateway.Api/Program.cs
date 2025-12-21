using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Common.Contracts.Gateway;
using Gateway.Api.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
});

builder.Services.AddHttpClient<OrderServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:Orders"]!);
    c.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddHttpClient<ProductServiceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:Products"]!);
    c.Timeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();

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
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var tokenUserId) || tokenUserId != userId)
            return Results.Forbid();

        var cacheKey = $"profile:{userId}";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            var cachedResponse = JsonSerializer.Deserialize<ProfileResponseDto>(cached)!;
            return Results.Ok(cachedResponse);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));
        var token = timeoutCts.Token;

        var userTask = users.GetByIdAsync(userId, token);
        var ordersTask = orders.GetByUserIdAsync(userId, token);

        await Task.WhenAll(userTask, ordersTask);

        var user = await userTask;
        if (user is null)
            return Results.NotFound(new ProblemDetails { Title = "User not found", Status = 404 });

        var userOrders = await ordersTask;

        var productIds = userOrders
            .SelectMany(o => o.OrderItems)
            .Select(i => i.ProductId.Value)
            .Distinct()
            .ToArray();

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

        return Results.Ok(response);
    })
    .WithName("GetProfile")
    .RequireAuthorization()
    .RequireRateLimiting("profile")
    .Produces<ProfileResponseDto>()
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();
