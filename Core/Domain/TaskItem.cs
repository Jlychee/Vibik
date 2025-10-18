namespace Vibik.Core.Domain;

public class TaskItem
{
    public string OwnerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    public int Award { get; private set; }
    
    public int SwapCost { get; private set; }
    
    public  int RequiredPhotoCount { get; private set; }
    public TimeSpan StartDate { get; set; }
    
    public int DayPassed { get; private set; }
    public ISet<string> Tags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public bool IsCompleted { get; private set; }
    //public List<ImageSource> Photos { get; } = new List<ImageSource>();
    
    public void Complete() => IsCompleted = true;

    public void SetAward(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Award = value;
    }

    public void SetSwapCost(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        SwapCost = value;
    }

    public void SetDayPassed(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        DayPassed = value;
    }

    public void SetRequiredPhotoCount(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        RequiredPhotoCount = value;
    }

    // public void AddPhoto(ImageSource image)
    // {
    //     ArgumentNullException.ThrowIfNull(image);
    //     Photos.Add(image);
    // }
}