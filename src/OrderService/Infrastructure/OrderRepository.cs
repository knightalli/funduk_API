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

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Order order)
    {
        _db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task AddOrderItemAsync(Order order, OrderItem orderItem)
    {
        order.AddItem(orderItem);
        return Task.CompletedTask;
    }

    public Task RemoveOrderItemAsync(Order order, OrderItem orderItem)
    {
        order.RemoveItem(orderItem);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}