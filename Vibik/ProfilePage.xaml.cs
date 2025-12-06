using System.Net;
using Core.Application;
using Core.Interfaces;
using Infrastructure.Services;

namespace Vibik;

public partial class ProfilePage
{
    private readonly IUserApi? userApi;
    private readonly LoginPage loginPage;
    private readonly IAuthService? authService;


    private string displayName = string.Empty;
    public string DisplayName
    {
        get => displayName;
        set { displayName = value; OnPropertyChanged(); }
    }

    private string username = string.Empty;
    public string Username
    {
        get => username;
        set { username = value; OnPropertyChanged(); }
    }

    private int level;
    public int Level
    {
        get => level;
        set { level = value; OnPropertyChanged(); }
    }

    private int experience;
    public int Experience
    {
        get => experience;
        set { experience = value; OnPropertyChanged(); }
    }

    private int completedTasks;
    public int CompletedTasks
    {
        get => completedTasks;
        set { completedTasks = value; OnPropertyChanged(); }
    }

    private int placesCount;
    public int PlacesCount
    {
        get => placesCount;
        set { placesCount = value; OnPropertyChanged(); }
    }

    public ProfilePage(IUserApi? userApi, LoginPage loginPage, IAuthService? authService)
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
        var userId = authService?.GetCurrentUser() ?? Preferences.Get("current_user", "");

        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlert("Ошибка", "Пользователь не найден. Зайдите заново.", "OK");
            Application.Current!.MainPage = new NavigationPage(loginPage);
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

        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            Preferences.Remove("current_user");
            Preferences.Remove("display_name");

            await DisplayAlert("Сессия истекла", "Пожалуйста, войдите ещё раз.", "OK");

            Application.Current!.MainPage = new NavigationPage(loginPage);
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
        Preferences.Remove("current_user");
        if (Application.Current != null) Application.Current.MainPage = new NavigationPage(loginPage);
    }
}
