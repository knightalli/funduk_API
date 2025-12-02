using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Infrastructure;

public class OrderRepository(OrderDbContext db) : IOrderRepository
{
    private readonly OrderDbContext _db = db;
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        return _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public Task AddAsync(Order order)
    {
        db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task AddOrderItem(Order order, OrderItem orderItem)
    {
        var items = (List<OrderItem>)order.OrderItems;
        items.Add(orderItem);
        return Task.CompletedTask;
    }

    public Task RemoveOrderItem(Order order, OrderItem orderItem)
    {
        var items = (List<OrderItem>)order.OrderItems;
        items.Remove(orderItem);
        return Task.CompletedTask;
    }
}