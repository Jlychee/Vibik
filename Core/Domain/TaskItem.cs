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
    
    public int DayPassed { get; private set; }
    
    public TimeSpan StartDate { get; set; }
    
    public ISet<string> Tags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public bool IsCompleted { get; private set; }
    
    public string PathToExampleCollage { get; set; } = string.Empty;
    
    public List<string> PhotoPaths { get; } = new ();
    
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

    public void AddPhotoPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        PhotoPaths.Add(path);
    }
    
    public bool RemovePhotoPath(string path)
        => PhotoPaths.Remove(path);


    public bool ReplacePhotoPath(string oldPath, string newPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);
        var idx = PhotoPaths.IndexOf(oldPath);
        if (idx < 0) return false;
        PhotoPaths[idx] = newPath;
        return true;
    }
}