using System.Net.Http.Headers;
using Core.Interfaces;

namespace Infrastructure.Networking;

public class RefreshHeaderHandler(IAuthService authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var refreshToken = await authService.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
