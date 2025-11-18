using Microsoft.Maui.Storage;
using Vibik.Services;

namespace Vibik;

public partial class LoginPage: ContentPage
{
    private readonly IUserApi userApi;
    
    public LoginPage(IUserApi userApi)
    {
        InitializeComponent();
        this.userApi = userApi;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        try
        {
            var user = await userApi.LoginAsync(username, password);
            if (user == null)
            {
                ShowError("Не удалось войти. Проверьте данные.");
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

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistrationPage(userApi));
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}