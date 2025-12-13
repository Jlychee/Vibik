    using System.Net.Http.Json;
    using Core.Domain;
    using Core.Interfaces;
    using Infrastructure.Api;
    using Infrastructure.Utils;

    namespace Infrastructure.Services;

    public class AuthService: IAuthService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly SemaphoreSlim refreshLock = new(1, 1);

        private string? accessToken;
        private string? refreshToken;

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

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

        public async Task<LoginUserResponse?> TryRefreshTokensAsync(CancellationToken ct = default)
        {
            await refreshLock.WaitAsync(ct);
            try
            {
                var token = await GetRefreshTokenAsync();
                await AppLogger.Info(token);
                if (string.IsNullOrEmpty(token))
                    return null;

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
                await AppLogger.Info(refreshed?.ToString());
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
        private record RefreshRequest(string RefreshToken);
    }
