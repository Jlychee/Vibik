namespace Vibik.Utils;

public static class AppAlerts
{
    public static Task WeatherUpdateFailed() =>
        Alerts.Info("Проблема с погодой",
            "Не удалось обновить погоду, показываем последнее значение.");

    public static Task WeatherUploadFailed(string details) =>
        Alerts.Error("Ошибка", $"Не удалось загрузить погоду: {details}");

    public static Task ProfileUploadFailed(string details) =>
        Alerts.Error("Ошибка", $"Не удалось загрузить задания: {details}");

    public static Task NoNewTasks() =>
        Alerts.Info("Новых заданий нет",
            "Сейчас нет заданий, которые вы ещё не делали и которых нет среди текущих");

    public static Task<bool> ChangeTask(int details) =>
        Alerts.Confirm("Сменить задание",
            $"Вы уверены, что хотите поменять задание за {details} опыта?");
}