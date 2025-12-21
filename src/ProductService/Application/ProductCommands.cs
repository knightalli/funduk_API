using Common.Contracts.Products;
using ProductService.Domain;

namespace ProductService.Application;

public sealed record CreateProductCommand(string Name, string? Category, decimal Price);

public interface IProductCommands
{
    Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken ct);
}

public sealed class ProductCommands(IProductRepository repository) : IProductCommands
{
    public async Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken ct)
    {
        var product = new Product(new ProductId(Guid.NewGuid()), command.Name, command.Category, command.Price);

        await repository.AddAsync(product, ct);
        await repository.SaveChangesAsync(ct);

        return product.ToDto();
    }
}