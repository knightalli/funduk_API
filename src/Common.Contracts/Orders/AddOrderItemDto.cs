namespace Common.Contracts.Orders;

public sealed record AddOrderItemDto(Guid ProductId, int Quantity);