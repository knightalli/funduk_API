using Common.Contracts.Products;
using ProductService.Domain;

namespace ProductService.Application;

public interface IProductQueries
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ProductDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
}

public sealed class ProductQueries(IProductRepository repository) : IProductQueries
{
    private readonly IProductRepository _repository = repository;

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(new ProductId(id), ct);
        return product?.ToDto();
    }

    public async Task<IReadOnlyList<ProductDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var productIds = ids.Select(x => new ProductId(x)).ToArray();
        var products = await _repository.GetByIdsAsync(productIds, ct);
        return [.. products.Select(p => p.ToDto())];
    }
}
