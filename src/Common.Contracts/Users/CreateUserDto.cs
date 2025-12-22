namespace Common.Contracts.Users;

public sealed record CreateUserDto(
    string FirstName,
    string LastName,
    string Email,
    string Password
);