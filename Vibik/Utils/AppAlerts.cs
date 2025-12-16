namespace Vibik.Utils;

public static class AppAlerts
{
    public static Task WeatherUpdateFailed() =>
        Alerts.Info("Проблема с погодой",
            "Не удалось обновить погоду, показываем последнее значение.");

    public static Task WeatherUploadFailed(string details) =>
        Alerts.Error($"Не удалось загрузить погоду: {details}");

    public static Task ProfileUploadFailed(string details) =>
        Alerts.Error($"Не удалось загрузить задания: {details}");

    public static Task NoNewTasks() =>
        Alerts.Info("Новых заданий нет",
            "Сейчас нет заданий, которые вы ещё не делали и которых нет среди текущих");

    public static Task<bool> ChangeTask(int details) =>
        Alerts.Confirm("Сменить задание",
            $"Вы уверены, что хотите поменять задание за {details} опыта?");

    public static Task TaskOnModeration() =>
        Alerts.Info("На модерации", "Это задание уже отправлено и сейчас проверяется.");
    
    public static Task TaskCompleted() =>
        Alerts.Info( "Уже проверено", "Это задание уже прошло модерацию, повторно отправлять не нужно.");

    public static Task NotEnoughPhotos(int required, int count) =>
        Alerts.Info("Недостаточно фото", $"Нужно минимум {required}. Сейчас: {count}.");

    public static Task NoPhotos() =>
        Alerts.Info("Нет фото", "Добавьте хотя бы одно фото");

    public static Task TaskSendFailed() =>
        Alerts.Info("Ошибка", "Не удалось отправить задание. Проверьте сеть и попробуйте ещё раз.");

    public static Task TaskSendSuccess() =>
        Alerts.Info("Отправлено", "Задание отправлено на модерацию. Мы сообщим, когда оно будет проверено.");

    public static Task TaskSendHttpError() =>
        Alerts.Info("Ошибка сети", "Не удалось связаться с сервером. Проверьте интернет и попробуйте ещё раз.");
    
    public static Task UniversalError(string details) =>
        Alerts.Error(details);

    public static Task<bool> DeletePhoto() =>
        Alerts.Confirm("Удалить фото?", "Это действие необратимо.", "Удалить", "Отмена");

    public static Task CanNotAddPhoto() =>
        Alerts.Info("Нельзя добавить фото",
            "Задание уже завершено или находится на модерации. Добавлять новые фото нельзя.");

    public static Task PhotoLimit() =>
        Alerts.Info("Лимит", "Достигнуто максимальное количество фото.");

    public static Task DoesNotSupport() =>
        Alerts.Info("Не поддерживается", "Медиа-пикер недоступен на этом устройстве.");

    public static Task NoPermission() =>
        Alerts.Info("Нет доступа", "Разрешите необходимые разрешения в настройках.");

}
