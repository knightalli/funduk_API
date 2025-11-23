using ProductService.Domain;

namespace ProductService.Infrastructure;

public class ProductRepository : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Product product, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
