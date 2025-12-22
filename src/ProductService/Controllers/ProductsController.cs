using Common.Contracts.Products;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application;

namespace ProductService.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductsController(
    IProductQueries queries,
    IProductCommands commands,
    ILogger<ProductsController> logger
    ) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        logger.LogInformation("Получить товар с id {ProductId}", id);
        var product = await queries.GetByIdAsync(id, ct);
        if (product is null)
        {
            logger.LogWarning("Товар {ProductId} не найден", id);
            return NotFound();
        }

        return Ok(product);
    }

    [HttpGet]
    public async Task<IActionResult> GetByIds([FromQuery] Guid[] ids, CancellationToken ct)
    {
        logger.LogInformation(
            "Найдены товары по id. Count={IdsCount}",
            ids.Length
        );
        if (ids.Length == 0)
        {
            logger.LogDebug("Товары не найдены. Возвращен пустой список");
            return Ok(Array.Empty<ProductDto>());
        }
        var products = await queries.GetByIdsAsync(ids, ct);
        logger.LogInformation(
            "Найдено {ProductsCount} товаров по id",
            products.Count()
        );
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        logger.LogInformation(
            "Товар создается. Name={Name}, Category={Category}",
            command.Name,
            command.Category
        );
        var created = await commands.CreateAsync(command, ct);
        logger.LogInformation(
            "ТОвар создан. ProductId={ProductId}, Name={Name}",
            created.Id,
            created.Name
        );
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}