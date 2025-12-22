using Microsoft.AspNetCore.Mvc;
using UserService.Application;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserQueries queries, ILogger<UsersController> logger) : ControllerBase
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
}