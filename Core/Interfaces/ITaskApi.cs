using Core.Domain;

namespace Core.Interfaces;

public interface ITaskApi
{
    Task<IReadOnlyList<TaskModel>> GetTasksAsync(CancellationToken ct = default);
    Task<TaskModel?> GetTaskAsync(string taskId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskModel>> GetCompletedAsync(CancellationToken ct = default);
    Task<string?> GetModerationStatusAsync(string userTaskId, CancellationToken ct = default);

    Task<TaskModel?> SwapTaskAsync(string taskId, CancellationToken ct = default);
    Task<bool> SubmitAsync(string taskId, IEnumerable<string> photoPaths, CancellationToken ct = default);
}