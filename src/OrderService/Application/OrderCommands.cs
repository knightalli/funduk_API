using Common.Contracts.Orders;
using OrderService.Domain;
using OrderItem = OrderService.Domain.OrderItem;
using ProductId = OrderService.Domain.ProductId;
using UserId = OrderService.Domain.UserId;

namespace OrderService.Application;

public sealed record CreateOrderCommand(Guid UserId);

public interface IOrderCommands
{
    Task<OrderDto> CreateAsync(CreateOrderCommand command, CancellationToken ct);
    
    Task AddOrderItemAsync(Guid orderId, ProductId productId, int quantity, CancellationToken ct);
    Task RemoveOrderItemAsync(Guid orderId, ProductId productId, CancellationToken ct);
}

public sealed class OrderCommands(IOrderRepository repository) : IOrderCommands
{
    private readonly IOrderRepository _repository = repository;

    public async Task<OrderDto> CreateAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = new Order(
            new OrderId(Guid.NewGuid()),
            new UserId(command.UserId),
            []
        );

        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync(ct);

        return order.ToDto();
    }

    public async Task AddOrderItemAsync(Guid orderId, ProductId productId, int quantity, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(new OrderId(orderId), ct)
                    ?? throw new Exception("Order not found");

        var item = new OrderItem(productId, quantity);

        await _repository.AddOrderItemAsync(order, item);

        await _repository.SaveChangesAsync(ct);
    }

    public async Task RemoveOrderItemAsync(Guid orderId, ProductId productId, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(new OrderId(orderId), ct)
                    ?? throw new Exception("Order not found");

        var item = order.OrderItems.FirstOrDefault(i => i.ProductId == productId)
                   ?? throw new Exception("Item not found");

        await _repository.RemoveOrderItemAsync(order, item);

        await _repository.SaveChangesAsync(ct);
    }
}
