namespace Infrastructure.Api;

internal static class ApiRoutes
{
    private const string Users = "api/users";

    public static string UserById(string userId) =>
        $"{Users}/{Uri.EscapeDataString(userId)}";

    public static string UserLogin    => $"{Users}/login";
    public static string UserRegister => $"{Users}/register";

    private const string Tasks = "api/Tasks";

    public static string AllTasks        => $"{Tasks}/get_all";
    public static string CompletedTasks  => $"{Tasks}/get_completed";

    public static string TaskById(string taskId) =>
        $"{Tasks}/get_task/{Uri.EscapeDataString(taskId)}";

    public static string SubmitTask(string taskId) =>
        $"{Tasks}/submit/{Uri.EscapeDataString(taskId)}";
}