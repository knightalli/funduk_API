namespace OrderService.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken);
    Task AddAsync(Order order);
    Task AddOrderItem(Order order, OrderItem orderItem);
    Task RemoveOrderItem(Order order, OrderItem orderItem);
}