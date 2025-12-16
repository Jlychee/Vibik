namespace Vibik.Utils;

public static class Alerts
{
    private const string OkMessage = "ОК";
    private const string ErrorMessage = "Ошибка";
    private static Page? CurrentPage =>
        Application.Current?.MainPage switch
        {
            Shell shell => shell.CurrentPage,
            NavigationPage nav => nav.CurrentPage,
            { } page => page,
            _ => null
        };

    private static async Task ShowAsync(string title, string message, string cancel)
    {
        var page = CurrentPage;
        if (page is null)
            return;

        if (MainThread.IsMainThread)
        {
            await page.DisplayAlert(title, message, cancel);
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(
                () => page.DisplayAlert(title, message, cancel));
        }
    }
    private static async Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = CurrentPage;
        if (page is null)
            return false;

        if (MainThread.IsMainThread)
        {
            return await page.DisplayAlert(title, message, accept, cancel);
        }

        return await MainThread.InvokeOnMainThreadAsync(
            () => page.DisplayAlert(title, message, accept, cancel));
    }

    public static Task Info(string title, string message) =>
        ShowAsync(title, message, OkMessage);

    public static Task Error(string message) =>
        ShowAsync(ErrorMessage, message, OkMessage);
    
    public static Task<bool> Confirm(
        string title, 
        string message, 
        string accept = "Да",
        string cancel = "Нет") => ShowConfirmAsync(title, message, accept, cancel);
}