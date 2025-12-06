using Core.Application;
using Core.Interfaces;

namespace Vibik;

public partial class ProfilePage
{
    private readonly IUserApi userApi;
    private readonly LoginPage loginPage;
    private readonly IAuthService authService;

    private string displayName = string.Empty;

    public string DisplayName
    {
        get => displayName;
        set
        {
            displayName = value;
            OnPropertyChanged();
        }
    }

    private string username = string.Empty;

    public string Username
    {
        get => username;
        set
        {
            username = value;
            OnPropertyChanged();
        }
    }

    private int level;

    public int Level
    {
        get => level;
        set
        {
            level = value;
            OnPropertyChanged();
        }
    }

    private int experience;

    public int Experience
    {
        get => experience;
        set
        {
            experience = value;
            OnPropertyChanged();
        }
    }

    private int completedTasks;

    public int CompletedTasks
    {
        get => completedTasks;
        set
        {
            completedTasks = value;
            OnPropertyChanged();
        }
    }

    private int placesCount;

    public int PlacesCount
    {
        get => placesCount;
        set
        {
            placesCount = value;
            OnPropertyChanged();
        }
    }

    public ProfilePage(IUserApi userApi, LoginPage loginPage, IAuthService authService)
    {
        InitializeComponent();
        this.userApi = userApi;
        this.loginPage = loginPage;
        this.authService = authService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserAsync();
    }

    private async Task LoadUserAsync()
    {
        var userId = Preferences.Get("current_user", "");

        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Ошибка", "Пользователь не найден. Зайдите заново.", "OK");
            await Navigation.PushModalAsync(new NavigationPage(loginPage));
            return;
        }

        try
        {
            var user = await userApi.GetUserAsync(userId);
            if (user is null)
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить профиль.", "OK");
                return;
            }

            Username = user.Username;
            DisplayName = user.DisplayName;
            Level = user.Level;
            Experience = user.Experience;

            // #ЗАГЛУШКА
            CompletedTasks = 0;
            PlacesCount = 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
        }
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }

    private async void OnProfileClicked(object? sender, EventArgs e)
    {
        if (ProfileScroll is not null)
            await ProfileScroll.ScrollToAsync(0, 0, true);
    }

    private async void OnMapClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        authService.Logout();
        Preferences.Remove("current_user");
        if (Application.Current != null) Application.Current.MainPage = new NavigationPage(loginPage);
    }
}