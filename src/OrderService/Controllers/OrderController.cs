using Common.Contracts.Orders;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application;
using ProductId = OrderService.Domain.ProductId;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(
    IOrderQueries queries,
    IOrderCommands commands,
    ILogger<OrdersController> logger
) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        logger.LogInformation("Поиск заказа с id {OrderId}", id);
        var order = await queries.GetByIdAsync(id, ct);
        if (order is null)
        {
            logger.LogWarning("Заказ {OrderId} не найден", id);
            return NotFound();
        }

        return Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserId([FromQuery] Guid userId, CancellationToken ct)
    {
        logger.LogInformation("Поиск заказов для пользователя {UserId}", userId);
        var orders = await queries.GetByUserIdAsync(userId, ct);
        logger.LogInformation(
            "Найдено {OrdersCount} заказов(а) для пользователя {UserId}",
            orders.Count(),
            userId
        );
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct)
    {
        logger.LogInformation(
            "Создаем заказ для пользователя {UserId}",
            command.UserId
        );
        var created = await commands.CreateAsync(command, ct);
        logger.LogInformation(
            "Заказ {OrderId} создан для пользователя {UserId}",
            created.Id,
            command.UserId
        );
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/items/add")]
    public async Task<IActionResult> AddItem(
        Guid id,
        [FromBody] AddOrderItemDto itemDto,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Добавить товар к заказу {OrderId}. ProductId={ProductId}, Quantity={Quantity}",
            id,
            itemDto.ProductId,
            itemDto.Quantity
        );
        await commands.AddOrderItemAsync(id, new ProductId(itemDto.ProductId), itemDto.Quantity, ct);
        logger.LogInformation(
            "Товар добавлен к заказу {OrderId}. ProductId={ProductId}, Quantity={Quantity}",
            id,
            itemDto.ProductId,
            itemDto.Quantity
        );
        return NoContent();
    }

    [HttpPatch("{id:guid}/items/remove")]
    public async Task<IActionResult> RemoveItem(
        Guid id,
        [FromBody] RemoveOrderItemDto itemDto,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Удалить товар из заказа {OrderId}. ProductId={ProductId}",
            id,
            itemDto.ProductId
        );
        await commands.RemoveOrderItemAsync(id, new ProductId(itemDto.ProductId), ct);
        logger.LogInformation(
            "Товар удален из заказа {OrderId}. ProductId={ProductId}",
            id,
            itemDto.ProductId
        );
        return NoContent();
    }
}