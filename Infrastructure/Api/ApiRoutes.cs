namespace Infrastructure.Api;

internal static class ApiRoutes
{
    private const string Users = "api/users";
    private const string Auth = "api/Auth";

    public static string UploadPhoto => "photos/upload";
    public static string UserById() => $"{Users}/get_user";

    public static string UserLogin => $"{Auth}/login";
    public static string UserRegister => $"{Auth}/register";

    private const string Tasks = "api/Tasks";

    public static string AllTasks => $"{Tasks}/get_all";
    public static string CompletedTasks => $"{Tasks}/get_completed";

    public static string TaskById(string taskId) =>
        $"{Tasks}/get_task/{Uri.EscapeDataString(taskId)}";

    public static string SubmitTask(string taskId) =>
        $"{Tasks}/submit/{Uri.EscapeDataString(taskId)}";
}