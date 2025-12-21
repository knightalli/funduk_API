namespace UserService.Domain;

public class User
{
    private User()
    {
    }

    public User(UserId id, string firstName, string lastName, string email)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public UserId Id { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string Email { get; private set; } = null!;
}

public readonly record struct UserId(Guid Value);