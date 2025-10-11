namespace Vibik.Core.Domain;

using BCrypt.Net;

public class User
{
    public string Name { get; private set; } = string.Empty;
    private string PasswordHash { get; set; } = string.Empty;
    public int Experience { get; set; }
    public ICollection<TaskItem> TaskItems { get; private set; } = new List<TaskItem>();

    public User(string name, string password)
    {
        SetName(name);
        SetPassword(password);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя обязательно", nameof(name));
        Name = NormalizedName(name);
    }

    public void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Необходимо ввести пароль");
        PasswordHash = BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password) => !string.IsNullOrEmpty(password) && BCrypt.Verify(password, PasswordHash);
    
    private static string NormalizedName(string name) => name.ToUpper().Trim();
}