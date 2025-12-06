using System.Collections.ObjectModel;
using Core;
using Core.Application;
using Core.Interfaces;
using Infrastructure.Api;
using Infrastructure.Services;
using Shared.Models;
using Vibik.Resources.Components;
using Task = System.Threading.Tasks.Task;
using TaskModel = Shared.Models.Task;
using Vibik.Utils;

namespace Vibik;

public partial class MainPage
{
    private bool taskLoaded;
    private readonly Random random = new();
    private readonly ITaskApi taskApi;
    private readonly List<TaskModel> allTasks = [];
    private readonly IUserApi userApi;
    private readonly LoginPage loginPage;
    private readonly IWeatherApi weatherApi;
    private readonly IAuthService authService;


    private int level;
    private int experience;
    private ImageSource? weatherImage;
    private ObservableCollection<View> VisibleCards { get; } = new();

    public ImageSource? WeatherImage { get => weatherImage; set { weatherImage = value; OnPropertyChanged(); } }
    private string weatherTemp = "—";
    private string weatherInfoAboutSky = "Загружаем погоду...";
    private string weatherInfoAboutFallout = string.Empty;
    private WeatherInfo? lastWeather;


    public int Level { get => level; set { level = value; OnPropertyChanged(); } }
    public int Experience { get => experience; set { experience = value; OnPropertyChanged(); } }
    public string WeatherTemp { get => weatherTemp; set { weatherTemp = value; OnPropertyChanged(); } }
    public string WeatherInfoAboutSky { get => weatherInfoAboutSky; set { weatherInfoAboutSky = value; OnPropertyChanged(); } }
    public string WeatherInfoAboutFallout { get => weatherInfoAboutFallout; set { weatherInfoAboutFallout = value; OnPropertyChanged(); } }
    private bool showCompleted;
    public bool ShowCompleted
    {
        get => showCompleted;
        set
        {
            if (showCompleted == value) return;
            showCompleted = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }
    private bool noTasks;
    public bool NoTasks
    {
        get => noTasks;
        set
        {
            if (noTasks == value) return;
            noTasks = value;
            OnPropertyChanged();
        }
    }

    public MainPage(ITaskApi taskApi, IUserApi userApi, LoginPage loginPage, IWeatherApi weatherApi, IAuthService authService)
    {
        InitializeComponent();
        BindingContext = this;
        this.taskApi = taskApi;
        this.userApi = userApi;
        this.weatherApi = weatherApi;
        this.loginPage = loginPage;
        this.authService = authService;
    }
    
    private async Task LoadWeatherAsync()
    {
        try
        {
            var weather = await weatherApi.GetCurrentWeatherAsync();
            ApplyWeather(weather);
        }
        catch (Exception ex)
        {
            if (lastWeather != null)
            {
                ApplyWeather(lastWeather);
                await AppAlerts.WeatherUpdateFailed();
            }
            else
            {
                WeatherTemp = "—";
                WeatherInfoAboutSky = "Погода недоступна";
                WeatherInfoAboutFallout = "Проверьте подключение к интернету";
                WeatherImage = null;
                await AppAlerts.WeatherUploadFailed(ex.Message);
            }
        }
    }

    private void ApplyWeather(WeatherInfo weather)
    {
        lastWeather = weather;
        WeatherTemp = $"{Math.Round(weather.TemperatureCelsius)}°";
        WeatherInfoAboutSky = string.IsNullOrWhiteSpace(weather.Description)
            ? weather.Condition
            : weather.Description;
        WeatherInfoAboutFallout = WeatherUtils.BuildWeatherInfoAboutFallout(weather);
        var normalized = weather.Condition.ToLowerInvariant();
        WeatherImage = WeatherUtils.DefineWeatherImage(normalized);

    }

    // private async Task<bool> EnsureAuthorizedAsync()
    // {
    //     var currentUser = authService.GetCurrentUser();
    //     var legacyUser = Preferences.Get("current_user", string.Empty);
    //     var userId = string.IsNullOrWhiteSpace(currentUser) ? legacyUser : currentUser;
    //     await AppLogger.Info($"Current user: {userId}, {currentUser ?? "<неизвестен>"}");
    //
    //     var token = await authService.GetAccessTokenAsync();
    //     await AppLogger.Info($"Access token: {token ?? "<null>"}");
    //     await AppLogger.Info($"Refresh token: {await authService.GetRefreshTokenAsync()?? "<null>"}");
    //
    //     return !string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(token);
    //
    // }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // if (!await EnsureAuthorizedAsync())
        // {
        //     await AppLogger.Warn("пользователь не авторизован");
        //     await Navigation.PushModalAsync(new NavigationPage(loginPage));
        //     return;
        // }
        var userTask = LoadUserAsync();
        var weatherTask = LoadWeatherAsync();

        var tasksTask = Task.CompletedTask;
        if (!taskLoaded)
        {
            tasksTask = LoadTasksAsync();
            taskLoaded = true;
        }
        await Task.WhenAll(userTask, weatherTask, tasksTask);
    }
    
    private async Task LoadUserAsync()
    {
        var userId =  Preferences.Get("current_user", "");
        if (string.IsNullOrWhiteSpace(userId))
        {
            await Navigation.PushModalAsync(new NavigationPage(loginPage));
            return;
        }

        try
        {
            var user = await userApi.GetUserAsync(userId);
            if (user != null)
            {
                Level = user.Level;
                Experience = user.Experience;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
        }
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            var tasks = await taskApi.GetTasksAsync();
            await AppLogger.Info($"LoadTasksAsync: получено задач = {tasks.Count}");

            foreach (var t in tasks)
            {
                await AppLogger.Info($"  task: id={t.TaskId}, name={t.Name}, reward={t.Reward}");
            }

            allTasks.Clear();
            allTasks.AddRange(tasks);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await AppAlerts.ProfileUploadFailed(ex.Message);
            VisibleCards.Clear();
            NoTasks = true;
        }
    }
    
    private void ApplyFilter()
    {
        VisibleCards.Clear();
        var filtered = allTasks.ToList();

        NoTasks = filtered.Count == 0;
        if (NoTasks)
            return;

        var count = Math.Min(4, filtered.Count);

        for (var i = 0; i < count; i++)
        {
            var task = filtered[i];
            var card = CreateTaskCard(task);
            CardsHost.Children.Add(card);
        }
        _ = AppLogger.Info($"ApplyFilter: VisibleCards = {VisibleCards.Count}, NoTasks = {NoTasks}");

    }

    private TaskCard CreateTaskCard(TaskModel task)
    {
        var card = new TaskCard
        {
            TaskApi = taskApi,
            Item = task,
            Title = task.Name,
            DaysPassed = task.DaysPassed(),
            Cost = task.Reward,
            SwapCost = task.Swap,
            HorizontalOptions = LayoutOptions.Fill
        };

        card.RefreshCommand = new Command(async () =>
        {
            var current = card.Item;
            if (current is null)
                return;

            var confirmed = await AppAlerts.ChangeTask(card.SwapCost);
            if (!confirmed)
                return;

            var currentTaskIds = CardsHost.Children
                .OfType<TaskCard>()
                .Select(c => c.Item?.TaskId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            var candidates = allTasks
                .Where(t => !t.Completed && !currentTaskIds.Contains(t.TaskId))
                .ToList();

            if (candidates.Count == 0)
            {
                await AppAlerts.NoNewTasks();
                return;
            }

            var next = candidates[random.Next(candidates.Count)];

            card.Item = next;
            card.Title = next.Name;
            card.DaysPassed = next.DaysPassed();
            card.Cost = next.Reward;
            card.SwapCost = next.Swap;
        });

        return card;
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopToRootAsync();
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage(userApi, loginPage, authService));
    }
}   