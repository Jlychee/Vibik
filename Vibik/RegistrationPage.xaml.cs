using Core.Interfaces;
using Vibik.Utils;

namespace Vibik;

public partial class RegistrationPage
{
    private readonly IUserApi userApi;
    private readonly IAuthService? authService;
    
    public RegistrationPage(IUserApi userApi, IAuthService? authService)
    {
        InitializeComponent();
        this.userApi = userApi;
        this.authService = authService;
        NavigationPage.SetHasNavigationBar(this, false);
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var displayName = DisplayNameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text;
        var repeatPassword = RepeatPasswordEntry.Text;
        
        if (string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(displayName))
        {
            ShowError("Заполните имя пользователя и пароль.");
            return;
        }
        if (password != repeatPassword)
        { 
            ErrorLabel.Text = "Пароли разные";
            ErrorLabel.IsVisible = true;

            return;
        }

        try
        {
            await AppLogger.Info($"Регистрация нового пользователя: '{username}'");
            try
            {
                await userApi.RegisterAsync(username, displayName, password);
                await AppLogger.Info($"try to register {username}");
            }
            catch (Exception exception)
            {
                await AppLogger.Error("не получилась регистрация {exception.Message}");
                ShowError($"{exception.Message}");
                return;
            }
        }
        catch (Exception ex)
        {
            await AppLogger.Error(ex.Message);
            ShowError(ex.Message);
            return;
        }
        
        Preferences.Set("login_prefill_username_once", username);
        Preferences.Set("login_prefill_password_once", password);

        await DisplayAlert("Успешно", "Вы успешно зарегистрированы. Теперь нужно войти в аккаунт.", "ОК");

        await Navigation.PopAsync();
    }
    
    private void OnUsernameTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry) return;

        var s = entry.Text ?? string.Empty;
        var lower = s.ToLowerInvariant();

        if (s != lower)
        {
            entry.Text = lower;
        }
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
    private void OnTogglePasswordRepeatdVisibilityClicked(object? sender, EventArgs e)
    {
        RepeatPasswordEntry.IsPassword = !RepeatPasswordEntry.IsPassword;
        TogglePasswordRepeatVisibilityButton.Source = RepeatPasswordEntry.IsPassword ? "eye_show.svg" : "eye_hide.svg";
    }
}