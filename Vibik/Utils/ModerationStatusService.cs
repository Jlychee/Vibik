using Core.Domain;

namespace Vibik.Utils;

public static class ModerationStatusService
{
    public static ModerationStatus MapModeration(string? statusString)
    {
        var normalized = statusString?.Trim().Trim('"').ToLowerInvariant();
        return normalized switch
        {
            "waiting" => ModerationStatus.Pending,
            "default" => ModerationStatus.None,
            "approve" => ModerationStatus.Approved,
            "reject" => ModerationStatus.Rejected,
            _ => ModerationStatus.None
        };
    }
}