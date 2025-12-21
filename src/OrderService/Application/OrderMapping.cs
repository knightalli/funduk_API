using Common.Contracts.Orders;
using OrderService.Domain;
using OrderItem = Common.Contracts.Orders.OrderItem;
using ProductId = Common.Contracts.Orders.ProductId;
using UserId = Common.Contracts.Orders.UserId;

namespace OrderService.Application;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto(
            order.Id.Value,
            new UserId(order.UserId.Value),
            order.OrderItems
                .Select(i => new OrderItem(
                    new ProductId(i.ProductId.Value),
                    i.Quantity))
                .ToList()
        );
    }
}