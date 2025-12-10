using Common.Contracts.Products;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application;

namespace ProductService.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductsController(IProductQueries queries, IProductCommands commands) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await queries.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet]
    public async Task<IActionResult> GetByIds([FromQuery] Guid[] ids, CancellationToken ct)
    {
        if (ids.Length == 0) return Ok(Array.Empty<ProductDto>());
        var products = await queries.GetByIdsAsync(ids, ct);
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var created = await commands.CreateAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
