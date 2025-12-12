using Common.Contracts.Gateway;
using Gateway.Api.Clients;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/profile/{userId:guid}", async (
    Guid userId,
    UserServiceClient users,
    OrderServiceClient orders,
    ProductServiceClient products,
    CancellationToken ct) =>
{
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
        [.. userOrders.Select(o => new ProfileOrderDto(
            o.Id,
            [.. o.OrderItems.Select(oi =>
            {
                var pid = oi.ProductId.Value;
                productById.TryGetValue(pid, out var p);

                return new ProfileOrderItemDto(
                    pid,
                    oi.Quantity,
                    p is null ? null : new ProfileProductDto(p.Id, p.Name, p.Category, p.Price)
                );
            })]
        ))]
    );

    return Results.Ok(response);
})
.WithName("GetProfile")
.Produces<ProfileResponseDto>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();
