using OrderService.Domain;
using OrderDto = Common.Contracts.Orders.OrderDto;

namespace OrderService.Application;

public interface IOrderQueries
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class OrderQueries(IOrderRepository repository) : IOrderQueries
{
    private readonly IOrderRepository _repository = repository;

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(new OrderId(id), cancellationToken);
        return order?.ToDto();
    }

    public async Task<IReadOnlyList<OrderDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetByUserIdAsync(new UserId(userId), cancellationToken);
        return [.. orders.Select(o => o.ToDto())];
    }
}