using Common.Contracts.Users;
using Microsoft.AspNetCore.Identity;
using UserService.Domain;
using UserService.Infrastructure;

namespace UserService.Application;

public interface IUserCommands
{
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct);
}

public sealed class UserCommands(
    IUserRepository profiles,
    UserManager<ApplicationUser> userManager
) : IUserCommands
{
    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct)
    {
        var email = dto.Email.Trim();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException("User with this email already exists.");

        var id = Guid.NewGuid();

        var identityUser = new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email
        };

        var createIdentity = await userManager.CreateAsync(identityUser, dto.Password);
        if (!createIdentity.Succeeded)
        {
            var errors = string.Join("; ", createIdentity.Errors.Select(e => $"{e.Code}:{e.Description}"));
            throw new InvalidOperationException($"Identity create failed: {errors}");
        }

        var profile = new User(new UserId(id), dto.FirstName, dto.LastName, email);

        await profiles.AddAsync(profile, ct);
        await profiles.SaveChangesAsync(ct);

        return profile.ToDto();
    }
}