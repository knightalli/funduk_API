using Common.Contracts.Users;
using UserService.Domain;

namespace UserService.Application;

public interface IUserQueries
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);
}

public sealed class UserQueries(IUserRepository repository) : IUserQueries
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(new UserId(id), ct);
        return user?.ToDto();
    }
}