using OrderService.Domain;

namespace OrderService.Application;

public sealed record OrderDto(Guid Id, UserId UserId, IReadOnlyCollection<OrderItem> OrderItems);