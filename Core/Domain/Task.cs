namespace Shared.Models;

public class Task
{    
    public string TaskId { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public int Reward {get; set;}
    
    public bool Completed { get; set; }
    public TaskExtendedInfo ExtendedInfo {get; set;}

    private const double MagicConst = 0.3;
    public int Swap => (int)(Reward * MagicConst);
}