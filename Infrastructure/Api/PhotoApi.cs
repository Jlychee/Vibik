using System.Net.Http.Headers;
using System.Net.Http.Json;
using Core.Interfaces;

namespace Infrastructure.Api;

public class PhotoApi(HttpClient http) : IPhotoApi
{
    public async Task<string?> UploadAsync(string filePath, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(filePath);
        using var sc = new StreamContent(fs);
        sc.Headers.ContentType = new MediaTypeHeaderValue(GetMime(filePath));

        using var form = new MultipartFormDataContent();
        form.Add(sc, "File", Path.GetFileName(filePath));

        using var resp = await http.PostAsync(ApiRoutes.UploadPhoto, form, ct);
        if (!resp.IsSuccessStatusCode) return null;

        try
        {
            var payload = await resp.Content.ReadFromJsonAsync<UploadResponse>(cancellationToken: ct);
            if (!string.IsNullOrWhiteSpace(payload?.Result))
                return payload!.Result;
        }
        catch { }

        if (resp.Headers.Location is Uri u) return u.ToString();

        var text = await resp.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string GetMime(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        _ => "application/octet-stream"
    };

    private sealed record UploadResponse(string Result);
}