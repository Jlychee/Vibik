using Core.Application;

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
        await DisplayAlert("Логи", Path.Combine(FileSystem.AppDataDirectory, "logs"), "OK");
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

    private async void OnShareLatestLogClicked(object sender, EventArgs e)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(dir);

        var last = Directory.GetFiles(dir, "*.log")
            .OrderByDescending(f => f)   // имена вида vibik-YYYYMMDD.log или raw-http.log
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

    private void async_void_OnShareLatestLogC(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
}