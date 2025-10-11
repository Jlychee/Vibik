namespace Vibik.Core.Domain;

public class TaskItem
{
    public string OwnerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Award { get; private set; }
    public ISet<string> Tags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public bool IsCompleted { get; private set; }
    
    public void Complete() => IsCompleted = true;

    public void SetAward(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Award = value;
    }
}