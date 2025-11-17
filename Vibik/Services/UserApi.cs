using System.Net.Http.Json;
using Shared.Models;

namespace Vibik.Services;

public class UserApi(HttpClient httpClient, bool useStub = false): IUserApi
{
    public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        // if (useStub) return StubUser();
        return await httpClient.GetFromJsonAsync<User>($"api/users/{Uri.EscapeDataString(userId)}",  ct);
    }

    private Task<User?> StubUser()
    {
        throw new NotImplementedException();
    }
}