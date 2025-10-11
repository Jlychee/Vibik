namespace Vibik.Core.Domain;

using BCrypt.Net;

public class User
{
    public string Name { get; private set; } = string.Empty;
    private string PasswordHash = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public int Experience { get; set; }
    public ICollection<TaskItem> TaskItems { get; private set; } = new List<TaskItem>();

    protected User()
    {
    }

    public User(string name, string password)
    {
        SetName(name);
        SetPassword(password);
    }

    internal static User FromDataBase(string username, string passwordHash) =>
        new User { Name = username.Trim(), NormalizedName = Normalize(username), PasswordHash = passwordHash };

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя обязательно", nameof(name));
        Name = name.Trim();
        NormalizedName = Normalize(name);
    }

    public void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Необходимо ввести пароль");
        PasswordHash = BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password) =>
        !string.IsNullOrWhiteSpace(password) && BCrypt.Verify(password, PasswordHash);

    private static string Normalize(string name) => name.Trim().ToLowerInvariant();
}