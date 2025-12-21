using Common.Contracts.Users;
using UserService.Domain;

namespace UserService.Application;

public static class UserMappings
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(user.Id.Value, user.FirstName, user.LastName, user.Email);
    }
}