    using System.Net.Http.Json;
    using Core.Domain;
    using Core.Interfaces;
    using Infrastructure.Api;
    using Infrastructure.Utils;

    namespace Infrastructure.Services;

    public class AuthService(IHttpClientFactory httpClientFactory) : IAuthService
    {
        private readonly SemaphoreSlim refreshLock = new(1, 1);

        private string? accessToken;
        private string? refreshToken;

        public async Task SetTokensAsync(string inputAccessToken, string? inputRefreshToken)
        {
            accessToken = inputAccessToken;
            refreshToken = inputRefreshToken;

            await TokenStorage.SaveAccessTokenAsync(inputAccessToken);
            if (inputRefreshToken != null)
                await TokenStorage.SaveRefreshTokenAsync(inputRefreshToken);
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

        public async Task<LoginUserResponse?> TryRefreshTokensAsync(CancellationToken ct = default)
        {
            await refreshLock.WaitAsync(ct);
            try
            {
                var token = await GetRefreshTokenAsync();
                if (token != null)
                {
                    await AppLogger.Info(token);
                    if (string.IsNullOrEmpty(token))
                        return null;
                }

                var client = httpClientFactory.CreateClient("AuthRefresh");
                using var response = await client.PostAsync(
                    ApiRoutes.AuthRefresh,
                    content: null,
                    ct);

                if (!response.IsSuccessStatusCode)
                    return null;

                var refreshed = await response.Content.ReadFromJsonAsync<LoginUserResponse>(cancellationToken: ct);
                if (refreshed != null)
                    await SetTokensAsync(refreshed.AccessToken, refreshed.RefreshToken);
                await AppLogger.Info(refreshed?.ToString()?? string.Empty);
                return refreshed;
            }
            finally
            {
                refreshLock.Release();
            }
        }

        public void Logout()
        {
            accessToken = null;
            refreshToken = null;
            TokenStorage.Clear();
        }
    }
