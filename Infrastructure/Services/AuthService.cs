namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private string? accessToken;
    private string? refreshToken;
    private string? username;

    private const string UsernameKey = "username";

    public async Task SetTokensAsync(string accessToken, string? refreshToken, string username)
    {
        // сохраняем юзернейм и в памяти, и в Preferences
        this.username = username;
        Preferences.Set(UsernameKey, username);

        this.accessToken = accessToken;
        this.refreshToken = refreshToken;

        await TokenStorage.SaveAccessTokenAsync(accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
            await TokenStorage.SaveRefreshTokenAsync(refreshToken);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        // если уже держим в памяти — возвращаем
        if (!string.IsNullOrWhiteSpace(accessToken))
            return accessToken;

        // иначе пытаемся достать из хранилища
        accessToken = await TokenStorage.GetAccessTokenAsync();
        return accessToken;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
            return refreshToken;

        refreshToken = await TokenStorage.GetRefreshTokenAsync();
        return refreshToken;
    }

    public string? GetCurrentUser()
    {
        if (!string.IsNullOrWhiteSpace(username))
            return username;

        // если ещё не читали в этом сеансе — достанем из Preferences
        username = Preferences.Get(UsernameKey, null);
        return username;
    }

    public void Logout()
    {
        accessToken = null;
        refreshToken = null;
        username = null;

        TokenStorage.Clear();
        Preferences.Remove(UsernameKey);
    }
}