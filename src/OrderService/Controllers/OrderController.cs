using Common.Contracts.Orders;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application;
using ProductId = OrderService.Domain.ProductId;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(
    IOrderQueries queries,
    IOrderCommands commands
) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await queries.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserId([FromQuery] Guid userId, CancellationToken ct)
    {
        var orders = await queries.GetByUserIdAsync(userId, ct);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
    {
        var created = await commands.CreateAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/items/add")]
    public async Task<IActionResult> AddItem(
        Guid id,
        [FromBody] AddOrderItemDto itemDto,
        CancellationToken ct)
    {
        await commands.AddOrderItemAsync(id, new ProductId(itemDto.ProductId), itemDto.Quantity, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/items/remove")]
    public async Task<IActionResult> RemoveItem(
        Guid id,
        [FromBody] RemoveOrderItemDto itemDto,
        CancellationToken ct)
    {
        await commands.RemoveOrderItemAsync(id, new ProductId(itemDto.ProductId), ct);
        return NoContent();
    }
}