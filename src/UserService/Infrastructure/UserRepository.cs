using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure;

public class UserRepository(UserDbContext db) : IUserRepository
{
    private readonly UserDbContext _db = db;

    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct)
    {
        return _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task AddAsync(User user, CancellationToken ct)
    {
        _db.UserProfiles.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}
