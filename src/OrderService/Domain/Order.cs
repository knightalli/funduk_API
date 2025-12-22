namespace OrderService.Domain;

public readonly record struct OrderId(Guid Value);

public readonly record struct UserId(Guid Value);

public readonly record struct ProductId(Guid Value);

public record OrderItem(ProductId ProductId, int Quantity);

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Order(OrderId id, UserId userId, IEnumerable<OrderItem> items)
    {
        Id = id;
        UserId = userId;
        _items = items.ToList();
    }

    public OrderId Id { get; private set; }
    public UserId UserId { get; private set; }

    public IReadOnlyCollection<OrderItem> OrderItems => _items;

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
    }

    public void RemoveItem(OrderItem item)
    {
        _items.Remove(item);
    }
}