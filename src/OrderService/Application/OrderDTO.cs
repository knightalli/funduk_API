using OrderService.Domain;
using UserService.Domain;

namespace OrderService.Application;

public sealed record OrderDTO(Guid Id, UserId UserId, IReadOnlyCollection<OrderItem> OrderItems);