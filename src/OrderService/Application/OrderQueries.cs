using Common.Contracts.Orders;
using OrderService.Domain;

namespace OrderService.Application;

public interface IOrderQueries
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class OrderQueries(IOrderRepository repository) : IOrderQueries
{
    private readonly IOrderRepository _repository = repository;

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(new OrderId(id), cancellationToken);
        return order?.ToDto();
    }
}