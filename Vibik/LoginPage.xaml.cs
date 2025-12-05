using Core.Application;
using Infrastructure.Services;
using Vibik.Utils;

namespace Vibik;

public partial class LoginPage
{
    private readonly IUserApi? userApi;
    private readonly IAuthService? authService;

    public static IUserApi? UserApi { get; set; }

    public LoginPage() : this(UserApi, null) { }

    public LoginPage(IUserApi? userApi, IAuthService? authService)
    {
        InitializeComponent();
        this.userApi = userApi;
        this.authService = authService;
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
        if (userApi is null)
        {
            await AppLogger.Error("LoginPage: userApi == null, DI не настроен");
            ShowError("Внутренняя ошибка: API не настроен.");
            return;
        }
        try
        {
            var result = await userApi.LoginAsync(username, password);

            if (result is null)
            {
                await AppLogger.Warn($"Неудачный логин для '{username}'");
                ShowError("Не удалось войти. Проверьте логин и пароль.");
                return;
            }
            var normalizedUsername = string.IsNullOrWhiteSpace(result.Username)
                ? username
                : result.Username;

            if (authService is null)
            {
                await AppLogger.Warn("LoginPage: authService == null, токены не сохраняются");
            }
            else
            {
                await authService.SetTokensAsync(result.AccessToken, result.RefreshToken, normalizedUsername);
            }
            Preferences.Set("current_user", normalizedUsername);

            Preferences.Set("display_name", result.DisplayName);

            await AppLogger.Info($"Успешный логин: '{normalizedUsername}'");

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
        await Navigation.PushAsync(new RegistrationPage(userApi, authService));
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