using System.Net;
using System.Net.Http.Headers;
using Core.Interfaces;
using Infrastructure.Services;
using Infrastructure.Utils;

namespace Infrastructure.Networking;

public sealed class AuthHeaderHandler(IAuthService authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetAccessTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await AppLogger.Warn("API вернул 401 — токен не добавлен или протух. Пользователь остаётся в сессии для ручного входа.");
        }

        return response;
    }
}