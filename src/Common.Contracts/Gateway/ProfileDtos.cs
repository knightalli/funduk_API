namespace Common.Contracts.Gateway;

public sealed record ProfileResponseDto(ProfileUserDto User, IReadOnlyList<ProfileOrderDto> Orders);

public sealed record ProfileUserDto(Guid Id, string FirstName, string LastName, string Email);

public sealed record ProfileOrderDto(Guid Id, IReadOnlyList<ProfileOrderItemDto> Items);

public sealed record ProfileOrderItemDto(Guid ProductId, int Quantity, ProfileProductDto? Product);

public sealed record ProfileProductDto(Guid Id, string Name, string? Category, decimal Price);