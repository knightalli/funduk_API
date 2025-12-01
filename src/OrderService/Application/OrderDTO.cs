using Common.Contracts;
using OrderService.Domain;

namespace OrderService.Application;

public sealed record OrderDTO(Guid Id, UserId UserId, IReadOnlyCollection<OrderItem> OrderItems);