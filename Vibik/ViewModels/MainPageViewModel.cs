using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Core.Domain;
using Core.Interfaces;
using Infrastructure.Api;
using Vibik.Alerts;
using Vibik.Resources.Components;
using Vibik.Utils;
using Vibik.Utils.Weather;
using Task = System.Threading.Tasks.Task;

namespace Vibik;

public interface IMainPageView
{
    INavigation Navigation { get; }
    Layout CardsHost { get; }
    Task DisplayAlert(string title, string message, string cancel);
}

public sealed class MainPageViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool taskLoaded;
    private static bool taskShouldBeChanged;

    private readonly IMainPageView view;
    private readonly ITaskApi taskApi;
    private readonly IUserApi userApi;
    private readonly LoginPage loginPage;
    private readonly IWeatherApi weatherApi;
    private readonly IAuthService authService;

    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private CancellationTokenSource? refreshCts;

    private bool isVisiblePage;

    private readonly List<TaskModel> allTasks = [];
    private readonly Dictionary<int, ModerationStatus> lastKnownModerationStatuses = new();
    private readonly HashSet<int> finalNotified = new();
    private readonly Dictionary<int, string> trackedPending = new();

    public ObservableCollection<string> Items { get; } = new();
    private ObservableCollection<View> VisibleCards { get; } = new();

    public ICommand RefreshCommand { get; }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(); }
    }

    private int money;
    public int Money
    {
        get => money;
        set { money = value; OnPropertyChanged(); }
    }

    private int level;
    public int Level
    {
        get => level;
        set { level = value; OnPropertyChanged(); }
    }

    private ImageSource? weatherImage;
    public ImageSource? WeatherImage
    {
        get => weatherImage;
        set { weatherImage = value; OnPropertyChanged(); }
    }

    private string weatherTemp = "—";
    public string WeatherTemp
    {
        get => weatherTemp;
        set { weatherTemp = value; OnPropertyChanged(); }
    }

    private string weatherInfoAboutSky = "Загружаем погоду...";
    public string WeatherInfoAboutSky
    {
        get => weatherInfoAboutSky;
        set { weatherInfoAboutSky = value; OnPropertyChanged(); }
    }

    private string weatherInfoAboutFallout = string.Empty;
    public string WeatherInfoAboutFallout
    {
        get => weatherInfoAboutFallout;
        set { weatherInfoAboutFallout = value; OnPropertyChanged(); }
    }

    private WeatherInfo? lastWeather;

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

    public MainPageViewModel(
        IMainPageView view,
        ITaskApi taskApi,
        IUserApi userApi,
        LoginPage loginPage,
        IWeatherApi weatherApi,
        IAuthService authService)
    {
        this.view = view;
        this.taskApi = taskApi;
        this.userApi = userApi;
        this.weatherApi = weatherApi;
        this.loginPage = loginPage;
        this.authService = authService;

        RefreshCommand = new Command(async () => await PullToRefreshAsync());

        AppEventHub.RefreshRequested -= OnRefreshRequested;
        AppEventHub.RefreshRequested += OnRefreshRequested;
    }

    public void OnDisappearing()
    {
        isVisiblePage = false;
    }

    public async Task OnAppearingAsync()
    {
        isVisiblePage = true;

        await refreshGate.WaitAsync();
        try
        {
            if (!await EnsureAuthorizedAsync())
            {
                await AppLogger.Warn("пользователь не авторизован");
                await view.Navigation.PushModalAsync(new NavigationPage(loginPage));
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

    public static void MarkTaskShouldBeChanged() => taskShouldBeChanged = true;

    private async Task PullToRefreshAsync()
    {
        refreshCts?.Cancel();
        var cts = refreshCts = new CancellationTokenSource();
        var ct = cts.Token;

        try
        {
            IsRefreshing = true;
            await Infrastructure.Utils.AppLogger.Info("Refreshing");
            await RefreshFromEventAsync(AppRefreshReason.Any, ct);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await AppLogger.Error("PullToRefreshAsync: ошибка", ex);
        }
        finally
        {
            await AppLogger.Info("Refreshing is done");
            IsRefreshing = false;
        }
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

    private async Task LoadUserAsync()
    {
        var userId = ResolveCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await AppLogger.Warn("LoadUserAsync: userId пуст, показываем экран логина");
            await view.Navigation.PushModalAsync(new NavigationPage(loginPage));
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
            await view.DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
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
            view.CardsHost.Children.Clear();
            NoTasks = true;
        }
    }

    private async Task RefreshModerationStatusesAsync()
    {
        await AppLogger.Info("RefreshModerationStatusesAsync: старт ");
        await EnsureCompletedTasksLoadedAsync(force: true);

        var completedIndex = completedTasks!
            .GroupBy(t => t.UserTaskId)
            .ToDictionary(g => g.Key, g => g.First());

        var snapshot = allTasks.ToArray();
        var snapshotIds = new HashSet<int>(snapshot.Select(t => t.UserTaskId));

        var needReloadUser = false;
        var needReloadTasks = false;

        foreach (var t in snapshot.Where(x => x.ModerationStatus == ModerationStatus.Pending))
            trackedPending[t.UserTaskId] = t.Name ?? "Задание";

        foreach (var task in snapshot)
        {
            var id = task.UserTaskId;

            await AppLogger.Info($"trackedPending.Contains({id})={trackedPending.ContainsKey(id)}, finalNotified.Contains({id})={finalNotified.Contains(id)}");
            await AppLogger.Info($"RefreshModerationStatusesAsync для задачи:{task.Name} ");

            string? statusString;
            try
            {
                statusString = await taskApi.GetModerationStatusAsync(id.ToString());
                await AppLogger.Info($"raw moderation status ({id}) = '{statusString ?? "<null>"}'");
            }
            catch (Exception ex)
            {
                await AppLogger.Warn($"Не удалось получить статус модерации для userTaskId={id}: {ex.Message}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(statusString))
            {
                await AppLogger.Warn($"Статус пуст для {id}. Оставляем {task.ModerationStatus}");
                continue;
            }

            var mapped = ModerationStatusConvertor.MapModeration(statusString);

            if (mapped == ModerationStatus.None)
            {
                await AppLogger.Warn($"Неизвестный статус '{statusString}' для {id}. Оставляем {task.ModerationStatus}");
                continue;
            }

            if (mapped == ModerationStatus.Pending)
            {
                finalNotified.Remove(id);
                trackedPending[id] = task.Name ?? "Задание";
            }

            await AppLogger.Info($"new ststus moder status: {mapped}");

            lastKnownModerationStatuses.TryGetValue(id, out var oldStatus);
            lastKnownModerationStatuses[id] = mapped;

            await AppLogger.Info($"old ststus moder status: {oldStatus}");

            var isFinal = mapped is ModerationStatus.Approved or ModerationStatus.Rejected;
            await AppLogger.Info($"isFinal ststus moder status: {isFinal}");

            var shouldNotify = isFinal && trackedPending.ContainsKey(id) && !finalNotified.Contains(id);
            await AppLogger.Info($"shouldNotify moder status: {shouldNotify}");

            task.ModerationStatus = mapped;
            if (mapped == ModerationStatus.Approved)
                task.Completed = true;

            if (shouldNotify)
            {
                finalNotified.Add(id);
                trackedPending.Remove(id);

                if (mapped == ModerationStatus.Approved)
                    ShowModerationBanner($"Задание \"{task.Name}\" одобрено! Награда начислена.", Color.FromArgb("#D9F8D9"));
                else
                    ShowModerationBanner($"Задание \"{task.Name}\" отклонено. Попробуйте ещё раз.", Color.FromArgb("#FFE0E0"));

                _ = HighlightCardAsync(task);

                needReloadUser = true;
                needReloadTasks = true;
            }
            else if (mapped != oldStatus && isFinal)
            {
                needReloadUser = true;
                needReloadTasks = true;
            }
        }

        foreach (var kv in trackedPending.ToArray())
        {
            var id = kv.Key;

            if (completedIndex.TryGetValue(id, out var done))
            {
                if (!finalNotified.Contains(id))
                {
                    finalNotified.Add(id);
                    trackedPending.Remove(id);

                    var st = done.ModerationStatus;
                    if (st == ModerationStatus.Approved)
                        ShowModerationBanner($"Задание \"{done.Name}\" одобрено! Награда начислена.", Color.FromArgb("#D9F8D9"));
                    else if (st == ModerationStatus.Rejected)
                        ShowModerationBanner($"Задание \"{done.Name}\" отклонено. Попробуйте ещё раз.", Color.FromArgb("#FFE0E0"));
                    else
                        ShowModerationBanner($"Задание \"{done.Name}\" одобрено! Награда начислена.", Color.FromArgb("#D9F8D9"));
                }

                needReloadUser = true;
                needReloadTasks = true;
                continue;
            }

            if (snapshotIds.Contains(id))
                continue;

            string? statusString;
            try
            {
                statusString = await taskApi.GetModerationStatusAsync(id.ToString());
            }
            catch
            {
                continue;
            }

            var newStatus = ModerationStatusConvertor.MapModeration(statusString);
            if (newStatus == ModerationStatus.Pending)
            {
                finalNotified.Remove(id);
                continue;
            }

            var isFinal = newStatus is ModerationStatus.Approved or ModerationStatus.Rejected;
            if (!isFinal || finalNotified.Contains(id))
                continue;

            finalNotified.Add(id);
            trackedPending.Remove(id);

            if (newStatus == ModerationStatus.Approved)
                ShowModerationBanner($"Задание \"{kv.Value}\" одобрено! Награда начислена.", Color.FromArgb("#D9F8D9"));
            else
                ShowModerationBanner($"Задание \"{kv.Value}\" отклонено. Попробуйте ещё раз.", Color.FromArgb("#FFE0E0"));

            needReloadUser = true;
            needReloadTasks = true;
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

    private async Task EnsureCompletedTasksLoadedAsync(bool force = false)
    {
        if (!force && completedTasks != null)
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

    private async Task<bool> ReloadTasksRawAsync()
    {
        var tasks = await taskApi.GetTasksAsync();
        await AppLogger.Info($"ReloadTasksRawAsync: получено задач = {tasks.Count}");

        allTasks.Clear();
        allTasks.AddRange(tasks);

        foreach (var t in allTasks)
        {
            if (lastKnownModerationStatuses.TryGetValue(t.UserTaskId, out var known))
            {
                t.ModerationStatus = known;

                if (known == ModerationStatus.Approved)
                    t.Completed = true;
            }
        }

        return true;
    }

    private async Task ApplyFilter()
    {
        view.CardsHost.Children.Clear();
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
            view.CardsHost.Children.Add(card);
            VisibleCards.Add(card);
            usedIds.Add(task.UserTaskId);
        }

        var pending = allTasks
            .Where(t => t.ModerationStatus == ModerationStatus.Pending &&
                        !usedIds.Contains(t.UserTaskId))
            .ToList();

        foreach (var t in pending)
            trackedPending[t.UserTaskId] = t.Name ?? "Задание";

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
            view.CardsHost.Children.Add(header);

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
                    await view.DisplayAlert(
                        "На модерации",
                        "Это задание уже отправлено на модерацию и не может быть заменено, пока идёт проверка.",
                        "OK");
                });

                view.CardsHost.Children.Add(card);
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
                view.CardsHost.Children.Add(header);

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

                    view.CardsHost.Children.Add(completedCard);
                    VisibleCards.Add(completedCard);
                    usedIds.Add(task.UserTaskId);

                    completedCard.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(async () => await OpenTaskDetailsAsync(task))
                    });
                }
            }
        }

        NoTasks = !view.CardsHost.Children.OfType<TaskCard>().Any();

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
            var card = view.CardsHost.Children
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
                await view.Navigation.PushAsync(new TaskDetailsPage(taskModel, taskApi));
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await view.DisplayAlert("Ошибка", $"Не удалось открыть задание: {ex.Message}", "OK");
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
                await view.DisplayAlert("Ошибка сети", "Не удалось заменить задание. Проверь интернет и попробуй снова.", "OK");
            }
            catch (Exception ex)
            {
                await AppLogger.Error("SwapTask: ошибка", ex);
                await view.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        });

        return card;
    }

    private void ShowModerationBanner(string text, Color background)
    {
        void Apply()
        {
            IsModerationBannerVisible = false;
            ModerationBannerText = text;
            ModerationBannerColor = background;
            IsModerationBannerVisible = true;

            _ = AppLogger.Info($"BANNER: {text}");
        }

        MainThread.BeginInvokeOnMainThread(Apply);
    }

    public void CloseModerationBanner()
    {
        IsModerationBannerVisible = false;
    }

    public async Task NavigateToMapAsync()
        => await view.Navigation.PushAsync(new MapPage());

    public async Task NavigateHomeAsync()
    {
        if (view.Navigation.NavigationStack.Count > 1)
            await view.Navigation.PopToRootAsync();
    }

    public async Task NavigateToProfileAsync()
        => await view.Navigation.PushAsync(new ProfilePage(userApi, loginPage, authService, taskApi));
}
