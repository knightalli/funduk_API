namespace OrderService.Domain;

public readonly record struct OrderId(Guid Value);
public readonly record struct UserId(Guid Value);
public readonly record struct ProductId(Guid Value);
public record OrderItem(ProductId ProductId, int Quantity);

public sealed class Order(OrderId id, UserId userId, IReadOnlyCollection<OrderItem> orderItems)
{
    private Order() : this(default, default, Array.Empty<OrderItem>()) { }
    public OrderId Id { get; private set; } = id;

    public UserId UserId { get; private set; } = userId;

    public IReadOnlyCollection<OrderItem> OrderItems { get; private set; } = orderItems;
}

