using TaskModel = Shared.Models.Task;

namespace Infrastructure.Api;

public static class BackendMapper
{
    public static int DaysPassed(this TaskModel t)
    {
        var startUtc = t.StartTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(t.StartTime, DateTimeKind.Utc)
            : t.StartTime.ToUniversalTime();

        var days = (int)Math.Max(0, (DateTime.UtcNow - startUtc).TotalDays);
        return days;
    } 
}