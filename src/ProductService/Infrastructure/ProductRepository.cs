using Microsoft.EntityFrameworkCore;
using ProductService.Domain;

namespace ProductService.Infrastructure;

public class ProductRepository(ProductDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct)
    {
        return db.Products.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct)
    {
        return await db.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);
    }

    public Task AddAsync(Product product, CancellationToken ct)
    {
        db.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }
}