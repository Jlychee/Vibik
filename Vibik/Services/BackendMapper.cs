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
    
    public static string? ExampleCollagePathOrUrl(this TaskModel t)
        => t.ExtendedInfo?.ExamplePhotos?.FirstOrDefault()?.Url;

    public static IEnumerable<string> UserPhotoUrls(this TaskModel t)
        => t.ExtendedInfo.UserPhotos?
               .Select(p => p.Url)
               .Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];

    public static int PhotosRequired(this TaskModel t)
        => t.ExtendedInfo?.PhotosRequired ?? 0;
}