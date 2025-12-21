using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure;

public class UserRepository(UserDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct)
    {
        return db.UserProfiles.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task AddAsync(User user, CancellationToken ct)
    {
        db.UserProfiles.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }
}