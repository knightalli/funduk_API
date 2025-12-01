using OrderService.Domain;

namespace OrderService.Infrastructure;

public class OrderRepository : IOrderRepository
{
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task AddOrderItemAsync(Order order, OrderItem orderItem, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RemoveOrderItemAsync(Order order, OrderItem orderItem, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}