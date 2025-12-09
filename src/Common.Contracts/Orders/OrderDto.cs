namespace Common.Contracts.Orders;

public readonly record struct UserId(Guid Value);
public readonly record struct ProductId(Guid Value);
public record OrderItem(ProductId ProductId, int Quantity);

public sealed record OrderDto(Guid Id, UserId UserId, IEnumerable<OrderItem> OrderItems);