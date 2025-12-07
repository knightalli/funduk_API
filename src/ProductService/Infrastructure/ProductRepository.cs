using Microsoft.EntityFrameworkCore;
using ProductService.Domain;

namespace ProductService.Infrastructure;

public class ProductRepository(ProductDbContext db) : IProductRepository
{
    private readonly ProductDbContext _db = db;

    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct)
    {
        return _db.Products.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task AddAsync(Product product, CancellationToken ct)
    {
        _db.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}
