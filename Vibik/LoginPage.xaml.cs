using Core.Application;
using Vibik.Utils;

namespace Vibik;

public partial class LoginPage
{
    private readonly IUserApi? userApi;

    public static IUserApi? UserApi { get; set; }

    public LoginPage() : this(UserApi) { }

    public LoginPage(IUserApi? userApi)
    {
        InitializeComponent();
        this.userApi = userApi;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await AppLogger.Warn("Попытка логина с пустым логином/паролем");
            ShowError("Введите логин и пароль.");
            return;
        }

        try
        {
            await AppLogger.Info($"Попытка логина: '{username}'");

            var user = await userApi!.GetUserAsync(username);
            if (user == null)
            {
                await AppLogger.Warn($"Неудачный логин: пользователь '{username}' не найден (заглушка).");
                ShowError("Не удалось войти. Проверьте данные.");
                return;
            }

            Preferences.Set("current_user", user.Username);
            Preferences.Set("display_name", user.DisplayName);

            await AppLogger.Info($"Успешный логин: '{user.Username}'");

            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await AppLogger.Error("Ошибка при логине", ex);
            ShowError(ex.Message);
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrationPage(userApi));
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void OnTogglePasswordVisibilityClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordVisibilityButton.Source = PasswordEntry.IsPassword ? "eye_show.svg" : "eye_hide.svg";
    }
    
    private async void OnShareLatestLogClicked(object sender, EventArgs e)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(dir);

        var last = Directory.GetFiles(dir, "*.log")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (last is null)
        {
            await DisplayAlert("Логи", "Файл логов ещё не создан. Сначала сделай HTTP-запрос.", "OK");
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Vibik log",
            File  = new ShareFile(last)
        });
    }
}