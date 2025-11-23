using System.Net.Http.Json;
using Core;
using Shared.Models;
using TaskModel = Shared.Models.Task;

namespace Infrastructure.Api;

public sealed class TaskApi: ITaskApi
{
    private readonly HttpClient httpClient;
    private readonly bool useStub;

    public TaskApi(HttpClient httpClient, bool useStub = true)
    { 
        this.httpClient = httpClient;
        this.httpClient.BaseAddress ??= new Uri("http://localhost:5000");
        this.useStub = useStub;
    }

    public async Task<IReadOnlyList<TaskModel>> GetTasksAsync(CancellationToken ct = default)
    {
        if (useStub) return StubTasks();
        var list = await httpClient.GetFromJsonAsync<List<TaskModel>>("api/tasks", ct);
        return list ?? [];
    }

    public async Task<TaskModel?> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        if (useStub) return StubTasks().FirstOrDefault(t => t.TaskId == taskId);
        return await httpClient.GetFromJsonAsync<TaskModel>($"api/tasks/{Uri.EscapeDataString(taskId)}", ct);
    }

    public async Task<bool> SwapTaskAsync(string taskId, CancellationToken ct = default)
    {
        if (useStub) return true;
        var resp = await httpClient.PostAsync($"api/tasks/{Uri.EscapeDataString(taskId)}/swap", content: null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> SubmitAsync(string taskId, IEnumerable<string> photoPaths, CancellationToken ct = default)
    {
        if (useStub) return true;

        using var content = new MultipartFormDataContent();
        foreach (var path in photoPaths.Where(File.Exists))
            content.Add(new StreamContent(File.OpenRead(path)), "files", Path.GetFileName(path));

        var resp = await httpClient.PostAsync($"api/tasks/{Uri.EscapeDataString(taskId)}/submit", content, ct);
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
                ExtendedInfo = new TaskExtendedInfo
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
                ExtendedInfo = new TaskExtendedInfo
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
                ExtendedInfo = new TaskExtendedInfo
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
                ExtendedInfo = new TaskExtendedInfo
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
                ExtendedInfo = new TaskExtendedInfo
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
                ExtendedInfo = new TaskExtendedInfo
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
