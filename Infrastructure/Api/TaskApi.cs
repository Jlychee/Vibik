using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using Core.Interfaces;
using Infrastructure.Utils;

namespace Infrastructure.Api;

public sealed class TaskApi(HttpClient httpClient) : ITaskApi
{
    public async Task<IReadOnlyList<TaskModel>> GetTasksAsync(CancellationToken ct = default)
    {
        var list = await httpClient.GetFromJsonAsync<List<TaskModel>>(
            ApiRoutes.AllTasks, 
            ct);
        var result = new List<TaskModel>();
        if (list == null) return [];
        foreach (var task in list)
        {
            try
            {
                var fullTask = await GetTaskAsync(task.UserTaskId.ToString(), ct);
                if (fullTask != null) result.Add(fullTask);
            }
            catch (Exception e)
            {
                await AppLogger.Error(e.Message);
                throw;
            }
        }
        return result;
    }
    
    public async Task<IReadOnlyList<TaskModel>> GetCompletedAsync(CancellationToken ct = default)
    {
        var list = await httpClient.GetFromJsonAsync<List<TaskModel>>(ApiRoutes.CompletedTasks, ct);
        return list ?? [];
    }

    public async Task<TaskModel?> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<TaskModel>(
            ApiRoutes.TaskById(taskId),
            ct);
    }
    
    public async Task<string?> GetModerationStatusAsync(string userTaskId, CancellationToken ct = default)
    {
        return await httpClient.GetStringAsync(
                ApiRoutes.ModerationStatus(userTaskId),
                    ct);
    }
    
    public async Task<TaskModel?> SwapTaskAsync(string taskId, CancellationToken ct = default)
    {
        var resp = await httpClient.PutAsync(ApiRoutes.SwapTask(taskId), content: null, ct);

        if (resp.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
            return null;

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"ChangeTask failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }

        if (resp.Content.Headers.ContentLength == 0)
            return null;

        return await resp.Content.ReadFromJsonAsync<TaskModel>(cancellationToken: ct);
    }
    
    public async Task<bool> SubmitAsync(string taskId, IEnumerable<string> photoPaths, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var path in photoPaths.Where(File.Exists))
        {
            var stream = File.OpenRead(path);
            var part = new StreamContent(stream);
            content.Add(part, "files", Path.GetFileName(path));
        }

        var resp = await httpClient.PostAsync(ApiRoutes.SubmitTask(taskId), content, ct);
        return resp.IsSuccessStatusCode;
    }
}
