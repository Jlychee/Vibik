using System.Net;
using System.Net.Http.Json;
using Core;
using Core.Domain;
using Core.Interfaces;
using Infrastructure.Utils;

namespace Infrastructure.Api;

public sealed class TaskApi: ITaskApi
{
    private readonly HttpClient httpClient;
    private readonly bool useStub;

    public TaskApi(HttpClient httpClient, bool useStub = false)
    { 
        this.httpClient = httpClient;
        this.useStub = useStub;
    }

    public async Task<IReadOnlyList<TaskModel>> GetTasksAsync(CancellationToken ct = default)
    {
        if (useStub) return StubTasks();
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
        return result ?? [];
    }
    
    public async Task<IReadOnlyList<TaskModel>> GetCompletedAsync(CancellationToken ct = default)
    {
        if (useStub) return [];
        var list = await httpClient.GetFromJsonAsync<List<TaskModel>>(ApiRoutes.CompletedTasks, ct);
        return list ?? [];
    }

    public async Task<TaskModel?> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        if (useStub) return StubTasks().FirstOrDefault(t => t.TaskId == taskId);
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
        if (useStub) return true;

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
    
    private static List<TaskModel> StubTasks()
    {
        return
        [
            new TaskModel
            {
                TaskId = "1",
                Name = "Сфотографируй облака",
                StartTime = DateTime.UtcNow.AddDays(-1),
                Reward = 50,
                Completed = false,
                ExtendedInfo = new TaskModelExtendedInfo
                {
                    Description = "Найди интересные облака и сфотографируй",
                    PhotosRequired = 2,
                    ExamplePhotos = null,
                    UserPhotos = []
                }
            },

            new TaskModel
            {
                TaskId = "2",
                Name = "Пейзаж у воды",
                StartTime = DateTime.UtcNow.AddDays(-3),
                Reward = 80,
                Completed = false,
                ExtendedInfo = new TaskModelExtendedInfo
                {
                    Description = "Линия горизонта и отражения",
                    PhotosRequired = 4,
                    ExamplePhotos = null,
                    UserPhotos = []
                }
            },
            
            new TaskModel
            {
                TaskId = "3",
                Name = "Медовые 6",
                StartTime = DateTime.UtcNow.AddDays(-1),
                Reward = 50,
                Completed = false,
                ExtendedInfo = new TaskModelExtendedInfo
                {
                    Description = "Сфотографируй 6 желтых машин",
                    PhotosRequired = 6,
                    ExamplePhotos = null,
                    UserPhotos = []
                }
            },
            new TaskModel
            {
                TaskId = "3",
                Name = "Медовые 9",
                StartTime = DateTime.UtcNow.AddDays(-1),
                Reward = 50,
                Completed = false,
                ExtendedInfo = new TaskModelExtendedInfo
                {
                    Description = "Сфотографируй 9 желтых машин",
                    PhotosRequired = 9,
                    ExamplePhotos = null,
                    UserPhotos = []
                }
            },
            new TaskModel
            {
                TaskId = "4",
                Name = "Медовые 666",
                StartTime = DateTime.UtcNow.AddDays(-1),
                Reward = 50,
                Completed = false,
                ExtendedInfo = new TaskModelExtendedInfo
                {
                    Description = "ЛОВУШКААКАКА",
                    PhotosRequired = 9,
                    ExamplePhotos = null,
                    UserPhotos = []
                }
            },
            new TaskModel
            {
                TaskId = "5",
                Name = "Русский андерграунд",
                StartTime = DateTime.UtcNow,
                Reward = 50,
                Completed = false,
                ExtendedInfo = new TaskModelExtendedInfo
                {
                    Description = "Сфоткать 5 графифити",
                    PhotosRequired = 5,
                    ExamplePhotos = null,
                    UserPhotos = []
                }
            }
        ];
    }
}
