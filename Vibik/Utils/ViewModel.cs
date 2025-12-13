using Core.Domain;
using Domain.Models;

namespace Utils;

public sealed class ViewModel
{
    public string TaskName { get; }
    public string Description { get; }
    public ImageSource ExampleCollage { get; }

    public ViewModel(TaskModel taskModel)
    {
        TaskName = string.IsNullOrWhiteSpace(taskModel.Name) ? "Задание" : taskModel.Name;
        Description = taskModel.ModelExtendedInfo?.Description ?? string.Empty;

        var example = taskModel.ModelExtendedInfo?.ExamplePhotos?.FirstOrDefault()?.AbsolutePath;
        ExampleCollage = ImageSourceFinder.ResolveImage(example);
    }
}