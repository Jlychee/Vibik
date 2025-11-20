namespace Infrastructure.Services;

public class AuthService
{
    private string? accessToken;
    private string? refreshToken;

    public async Task SetTokensAsync(string accessToken, string? refreshToken)
    {
        this.accessToken = accessToken;
        this.refreshToken = refreshToken;

        await TokenStorage.SaveAccessTokenAsync(accessToken);
        if (refreshToken != null)
            await TokenStorage.SaveRefreshTokenAsync(refreshToken);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(accessToken))
            return accessToken;

        accessToken = await TokenStorage.GetAccessTokenAsync();
        return accessToken;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        if (!string.IsNullOrEmpty(refreshToken))
            return refreshToken;

        refreshToken = await TokenStorage.GetRefreshTokenAsync();
        return refreshToken;
    }

    public void Logout()
    {
        accessToken = null;
        refreshToken = null;
        TokenStorage.Clear();
    }
}
