using Microsoft.AspNetCore.Mvc;
using UserService.Application;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserQueries queries) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await queries.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }
}