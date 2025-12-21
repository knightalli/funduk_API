namespace ProductService.Domain;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}