using Core.Interfaces;
using Vibik.Utils;

namespace Vibik;

public partial class LoginPage
{
    private readonly IUserApi userApi;
    private readonly IAuthService authService;

    public LoginPage(IUserApi userApi, IAuthService authService)
    {
        InitializeComponent();
        this.userApi = userApi;
        this.authService = authService;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var uOnce = Preferences.Get("login_prefill_username_once", string.Empty);
        var pOnce = Preferences.Get("login_prefill_password_once", string.Empty);

        if (!string.IsNullOrWhiteSpace(uOnce))
        {
            UsernameEntry.Text = uOnce;
            Preferences.Remove("login_prefill_username_once");
        }
        else
        {
            UsernameEntry.Text = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(pOnce))
        {
            PasswordEntry.Text = pOnce;
            Preferences.Remove("login_prefill_password_once");
        }
        else
        {
            PasswordEntry.Text = string.Empty;
        }

        ErrorLabel.IsVisible = false;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await AppLogger.Warn("Попытка логина с пустым логином/паролем");
            ShowError("Введите логин и пароль.");
            return;
        }

        try
        {
            await AppLogger.Info($"Попытка логина: '{username}'");
            var tokens = await userApi.LoginAsync(username, password);

            if (tokens == null)
            {
                await AppLogger.Warn($"Неудачный логин: '{username}'");
                ShowError("Не удалось войти. Проверьте данные.");
                return;
            }

            await authService.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
            Preferences.Set("current_user", username);

            await AppLogger.Info($"Успешный логин: '{username}'");
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
            File = new ShareFile(last)
        });
    }
}
