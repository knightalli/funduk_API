using Common.Contracts.Products;
using ProductService.Domain;

namespace ProductService.Application;

public static class ProductMappings
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(product.Id.Value, product.Name, product.Category, product.Price);
    }
}