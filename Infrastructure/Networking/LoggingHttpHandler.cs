using System.Net.Http.Headers;
using System.Text;
using Microsoft.Maui.Storage;

namespace Vibik.Utils;

public sealed class HttpLoggingHandler : DelegatingHandler
{
    private readonly string logFilePath;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public HttpLoggingHandler()
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(dir);
        logFilePath = Path.Combine(dir, "raw-http.log");
        
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTimeOffset.Now:O}] Request: {request.Method} {request.RequestUri}");
        AppendHeaders(sb, "RequestHeaders", request.Headers);

        if (request.Content is not null)
        {
            AppendHeaders(sb, "RequestContentHeaders", request.Content.Headers);
            var body = await request.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body)) sb.AppendLine($"RequestContent: {body}");
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Request failed: {ex}");
            await AppendLogAsync(sb.ToString(), ct);
            throw;
        }

        sb.AppendLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
        AppendHeaders(sb, "ResponseHeaders", response.Headers);
        if (response.Content is not null)
        {
            AppendHeaders(sb, "ResponseContentHeaders", response.Content.Headers);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body)) sb.AppendLine($"ResponseContent: {body}");
        }

        sb.AppendLine(new string('-', 80));
        await AppendLogAsync(sb.ToString(), ct);
        return response;
    }

    private static void AppendHeaders(StringBuilder b, string title, HttpHeaders headers)
    {
        if (!headers.Any()) return;
        b.AppendLine(title + ":");
        foreach (var h in headers) b.AppendLine($"  {h.Key}: {string.Join(", ", h.Value)}");
    }

    private async Task AppendLogAsync(string text, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(logFilePath, text + Environment.NewLine, Encoding.UTF8, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) semaphore.Dispose();
        base.Dispose(disposing);
    }
}
