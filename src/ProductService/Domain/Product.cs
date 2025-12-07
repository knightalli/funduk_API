namespace ProductService.Domain;

public class Product(ProductId id, string name, string? category, decimal price)
{
    public ProductId Id { get; private set; } = id;

    public string Name { get; private set; } = name;

    public string? Category { get; private set; } = category;

    public decimal Price { get; private set; } = price;
}

public readonly record struct ProductId(Guid Value);