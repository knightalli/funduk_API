using ProductService.Domain;
using UserService.Domain;

namespace OrderService.Domain;

public class Order(OrderId id, UserId userId, IReadOnlyCollection<OrderItem> orderItems)
{
    public OrderId Id { get; private set; } = id;

    public UserId UserId { get; private set; } = userId;

    public IReadOnlyCollection<OrderItem> OrderItems { get; private set; } = orderItems;
}

public record OrderItem(ProductId ProductId, int Quantity);