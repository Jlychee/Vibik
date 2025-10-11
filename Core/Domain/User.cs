namespace Vibik.Core.Domain;

using BCrypt.Net;

public class User
{
    public string Name { get; set; } = string.Empty;
    private string PasswordHash { get; set; } = string.Empty;

    public void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Необходимо ввести пароль");
        PasswordHash = BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password)
    {
        return !string.IsNullOrEmpty(password) && BCrypt.Verify(password, PasswordHash);
    }
}