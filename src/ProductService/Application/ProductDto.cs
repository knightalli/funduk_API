namespace ProductService.Application;

public sealed record ProductDto(Guid Id, string Name, string? Category, decimal Price);