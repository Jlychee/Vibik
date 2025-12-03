using Task = Shared.Models.Task;

namespace Utils;

public sealed class ViewModel
{
    public string TaskName { get; }
    public string Description { get; }
    public ImageSource ExampleCollage { get; }

    public ViewModel(Task task)
    {
        TaskName = string.IsNullOrWhiteSpace(task.Name) ? "Задание" : task.Name;
        Description = task.ExtendedInfo?.Description ?? string.Empty;

        var example = task.ExtendedInfo?.ExamplePhotos?.FirstOrDefault()?.AbsolutePath;
        ExampleCollage = ImageSourceFinder.ResolveImage(example);
    }
}