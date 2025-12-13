using Core.Domain;
using Domain.Models;

namespace Core;

public interface ITaskApi
{
    Task<IReadOnlyList<TaskModel>> GetTasksAsync(CancellationToken ct = default);
    Task<TaskModel?> GetTaskAsync(string taskId, CancellationToken ct = default);

    Task<bool> SwapTaskAsync(string taskId, CancellationToken ct = default);
    Task<bool> SubmitAsync(string taskId, IEnumerable<string> photoPaths, CancellationToken ct = default);
}