using Core.Domain;
using Core.Interfaces;
using Vibik.Utils;
using Task = System.Threading.Tasks.Task;

namespace Vibik;

public partial class TaskDetailsPage
{
    private const int TileSize = 120;
    private const int Columns = 3;
    private const double CellGap = 24;

    private readonly TaskModel taskModel;
    private readonly ITaskApi taskApi;

    private bool isCompletedView;

    private TaskModelExtendedInfo ExtendedInfo
    {
        get
        {
            if (taskModel.ExtendedInfo == null)
                taskModel.ExtendedInfo = new TaskModelExtendedInfo();

            taskModel.ExtendedInfo.UserPhotos ??= new List<Uri>();
            return taskModel.ExtendedInfo;
        }
    }

    public TaskDetailsPage(TaskModel taskModel, ITaskApi taskApi)
    {
        InitializeComponent();

        this.taskModel = taskModel ?? throw new ArgumentNullException(nameof(taskModel));
        this.taskApi = taskApi ?? throw new ArgumentNullException(nameof(taskApi));

        isCompletedView =
            taskModel.Completed ||
            taskModel.ModerationStatus is ModerationStatus.Approved or ModerationStatus.Rejected;

        _ = ExtendedInfo;

        BindingContext = new TaskDetailsViewModel(this.taskModel);
        if (BindingContext is TaskDetailsViewModel vm)
        {
            vm.IsCompletedView = isCompletedView;
            vm.ModerationStatus = taskModel.ModerationStatus;
        }

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await EnsureExtendedInfoLoadedAsync();
        if (!isCompletedView)
        {
            LoadSavedPhotos();
        }
        BuildPhotosGrid();
    }

    private async Task EnsureExtendedInfoLoadedAsync()
    {
        try
        {
            var full = await taskApi.GetTaskAsync(taskModel.UserTaskId.ToString());

            if (full != null)
            {
                taskModel.Name             = full.Name;
                taskModel.Reward           = full.Reward;
                taskModel.Completed        = full.Completed;
                taskModel.ModerationStatus = full.ModerationStatus;
                taskModel.TaskId           = full.TaskId;
                
                if (full.ExtendedInfo != null)
                {
                    taskModel.ExtendedInfo = full.ExtendedInfo;
                    taskModel.ExtendedInfo.UserPhotos ??= new List<Uri>();
                }

                await AppLogger.Info(
                    $"EnsureExtendedInfoLoadedAsync: userTaskId={taskModel.UserTaskId}, " +
                    $"desc='{taskModel.ExtendedInfo?.Description}', " +
                    $"photos={taskModel.ExtendedInfo?.UserPhotos?.Count ?? 0}");
            }
            else
            {
                await AppLogger.Warn(
                    $"EnsureExtendedInfoLoadedAsync: сервер вернул null для userTaskId={taskModel.UserTaskId}");
            }
        }
        catch (Exception ex)
        {
            await AppLogger.Warn(
                $"EnsureExtendedInfoLoadedAsync: не удалось получить данные с бэка для userTaskId={taskModel.UserTaskId}: {ex.Message}");
        }

        _ = ExtendedInfo;

        isCompletedView =
            taskModel.Completed ||
            taskModel.ModerationStatus is ModerationStatus.Approved or ModerationStatus.Rejected;

        BindingContext = new TaskDetailsViewModel(taskModel);
    }

    private async void OnSendClick(object? sender, EventArgs e)
    {
        var vm = BindingContext as TaskDetailsViewModel;

        if (isCompletedView || taskModel.ModerationStatus == ModerationStatus.Pending)
        {
            await DisplayAlert("На модерации", "Это задание уже отправлено и сейчас проверяется.", "OK");
            return;
        }
        
        try
        {
            var serverRaw = await taskApi.GetModerationStatusAsync(taskModel.UserTaskId.ToString());
            var serverStatus = MapModeration(serverRaw);

            taskModel.ModerationStatus = serverStatus;
            if (vm != null) vm.ModerationStatus = serverStatus;

            if (serverStatus == ModerationStatus.Pending)
            {
                await DisplayAlert("На модерации", "Это задание уже отправлено и сейчас проверяется.", "OK");
                return;
            }

            if (serverStatus is ModerationStatus.Approved)
            {
                await DisplayAlert("Уже проверено", "Это задание уже прошло модерацию, повторно отправлять не нужно.", "OK");
                return;
            }

            var required = ExtendedInfo.PhotosRequired;

            var localPaths = ExtendedInfo.UserPhotos
                .Select(GetPathFromUri)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Where(IsLocalPath)
                .Where(File.Exists)
                .ToList();

            var count = localPaths.Count;

            await AppLogger.Info(
                $"TaskDetailsPage.OnSendClick: required={required}, localPhotos={count}");

            if (required > 0 && count < required)
            {
                await DisplayAlert(
                    "Недостаточно фото",
                    $"Нужно минимум {required}. Сейчас: {count}.",
                    "OK");
                return;
            }

            if (count == 0)
            {
                await DisplayAlert("Нет фото", "Добавьте хотя бы одно фото", "OK");
                return;
            }
            if (vm != null) vm.IsSending = true;

            var ok = await taskApi.SubmitAsync(taskModel.UserTaskId.ToString(), localPaths);
            if (!ok)
            {
                await DisplayAlert(
                    "Ошибка",
                    "Не удалось отправить задание. Проверьте сеть и попробуйте ещё раз.",
                    "OK");
                return;
            }

            taskModel.ModerationStatus = ModerationStatus.Pending;
            MainPage.MarkTaskShouldBeChanged();

            MainPage.MarkTaskShouldBeChanged();
            AppEventHub.RequestRefresh(AppRefreshReason.TaskSent);

            await DisplayAlert(
                "Отправлено",
                "Задание отправлено на модерацию. Мы сообщим, когда оно будет проверено.",
                "OK");

            BuildPhotosGrid();
            await Navigation.PopAsync();
        }
        catch (HttpRequestException ex)
        {
            await AppLogger.Warn($"Сетевая ошибка при отправке задания: {ex.Message}");
            await DisplayAlert(
                "Ошибка сети",
                "Не удалось связаться с сервером. Проверьте интернет и попробуйте ещё раз.",
                "OK");
        }
        catch (Exception ex)
        {
            await AppLogger.Error(ex.ToString());
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
        finally
        {
            if (vm != null) vm.IsSending = false;
        }
    }
    private static ModerationStatus MapModeration(string? statusString)
    {
        var normalized = statusString?.Trim().Trim('"').ToLowerInvariant();
        return normalized switch
        {
            "waiting" => ModerationStatus.Pending,
            "default" => ModerationStatus.None,
            "approved" or "approve" or "success" => ModerationStatus.Approved,
            "rejected" or "reject" or "failed" => ModerationStatus.Rejected,
            _ => ModerationStatus.None
        };
    }

    private void BuildPhotosGrid()
    {
        PhotoShotsGrid.Children.Clear();
        PhotoShotsGrid.ColumnDefinitions.Clear();
        PhotoShotsGrid.RowDefinitions.Clear();

        for (var c = 0; c < Columns; c++)
            PhotoShotsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        PhotoShotsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        PhotoShotsGrid.ColumnSpacing = CellGap;
        PhotoShotsGrid.RowSpacing = CellGap;

        PhotoShotsGrid.HorizontalOptions = LayoutOptions.Center;
        PhotoShotsGrid.WidthRequest = Columns * TileSize + (Columns - 1) * CellGap;

        var paths = ExtendedInfo.UserPhotos
            .Select(GetPathFromUri)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();

        var required = ExtendedInfo.PhotosRequired;
        var totalSlots = required > 0 ? required : Math.Max(paths.Count + 1, 1);

        var rows = (int)Math.Ceiling(totalSlots / (double)Columns);
        for (var r = 0; r < rows; r++)
            PhotoShotsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var i = 0;

        foreach (var path in paths.Take(totalSlots))
            AddPhotoTile(path, i++);

        if (isCompletedView) return;
        for (; i < totalSlots; i++)
            AddAddTile(i);
    }

    private void LoadSavedPhotos()
    {
        var saved = PhotoService.GetSavedPhotos(TaskKey);
        ExtendedInfo.UserPhotos.RemoveAll(p =>
        {
            var local = GetPathFromUri(p);
            if (string.IsNullOrWhiteSpace(local)) return false;
            if (!IsLocalPath(local)) return false;
            return !File.Exists(local);
        });

        var existing = ExtendedInfo.UserPhotos
            .Select(GetPathFromUri)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in saved.Where(File.Exists))
        {
            if (existing.Add(path))
                ExtendedInfo.UserPhotos.Add(new Uri(path));
        }
    }

    private void AddPhotoTile(string path, int index)
    {
        var r = index / Columns;
        var c = index % Columns;

        var tile = new Resources.Components.PhotoTile
        {
            PathOrUrl = path,
            TileSize = TileSize,
            IsAddTile = false
        };
        tile.PhotoTapped += async (_, p) =>
        {
            if (!string.IsNullOrWhiteSpace(p))
                await OnPhotoTappedCore(p);
        };

        Grid.SetRow(tile, r);
        Grid.SetColumn(tile, c);
        PhotoShotsGrid.Children.Add(tile);
    }

    private void AddAddTile(int index)
    {
        var r = index / Columns;
        var c = index % Columns;

        var add = new Resources.Components.PhotoTile
        {
            IsAddTile = true,
            TileSize = TileSize
        };
        add.AddTapped += OnAddPhotoTapped;

        Grid.SetRow(add, r);
        Grid.SetColumn(add, c);
        PhotoShotsGrid.Children.Add(add);
    }

    private async Task OnPhotoTappedCore(string path)
    {
        var actions = isCompletedView
            ? new[] { "Просмотреть" }
            : new[] { "Просмотреть", "Заменить", "Удалить" };

        var choice = await DisplayActionSheet("Фото", "Отмена", null, actions);
        if (string.IsNullOrEmpty(choice) || choice == "Отмена") return;

        switch (choice)
        {
            case "Просмотреть":
                await ShowPreviewAsync(path);
                break;

            case "Заменить" when !isCompletedView:
            {
                var newFile = await PhotoService.PickOrCaptureAsync(this);
                if (newFile == null) return;

                var newPath = await PhotoService.SaveFileResultAsync(newFile, TaskKey);
                var idx = ExtendedInfo.UserPhotos.FindIndex(p => GetPathFromUri(p) == path);
                if (idx >= 0)
                {
                    TryDeleteLocal(GetPathFromUri(ExtendedInfo.UserPhotos[idx]));

                    ExtendedInfo.UserPhotos[idx] = new Uri(newPath);
                    BuildPhotosGrid();
                }
                break;
            }

            case "Удалить" when !isCompletedView:
            {
                var ok = await DisplayAlert(
                    "Удалить фото?",
                    "Это действие необратимо.",
                    "Удалить",
                    "Отмена");
                if (!ok) return;

                var removed = ExtendedInfo.UserPhotos.RemoveAll(p => GetPathFromUri(p) == path) > 0;
                if (removed) TryDeleteLocal(path);
                BuildPhotosGrid();
                break;
            }
        }
    }

    private async void OnAddPhotoTapped(object? sender, EventArgs e)
    {
        if (isCompletedView)
        {
            await DisplayAlert("Нельзя добавить фото",
                "Задание уже завершено. Добавлять новые фото нельзя.",
                "OK");
            return;
        }

        try
        {
            var current = ExtendedInfo.UserPhotos?.Count ?? 0;
            if (ExtendedInfo.PhotosRequired > 0 && current >= ExtendedInfo.PhotosRequired)
            {
                await DisplayAlert("Лимит", "Достигнуто максимальное количество фото.", "OK");
                return;
            }

            var file = await PhotoService.PickOrCaptureAsync(this);
            if (file == null) return;

            var savedPath = await PhotoService.SaveFileResultAsync(file, TaskKey);

            ExtendedInfo.UserPhotos!.Add(new Uri(savedPath));
            BuildPhotosGrid();
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Не поддерживается", "Медиа-пикер недоступен на этом устройстве.", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Нет доступа", "Разрешите необходимые разрешения в настройках.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private string TaskKey =>
        taskModel.UserTaskId != 0
            ? taskModel.UserTaskId.ToString()
            : taskModel.TaskId;
    
    private static string GetPathFromUri(Uri uri)
    {
        if (uri == null)
            return string.Empty;

        if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            return uri.OriginalString;

        if (uri.IsFile)
            return uri.LocalPath;

        if (!string.IsNullOrEmpty(uri.OriginalString))
            return uri.OriginalString;

        return uri.AbsolutePath;
    }

    private static bool IsLocalPath(string urlOrPath)
    {
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
            return !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return true;
    }

    private static void TryDeleteLocal(string path)
    {
        try
        {
            if (IsLocalPath(path) && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }

    private async Task ShowPreviewAsync(string pathOrUrl)
    {
        var img = new Image
        {
            Source = ImageSourceFinder.ResolveImage(pathOrUrl),
            Aspect = Aspect.AspectFit
        };

        var close = new Button
        {
            Text = "Закрыть",
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 12, 12, 0)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.Children.Add(close);
        Grid.SetRow(img, 1);
        grid.Children.Add(img);

        var page = new ContentPage { BackgroundColor = Colors.Black, Content = grid };
        close.Clicked += (_, __) => Navigation.PopModalAsync();
        await Navigation.PushModalAsync(page, true);
    }
}
