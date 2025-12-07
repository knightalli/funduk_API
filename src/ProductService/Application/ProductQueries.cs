using Common.Contracts.Products;
using ProductService.Domain;

namespace ProductService.Application;

public interface IProductQueries
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct);
}

public sealed class ProductQueries(IProductRepository repository) : IProductQueries
{
    private readonly IProductRepository _repository = repository;

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(new ProductId(id), ct);
        return product?.ToDto();
    }
}
