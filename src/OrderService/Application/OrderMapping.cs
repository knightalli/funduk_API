using Common.Contracts.Orders;
using OrderService.Domain;

namespace OrderService.Application;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto(
            order.Id.Value,
            new Common.Contracts.Orders.UserId(order.UserId.Value),
            order.OrderItems
                .Select(i => new Common.Contracts.Orders.OrderItem(
                    new Common.Contracts.Orders.ProductId(i.ProductId.Value),
                    i.Quantity))
                .ToList()
        );
    }
}