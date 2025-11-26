using Core.Application;

namespace Vibik;

public partial class RegistrationPage
{
    private readonly IUserApi userApi;
    
    public RegistrationPage(IUserApi userApi)
    {
        InitializeComponent();
        this.userApi = userApi;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var displayName = DisplayNameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text;
        var repeatPassword = RepeatPasswordEntry.Text;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Заполните имя пользователя и пароль.");
            return;
        }

        try
        {
            if (password != repeatPassword)
            { 
                ErrorLabel.Text = "Пароли разные";
                ErrorLabel.IsVisible = true;

                return;
            }

            var user = await userApi.RegisterAsync(username, displayName, password);
            if (user == null)
            {
                ShowError("Не удалось создать аккаунт.");
                return;
            }
            
            Preferences.Set("current_user", user.Username);
            Preferences.Set("display_name", user.DisplayName);

            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
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