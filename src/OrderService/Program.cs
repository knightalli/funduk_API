using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OrderService.Application;
using OrderService.Domain;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .WithMetrics(mb =>
    {
        mb.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    });

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderQueries, OrderQueries>();
builder.Services.AddScoped<IOrderCommands, OrderCommands>();

builder.Services.AddAuthorization();

var app = builder.Build();

await EnsureDatabaseAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

await app.RunAsync();
return;

static async Task EnsureDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInit");

    var ct = app.Lifetime.ApplicationStopping;
    const int maxAttempts = 15;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(ct);
            if (!canConnect)
                throw new InvalidOperationException("Cannot connect to database.");

            await db.Database.EnsureCreatedAsync(ct);

            logger.LogInformation("OrderService database schema is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "OrderService DB init failed. Attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    logger.LogError("OrderService DB init failed after {MaxAttempts} attempts.", maxAttempts);
    await db.Database.EnsureCreatedAsync(ct);
}
