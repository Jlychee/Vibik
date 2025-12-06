using Core.Application;
using Core.Interfaces;
using Infrastructure.Services;
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
            var login = await userApi.RegisterAsync(username, password, displayName);
            await AppLogger.Info($"есть ли логин: '{login}'");

            if (login is null)
            {
                ShowError("Не удалось создать аккаунт.");
                return;
            }
            // if (authService is not null)
            // {
            //     await authService.SetTokensAsync( login., login.RefreshToken, login.Username);
            // }
            
            Preferences.Set("current_user", login.Username);
            Preferences.Set("display_name", login.DisplayName);

            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await AppLogger.Error("Ошибка при регистрации", ex);

            ShowError(ex.Message);
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