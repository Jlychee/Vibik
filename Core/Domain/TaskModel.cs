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

    private const double SwapDivisor = 5;
    public int Swap => (int)(Reward / SwapDivisor);
    public ModerationStatus ModerationStatus {get; set;}
}