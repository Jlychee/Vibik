using Vibik.Services;
using Microsoft.Maui.Storage;

namespace Vibik;

public partial class RegistrationPage: ContentPage
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

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Заполните имя пользователя и пароль.");
            return;
        }

        try
        {
            var user = await userApi.RegisterAsync(username, displayName, password);
            if (user == null)
            {
                ShowError("Не удалось создать аккаунт.");
                return;
            }

            Preferences.Set("current_user", user.Username);
            Preferences.Set("display_name", user.DisplayName ?? user.Username);

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
}