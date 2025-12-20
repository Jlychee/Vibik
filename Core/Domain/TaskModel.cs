namespace Core.Domain;

public class TaskModel
{    
    public int UserTaskId { get; init; }
    public required string TaskId { get; set; }
    public required string Name { get; set; }
    public DateTime StartTime { get; init; }
    public int Reward {get; set;}
    
    public bool Completed { get; set; }
    public TaskModelExtendedInfo? ExtendedInfo {get; set;}
    public int Swap { get; set; }
    public ModerationStatus ModerationStatus {get; set;}
}