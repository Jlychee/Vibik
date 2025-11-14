using Task = Shared.Models.Task;
namespace Vibik.Services;

public interface ITaskApi
{
    Task<IReadOnlyList<Task>> GetTasksAsync(CancellationToken ct = default);
    Task<Task?> GetTaskAsync(string taskId, CancellationToken ct = default);

    Task<bool> SwapTaskAsync(string taskId, CancellationToken ct = default);
    Task<bool> SubmitAsync(string taskId, IEnumerable<string> photoPaths, CancellationToken ct = default);
}