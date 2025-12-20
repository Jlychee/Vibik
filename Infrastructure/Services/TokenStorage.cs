namespace Infrastructure.Services;

using Microsoft.Maui.Storage;

public static class TokenStorage
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    public static async Task SaveAccessTokenAsync(string token)
    {
        await SecureStorage.SetAsync(AccessTokenKey, token);
    }

    public static async Task<string?> GetAccessTokenAsync()
    {
        return await SecureStorage.GetAsync(AccessTokenKey);
    }

    public static async Task SaveRefreshTokenAsync(string token)
    {
        await SecureStorage.SetAsync(RefreshTokenKey, token);
    }

    public static async Task<string?> GetRefreshTokenAsync()
    {
        return await SecureStorage.GetAsync(RefreshTokenKey);
    }

    public static void Clear()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
    }
}