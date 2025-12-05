namespace Shared.Models;

public class LoginResponse
{
    /// <summary>Имя пользователя (логин).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Отображаемое имя (ник/ФИО).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Access-токен (JWT), которым будем ходить в API.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Refresh-токен, если backend его выдаёт (может быть null).</summary>
    public string? RefreshToken { get; set; }
}