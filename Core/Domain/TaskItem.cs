namespace Vibik.Core.Domain;

public class TaskItem
{
    public string Title { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Award { get; private set; }
    public ICollection<string> Tags { get; private set; } = new List<string>();
    public bool IsCompleted { get; set; }
    
    public void Complete() => IsCompleted = true;
}