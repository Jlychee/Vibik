using System.Collections.ObjectModel;
using System.Text;
using Core.Domain;
using Core.Interfaces;
using Infrastructure.Api;
using Vibik.Resources.Components;
using Task = System.Threading.Tasks.Task;
using Vibik.Utils;

namespace Vibik;

public partial class MainPage
{
    private bool taskLoaded;
    private static bool taskShouldBeChanged;
    private async Task<bool> ReloadTasksRawAsync()
    {
        var tasks = await taskApi.GetTasksAsync();
        await AppLogger.Info($"ReloadTasksRawAsync: получено задач = {tasks.Count}");

        allTasks.Clear();
        allTasks.AddRange(tasks);
        return true;
    }

    private readonly ITaskApi taskApi;
    private readonly List<TaskModel> allTasks = [];
    private readonly IUserApi userApi;
    private readonly LoginPage loginPage;
    private readonly IWeatherApi weatherApi;
    private readonly IAuthService authService;
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private CancellationTokenSource? refreshCts;
    private bool isVisiblePage;


    private int money;
    private int level;
    private ImageSource? weatherImage;
    private ObservableCollection<View> VisibleCards { get; } = new();
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        isVisiblePage = false;
    }

    public ImageSource? WeatherImage
    {
        get => weatherImage;
        set { weatherImage = value; OnPropertyChanged(); }
    }

    private string weatherTemp = "—";
    private string weatherInfoAboutSky = "Загружаем погоду...";
    private string weatherInfoAboutFallout = string.Empty;
    private WeatherInfo? lastWeather;

    public int Level
    {
        get => level;
        set { level = value; OnPropertyChanged(); }
    }

    public int Money
    {
        get => money;
        set { money = value; OnPropertyChanged(); }
    }

    public string WeatherTemp
    {
        get => weatherTemp;
        set { weatherTemp = value; OnPropertyChanged(); }
    }

    public string WeatherInfoAboutSky
    {
        get => weatherInfoAboutSky;
        set { weatherInfoAboutSky = value; OnPropertyChanged(); }
    }

    public string WeatherInfoAboutFallout
    {
        get => weatherInfoAboutFallout;
        set { weatherInfoAboutFallout = value; OnPropertyChanged(); }
    }

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

    public MainPage(
        ITaskApi taskApi,
        IUserApi userApi,
        LoginPage loginPage,
        IWeatherApi weatherApi,
        IAuthService authService)
    {
        InitializeComponent();
        BindingContext = this;
        this.taskApi = taskApi;
        this.userApi = userApi;
        this.weatherApi = weatherApi;
        this.loginPage = loginPage;
        this.authService = authService;
        AppEventHub.RefreshRequested -= OnRefreshRequested;
        AppEventHub.RefreshRequested += OnRefreshRequested;

    }
    private void OnRefreshRequested(AppRefreshReason reason)
    {
        refreshCts?.Cancel();
        var cts = refreshCts = new CancellationTokenSource();

        if (!isVisiblePage)
        {
            MarkTaskShouldBeChanged();
            return;
        }

        MarkTaskShouldBeChanged();

        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await Task.Delay(150, cts.Token);
                await RefreshFromEventAsync(reason, cts.Token);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                await AppLogger.Error("OnRefreshRequested: ошибка обновления", ex);
            }
        });
    }


    private async Task RefreshFromEventAsync(AppRefreshReason reason, CancellationToken ct)
    {
        if (!await EnsureAuthorizedAsync())
            return;

        await refreshGate.WaitAsync(ct);
        try
        {
            await AppLogger.Info($"RefreshFromEventAsync: reason={reason}");
            taskShouldBeChanged = true;
            taskLoaded = false;

            var userTask = LoadUserAsync();
            var weatherTask = LoadWeatherAsync();
            var tasksTask = LoadTasksAsync();

            await Task.WhenAll(userTask, weatherTask, tasksTask);
        }
        finally
        {
            refreshGate.Release();
        }
    }

    public static void MarkTaskShouldBeChanged() => taskShouldBeChanged = true;

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
        var description = weather.Description;
        var sb = new StringBuilder(description);
        sb[0] = char.ToUpper(sb[0]);
        description = sb.ToString();
        WeatherInfoAboutSky = string.IsNullOrWhiteSpace(description)
            ? weather.Condition
            : description;
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
        isVisiblePage = true;

        await refreshGate.WaitAsync();
        try
        {
            if (!await EnsureAuthorizedAsync())
            {
                await AppLogger.Warn("пользователь не авторизован");
                await Navigation.PushModalAsync(new NavigationPage(loginPage));
                return;
            }

            var userTask = LoadUserAsync();
            var weatherTask = LoadWeatherAsync();

            Task tasksTask;
            if (!taskLoaded || taskShouldBeChanged)
            {
                await AppLogger.Info($"OnAppearing: загрузка задач, taskLoaded={taskLoaded}, taskShouldBeChanged={taskShouldBeChanged}");
                taskLoaded = true;
                taskShouldBeChanged = false;
                tasksTask = LoadTasksAsync();
            }
            else
            {
                await AppLogger.Info("OnAppearing: обновляем только статусы модерации");
                tasksTask = RefreshModerationStatusesAsync();
            }

            await Task.WhenAll(userTask, weatherTask, tasksTask);
        }
        finally
        {
            refreshGate.Release();
        }
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
            await ReloadTasksRawAsync();
            await RefreshModerationStatusesAsync();
        }
        catch (Exception ex)
        {
            await AppAlerts.ProfileUploadFailed(ex.Message);
            VisibleCards.Clear();
            CardsHost.Children.Clear();
            NoTasks = true;
        }
    }

    private readonly Dictionary<int, ModerationStatus> lastKnownModerationStatuses = new();

    private async Task RefreshModerationStatusesAsync()
{
    await AppLogger.Info("RefreshModerationStatusesAsync: старт");

    var snapshot = allTasks.ToArray();

    var needReloadUser = false;
    var needReloadTasks = false;

    foreach (var task in snapshot)
    {
        string? statusString;
        try
        {
            statusString = await taskApi.GetModerationStatusAsync(task.UserTaskId.ToString());
        }
        catch (Exception ex)
        {
            await AppLogger.Warn($"Не удалось получить статус модерации для userTaskId={task.UserTaskId}: {ex.Message}");
            continue;
        }

        var newStatus = ModerationStatusService.MapModeration(statusString);

        lastKnownModerationStatuses.TryGetValue(task.UserTaskId, out var oldStatus);
        lastKnownModerationStatuses[task.UserTaskId] = newStatus;

        if (newStatus != oldStatus)
        {
            await AppLogger.Info($"Статус userTaskId={task.UserTaskId} изменился: {oldStatus} -> {newStatus}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                switch (newStatus)
                {
                    case ModerationStatus.Approved:
                        task.Completed = true;
                        ShowModerationBanner($"Задание \"{task.Name}\" одобрено! Награда начислена.", Color.FromArgb("#D9F8D9"));
                        break;

                    case ModerationStatus.Rejected:
                        ShowModerationBanner($"Задание \"{task.Name}\" отклонено. Попробуйте ещё раз.", Color.FromArgb("#FFE0E0"));
                        break;
                }
            });

            _ = HighlightCardAsync(task);

            if (newStatus is ModerationStatus.Approved or ModerationStatus.Rejected)
            {
                needReloadUser = true;
                needReloadTasks = true;
            }
        }

        task.ModerationStatus = newStatus;
    }

    if (needReloadUser)
        await LoadUserAsync();

    if (needReloadTasks)
    {
        completedTasks = null;
        await ReloadTasksRawAsync();
        await ApplyFilter();
        return;
    }

    await ApplyFilter();
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

        var usedIds = new HashSet<int>();
        var activeTasks = allTasks
            .Where(t => !t.Completed && t.ModerationStatus != ModerationStatus.Pending)
            .ToList();

        if (activeTasks.Count < 4)
        {
            await AppLogger.Info(
                $"ApplyFilter: активных задач меньше 4: {activeTasks.Count}. " +
                "Сервер не выдал достаточно активных задач для заполнения всех слотов.");
        }

        var activeCount = Math.Min(4, activeTasks.Count);

        for (var i = 0; i < activeCount; i++)
        {
            var task = activeTasks[i];
            var card = CreateTaskCard(task);
            CardsHost.Children.Add(card);
            VisibleCards.Add(card);
            usedIds.Add(task.UserTaskId);
        }

        var pending = allTasks
            .Where(t => t.ModerationStatus == ModerationStatus.Pending &&
                        !usedIds.Contains(t.UserTaskId))
            .ToList();

        if (pending.Count > 0)
        {
            var header = new Label
            {
                Text = "Ожидают модерации",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(16, 24, 16, 8),
                TextColor = (Color)Application.Current!.Resources["MilkChocolate"]
            };
            CardsHost.Children.Add(header);

            foreach (var t in pending)
            {
                var card = CreateTaskCard(t);

                card.Title = $"⏳ {t.Name}";
                card.BackgroundColor = (Color)Application.Current!.Resources["LightYelloww"];
                card.Opacity = 1.0;
                card.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await OpenTaskDetailsAsync(t))
                });

                card.RefreshCommand = new Command(async () =>
                {
                    await DisplayAlert(
                        "На модерации",
                        "Это задание уже отправлено на модерацию и не может быть заменено, пока идёт проверка.",
                        "OK");
                });

                CardsHost.Children.Add(card);
                VisibleCards.Add(card);
                usedIds.Add(t.UserTaskId);
            }
        }

        if (ShowCompleted)
        {
            await EnsureCompletedTasksLoadedAsync();

            var done = completedTasks!
                .Concat(allTasks.Where(t => t.Completed))
                .GroupBy(t => t.UserTaskId)
                .Select(g => g.First())
                .Where(t => !usedIds.Contains(t.UserTaskId))
                .ToList();

            if (done.Count > 0)
            {
                var header = new Label
                {
                    Text = "Выполненные",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(16, 24, 16, 8),
                    TextColor = (Color)Application.Current!.Resources["MilkChocolate"]
                };
                CardsHost.Children.Add(header);

                foreach (var task in done)
                {
                    var completedCard = CreateTaskCard(task);
                    completedCard.Opacity = 0.7;
                    switch (task.ModerationStatus)
                    {
                        case ModerationStatus.Approved:
                            completedCard.Title = $"✅ {task.Name}";
                            completedCard.BackgroundColor = Color.FromArgb("#D9F8D9");
                            break;

                        case ModerationStatus.Rejected:
                            completedCard.Title = $"❌ {task.Name}";
                            completedCard.BackgroundColor = Color.FromArgb("#FFE0E0");
                            break;

                        default:
                            completedCard.BackgroundColor = Color.FromArgb("#F2F2F2");
                            break;
                    }
                    CardsHost.Children.Add(completedCard);
                    VisibleCards.Add(completedCard);
                    usedIds.Add(task.UserTaskId);
                    completedCard.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(async () => await OpenTaskDetailsAsync(task))
                    });

                }
            }
        }

        NoTasks = !CardsHost.Children.OfType<TaskCard>().Any();

        _ = AppLogger.Info(
            $"ApplyFilter: VisibleCards = {VisibleCards.Count}, NoTasks = {NoTasks}, " +
            $"PendingCount = {pending.Count}, ShowCompleted = {ShowCompleted}");
    }
    private async Task HighlightCardAsync(TaskModel taskModel)
    {
        if (taskModel == null)
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var card = CardsHost.Children
                .OfType<TaskCard>()
                .FirstOrDefault(c => c.Item?.UserTaskId == taskModel.UserTaskId);

            if (card == null)
                return;

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    await card.ScaleTo(1.03, 80, Easing.CubicOut);
                    await card.ScaleTo(1.0, 80, Easing.CubicIn);
                }
            }
            catch
            {
            }
        });
    }

    private async Task OpenTaskDetailsAsync(TaskModel taskModel)
    {
        if (taskModel == null)
            return;

        try
        {
            taskModel.ExtendedInfo ??= new TaskModelExtendedInfo();
            taskModel.ExtendedInfo.UserPhotos ??= new List<Uri>();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(new TaskDetailsPage(taskModel, taskApi));
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть задание: {ex.Message}", "OK");
            });
            await AppLogger.Error($"OpenTaskDetailsAsync error: {ex}");
        }
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
        var hasMoneyForSwap = Money >= taskModel.Swap;
        card.CoinsColor = hasMoneyForSwap
            ? Color.FromArgb("#42AD91")
            : Color.FromArgb("#F37E6C");
        card.RefreshCommand = new Command(async () =>
        {
            var current = card.Item;
            if (current is null)
                return;

            var confirmed = await AppAlerts.ChangeTask(card.SwapCost);
            if (!confirmed)
                return;

            try
            {
                var oldTask = current;
                var oldId = oldTask.UserTaskId;

                var candidate = await taskApi.SwapTaskAsync(oldId.ToString());
                if (candidate is null)
                {
                    await AppAlerts.NoNewTasks();
                    return;
                }

                TaskPhotosCleaner.CleanupTaskPhotos(oldTask);
                TaskPhotosCleaner.DeleteTaskFolder(oldId); 

                card.Item = candidate;
                card.Title = candidate.Name ?? "Задание";
                card.DaysPassed = candidate.DaysPassed();
                card.Cost = candidate.Reward;
                card.SwapCost = candidate.Swap;

                var enoughNow = Money >= candidate.Swap;
                card.CoinsColor = enoughNow
                    ? Color.FromArgb("#2E7D32")
                    : Color.FromArgb("#C62828");

                MarkTaskShouldBeChanged();
                AppEventHub.RequestRefresh(AppRefreshReason.TaskSwapped);
            }
            catch (HttpRequestException ex)
            {
                await AppLogger.Warn($"SwapTask: HTTP ошибка: {ex.Message}");
                await DisplayAlert("Ошибка сети", "Не удалось заменить задание. Проверь интернет и попробуй снова.", "OK");
            }
            catch (Exception ex)
            {
                await AppLogger.Error("SwapTask: ошибка", ex);
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
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
    private bool isModerationBannerVisible;
    public bool IsModerationBannerVisible
    {
        get => isModerationBannerVisible;
        set
        {
            if (isModerationBannerVisible == value) return;
            isModerationBannerVisible = value;
            OnPropertyChanged();
        }
    }

    private string moderationBannerText = string.Empty;
    public string ModerationBannerText
    {
        get => moderationBannerText;
        set
        {
            if (moderationBannerText == value) return;
            moderationBannerText = value;
            OnPropertyChanged();
        }
    }
    private void ShowModerationBanner(string text, Color background)
    {
        ModerationBannerText = text;
        ModerationBannerColor = background;
        IsModerationBannerVisible = true;
    }

    private Color moderationBannerColor = Colors.Transparent;
    public Color ModerationBannerColor
    {
        get => moderationBannerColor;
        set
        {
            if (moderationBannerColor == value) return;
            moderationBannerColor = value;
            OnPropertyChanged();
        }
    }

    private void OnCloseModerationBannerClicked(object sender, EventArgs e)
    {
        IsModerationBannerVisible = false;
    }
}
