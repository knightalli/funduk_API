using Common.Contracts;

namespace ProductService.Domain;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
}
