using Common.Contracts.Users;
using UserService.Domain;

namespace UserService.Application;

public interface IUserQueries
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);
}

public sealed class UserQueries(IUserRepository repository) : IUserQueries
{
    private readonly IUserRepository _repository = repository;

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(new UserId(id), ct);
        return user?.ToDto();
    }
}
