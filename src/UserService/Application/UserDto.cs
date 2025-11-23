namespace UserService.Application;

public sealed record UserDto(Guid Id, string FirstName, string LastName, string Email);