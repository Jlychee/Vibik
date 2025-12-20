namespace Core.Domain;

public enum AppRefreshReason
{
    TaskSent,
    TaskSwapped,
    ModerationChanged,
    ProfileChanged,
    Any
}

public static class AppEventHub
{
    public static event Action<AppRefreshReason>? RefreshRequested;

    public static void RequestRefresh(AppRefreshReason reason = AppRefreshReason.Any)
        => RefreshRequested?.Invoke(reason);
}