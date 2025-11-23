namespace UserService.Domain;

public class User(UserId id, string firstName, string lastName, string email)
{
    public UserId Id { get; private set; } = id;

    public string FirstName { get; private set; } = firstName;

    public string LastName { get; private set; } = lastName;

    public string Email { get; private set; } = email;
}
