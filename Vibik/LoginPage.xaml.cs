using Core.Application;

namespace Vibik;

public partial class LoginPage : ContentPage
{
    private readonly IUserApi userApi;

    public static IUserApi UserApi { get; set; }

    public LoginPage() : this(UserApi) { }

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

}