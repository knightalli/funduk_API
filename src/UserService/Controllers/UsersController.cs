using Microsoft.AspNetCore.Mvc;
using UserService.Application;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserQueries queries) : ControllerBase
{
    private readonly IUserQueries _queries = queries;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _queries.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }
}
