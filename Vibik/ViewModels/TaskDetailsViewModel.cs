using System.ComponentModel;
using System.Runtime.CompilerServices;
using Core.Domain;
using Vibik.Utils;
using Vibik.Utils.Image;

namespace Vibik.ViewModels;

public sealed class TaskDetailsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string TaskName { get; set; }
    public string Description { get; set; }
    public ImageSource ExampleCollage { get; set; }
    public string PhotoCountLabel { get; set; }

    private bool isSending;
    public bool IsSending
    {
        get => isSending;
        set
        {
            if (isSending == value) return;
            isSending = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSend));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    private ModerationStatus moderationStatus;
    public ModerationStatus ModerationStatus
    {
        get => moderationStatus;
        set
        {
            if (moderationStatus == value) return;
            moderationStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSend));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    private bool isCompletedView;
    public bool IsCompletedView
    {
        get => isCompletedView;
        set
        {
            if (isCompletedView == value) return;
            isCompletedView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSend));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    public bool CanSend =>
        !IsSending &&
        !IsCompletedView &&
        ModerationStatus is not ModerationStatus.Pending
                         and not ModerationStatus.Approved;

    public string SendButtonText =>
        IsSending ? "Отправляем…" :
        ModerationStatus == ModerationStatus.Pending ? "На модерации" :
        ModerationStatus == ModerationStatus.Approved ? "Одобрено" :
        "Отправить";

    public TaskDetailsViewModel(TaskModel taskModel)
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
            var (word1, word2) = ChoosePhotoWord(required);
            PhotoCountLabel = $"{word1} {required} {word2}";
        }

        ModerationStatus = taskModel.ModerationStatus;
    }

    private static (string, string) ChoosePhotoWord(int n)
    {
        var nAbs = Math.Abs(n);
        var lastTwo = nAbs % 100;
        var last = nAbs % 10;

        return last switch
        {
            1 when lastTwo != 11 => ("Нужна", "фотка"),
            >= 2 and <= 4 when lastTwo is < 12 or > 14 => ("Нужны", "фотки"),
            _ => ("Нужно", "фоток")
        };
    }
}
