using Domain.Models;

namespace Core.Domain;

public class TaskModel
{    
    public string TaskId { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public int Reward {get; set;}
    
    public bool Completed { get; set; }
    public TaskModelExtendedInfo ModelExtendedInfo {get; set;}

    private const double MagicConst = 0.2;
    public int Swap => (int)(Reward * MagicConst);
}