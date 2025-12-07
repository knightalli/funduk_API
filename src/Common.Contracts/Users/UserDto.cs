namespace Common.Contracts.Users;

public sealed record UserDto(Guid Id, string FirstName, string LastName, string Email);