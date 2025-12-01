namespace OrderService.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task AddOrderItemAsync(Order order, OrderItem orderItem, CancellationToken cancellationToken);
    Task RemoveOrderItemAsync(Order order, OrderItem orderItem, CancellationToken cancellationToken);
}