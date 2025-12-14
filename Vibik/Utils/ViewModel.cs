using Core.Domain;

namespace Vibik.Utils;

public sealed class ViewModel
{
    public string TaskName { get; set; }
    public string Description { get; set; }
    public ImageSource ExampleCollage { get; set; }

    public string PhotoCountLabel { get; set; }

    public ViewModel(TaskModel taskModel)
    {
        TaskName = string.IsNullOrWhiteSpace(taskModel.Name) ? "Задание" : taskModel.Name;
        Description = taskModel.ExtendedInfo?.Description ?? string.Empty;

        var example = taskModel.ExtendedInfo?.ExamplePhotos?.FirstOrDefault()?.AbsolutePath;
        ExampleCollage = ImageSourceFinder.ResolveImage(example);

        var required = taskModel.ExtendedInfo?.PhotosRequired ?? 0;

        if (required == -1)
        {
            PhotoCountLabel = "Сделайте столько фоток, сколько душе угодно";
        }
        else
        {
            var word = ChoosePhotoWord(required);
            PhotoCountLabel = $"Нужно {required} {word}";
        }
    }
    
    private static string ChoosePhotoWord(int n)
    {
        var nAbs = Math.Abs(n);
        var lastTwo = nAbs % 100;
        var last = nAbs % 10;

        return last switch
        {
            1 when lastTwo != 11 => "фотка",
            >= 2 and <= 4 when lastTwo is < 12 or > 14 => "фотки",
            _ => "фоток"
        };
    }

}