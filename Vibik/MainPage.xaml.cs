using System.Collections.ObjectModel;
using Core;
using Core.Application;
using Core.Domain;
using Core.Interfaces;
using Infrastructure.Api;
using Domain.Models;
using Vibik.Resources.Components;
using Task = System.Threading.Tasks.Task;
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


    private int money;
    private int level;
    private ImageSource? weatherImage;
    private ObservableCollection<View> VisibleCards { get; } = new();

    public ImageSource? WeatherImage { get => weatherImage; set { weatherImage = value; OnPropertyChanged(); } }
    private string weatherTemp = "—";
    private string weatherInfoAboutSky = "Загружаем погоду...";
    private string weatherInfoAboutFallout = string.Empty;
    private WeatherInfo? lastWeather;


    public int Level { get => level; set { level = value; OnPropertyChanged(); } }
    public int Money { get => money; set { money = value; OnPropertyChanged(); } }
    public string WeatherTemp { get => weatherTemp; set { weatherTemp = value; OnPropertyChanged(); } }
    public string WeatherInfoAboutSky { get => weatherInfoAboutSky; set { weatherInfoAboutSky = value; OnPropertyChanged(); } }
    public string WeatherInfoAboutFallout { get => weatherInfoAboutFallout; set { weatherInfoAboutFallout = value; OnPropertyChanged(); } }
    
    private List<TaskModel>? completedTasks;
    private bool showCompleted;
    public bool ShowCompleted
    {
        get => showCompleted;
        set
        {
            if (showCompleted == value) return;
            showCompleted = value;
            OnPropertyChanged();
            _ = ApplyFilter();
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

    private string? ResolveCurrentUserId()
    {
        var userId = Preferences.Get("current_user", string.Empty);
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }

    private async Task<bool> EnsureAuthorizedAsync()
    {
        var userId = ResolveCurrentUserId();
        await AppLogger.Info($"Current user: {userId ?? "<null>"}");

        var accessToken = await authService.GetAccessTokenAsync();
        var refreshToken = await authService.GetRefreshTokenAsync();

        await AppLogger.Info($"Access token: {accessToken ?? "<null>"}");
        await AppLogger.Info($"Refresh token: {refreshToken ?? "<null>"}");

        return !string.IsNullOrWhiteSpace(userId) &&
               !string.IsNullOrWhiteSpace(accessToken);
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await EnsureAuthorizedAsync())
        {
            await AppLogger.Warn("пользователь не авторизован");
            await Navigation.PushModalAsync(new NavigationPage(loginPage));
            return;
        }

        var user = LoadUserAsync();
        var weather = LoadWeatherAsync();

        var tasks = Task.CompletedTask;
        if (!taskLoaded)
        {
            tasks = LoadTasksAsync();
            taskLoaded = true;
        }
        await Task.WhenAll(user, weather, tasks);
    }
    
    private async Task LoadUserAsync()
    {
        var userId = ResolveCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await AppLogger.Warn("LoadUserAsync: userId пуст, показываем экран логина");
            await Navigation.PushModalAsync(new NavigationPage(loginPage));
            return;
        }

        try
        {
            var user = await userApi.GetUserAsync(userId);
            if (user != null)
            {
                Money = user.Money;
                Level = user.Level;
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
            await ApplyFilter();
        }
        catch (Exception ex)
        {
            await AppAlerts.ProfileUploadFailed(ex.Message);
            VisibleCards.Clear();
            NoTasks = true;
        }
    }
    
    private async Task EnsureCompletedTasksLoadedAsync()
    {
        if (completedTasks != null)
            return;

        try
        {
            var list = await taskApi.GetCompletedAsync();
            completedTasks = list?.ToList() ?? new List<TaskModel>();

            await AppLogger.Info($"LoadCompletedTasks: получено выполненных задач = {completedTasks.Count}");
        }
        catch (Exception ex)
        {
            completedTasks = [];
            await AppLogger.Warn($"Не удалось загрузить выполненные задания: {ex.Message}");
        }
    }

    private async Task ApplyFilter()
    {
        CardsHost.Children.Clear();
        VisibleCards.Clear();
        var activeTasks = allTasks.Where(t => !t.Completed).ToList();
        var activeCount = Math.Min(4, activeTasks.Count);

        for (var i = 0; i < activeCount; i++)
        {
            var task = activeTasks[i];
            var card = CreateTaskCard(task);
            CardsHost.Children.Add(card);
            VisibleCards.Add(card);
        }
        if (ShowCompleted)
        {
            await EnsureCompletedTasksLoadedAsync();

            var done = completedTasks!
                .ToList();

            if (done.Count > 0)
            {
                var header = new Label
                {
                    Text = "Выполненные",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(16, 24, 16, 8),
                    TextColor = (Color)Application.Current.Resources["MilkChocolate"]
                };
                CardsHost.Children.Add(header);

                foreach (var completedCard in done.Select(task => CreateTaskCard(task)))
                {
                    completedCard.Opacity = 0.7;
                    CardsHost.Children.Add(completedCard);
                    VisibleCards.Add(completedCard);
                }
            }
        }

        NoTasks = !CardsHost.Children.OfType<TaskCard>().Any();

        _ = AppLogger.Info(
            $"ApplyFilterAsync: VisibleCards = {VisibleCards.Count}, NoTasks = {NoTasks}, ShowCompleted = {ShowCompleted}");
    }

    private TaskCard CreateTaskCard(TaskModel taskModel)
    {
        var card = new TaskCard
        {
            TaskApi = taskApi,
            Item = taskModel,
            Title = taskModel.Name,
            DaysPassed = taskModel.DaysPassed(),
            Cost = taskModel.Reward,
            SwapCost = taskModel.Swap,
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
                .Where(id => id != null)
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
        await Navigation.PushAsync(new ProfilePage(userApi, loginPage, authService, taskApi));
    }
}   