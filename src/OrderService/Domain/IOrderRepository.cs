namespace OrderService.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken);
    Task AddAsync(Order order);
    Task AddOrderItemAsync(Order order, OrderItem orderItem);
    Task RemoveOrderItemAsync(Order order, OrderItem orderItem);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}