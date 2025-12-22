using Common.Contracts.Users;
using Microsoft.AspNetCore.Mvc;
using UserService.Application;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserQueries queries, IUserCommands commands, ILogger<UsersController> logger)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        logger.LogInformation(
            "Получить пользователя с id {UserId}",
            id
        );

        var user = await queries.GetByIdAsync(id, ct);

        if (user is null)
        {
            logger.LogInformation(
                "Пользователь не найден. UserId={UserId}",
                id
            );
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var created = await commands.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}