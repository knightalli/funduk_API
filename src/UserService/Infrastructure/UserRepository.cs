using UserService.Domain;

namespace UserService.Infrastructure;

public class UserRepository : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(User user, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
