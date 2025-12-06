using Infrastructure.Api;
using System.Net;
using System.Net.Http.Headers;
using Core.Interfaces;
using Infrastructure.Utils;

namespace Infrastructure.Networking;

public class AuthHeaderHandler(IAuthService authService) : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> RefreshAttemptKey = new("AuthHeaderHandler.RefreshAttempt");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var refreshAttempted = request.Options.TryGetValue(RefreshAttemptKey, out var attempted) && attempted;

        var token = await authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        await AppLogger.Warn("Я тут");
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized ||
            refreshAttempted ||
            IsRefreshRequest(request))
            return response;
        await AppLogger.Warn("Я тут 2");
        var refreshed = await authService.TryRefreshTokensAsync(cancellationToken);
        if (refreshed is null)
            return response;
        await AppLogger.Warn("Я тут 3");
        response.Dispose();

        var retryRequest = await CloneRequestAsync(request, cancellationToken);
        retryRequest.Options.Set(RefreshAttemptKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var buffer = await request.Content.ReadAsByteArrayAsync(ct);
            var content = new ByteArrayContent(buffer);
            foreach (var h in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            clone.Content = content;
        }

        foreach (var opt in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(opt.Key), opt.Value);

        return clone;
    }

    private static bool IsRefreshRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath;
        return path != null &&
               path.TrimEnd('/').EndsWith("/" + ApiRoutes.AuthRefresh, StringComparison.OrdinalIgnoreCase);
    }
}