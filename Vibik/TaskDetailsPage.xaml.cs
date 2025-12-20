using Core.Domain;
using Core.Interfaces;
using Vibik.Alerts;
using Vibik.Services;
using Vibik.Utils;
using Vibik.Utils.Image;
using Vibik.ViewModels;
using CompressionUtils = Vibik.Utils.Compress.CompressionUtils;

namespace Vibik;

public partial class TaskDetailsPage
{
    private const int TileSize = 120;
    private const int Columns = 3;
    private const double CellGap = 24;

    private readonly TaskModel taskModel;
    private readonly ITaskApi taskApi;

    private bool isTaskCompleted;
    private string TaskKey =>
        taskModel.UserTaskId != 0
            ? taskModel.UserTaskId.ToString()
            : taskModel.TaskId;


    private TaskModelExtendedInfo ExtendedInfo
    {
        get
        {
            taskModel.ExtendedInfo ??= new TaskModelExtendedInfo();

            taskModel.ExtendedInfo.UserPhotos ??= [];
            return taskModel.ExtendedInfo;
        }
    }

    public TaskDetailsPage(TaskModel taskModel, ITaskApi taskApi)
    {
        InitializeComponent();

        this.taskModel = taskModel ?? throw new ArgumentNullException(nameof(taskModel));
        this.taskApi = taskApi ?? throw new ArgumentNullException(nameof(taskApi));

        isTaskCompleted =
            taskModel.Completed ||
            taskModel.ModerationStatus is ModerationStatus.Approved;

        _ = ExtendedInfo;

        BindingContext = new TaskDetailsViewModel(this.taskModel);
        if (BindingContext is not TaskDetailsViewModel vm) return;
        vm.IsCompletedView = isTaskCompleted;
        vm.ModerationStatus = taskModel.ModerationStatus;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await EnsureExtendedInfoLoadedAsync();
        if (!isTaskCompleted)
        {
            PhotoService.LoadLocalPhotos(taskModel, TaskKey);
        }
        
        ImageSourceFinder.EnsureOnlyLocalPhotosForEditableStatuses(taskModel);
        _ = BuildPhotosGrid().ContinueWith(task =>
        {
            var ex = task.Exception?.GetBaseException();
            _ = AppLogger.Error($"Ошибка при построении PhotosGrid: {ex}");
        }, TaskContinuationOptions.OnlyOnFaulted);

    }

    private async Task EnsureExtendedInfoLoadedAsync()
    {
        TaskModel? fullInfoAboutTask = null;
        try
        {
            fullInfoAboutTask = await taskApi.GetTaskAsync(taskModel.UserTaskId.ToString());
        }
        catch (Exception ex)
        {
            await AppLogger.Warn(
                $"EnsureExtendedInfoLoadedAsync: не удалось получить данные с бэка для userTaskId={taskModel.UserTaskId}: {ex.Message}");
        }

        if (fullInfoAboutTask is null)
        {
            taskModel.ExtendedInfo ??= new TaskModelExtendedInfo();
            taskModel.ExtendedInfo.UserPhotos ??= new List<Uri>();

            isTaskCompleted = taskModel.Completed || taskModel.ModerationStatus == ModerationStatus.Approved;
            BindingContext = new TaskDetailsViewModel(taskModel);
            return;
        }
        
        taskModel.Name = fullInfoAboutTask.Name;
        taskModel.Reward = fullInfoAboutTask.Reward;
        taskModel.Completed = fullInfoAboutTask.Completed;
        taskModel.ModerationStatus = fullInfoAboutTask.ModerationStatus;
        taskModel.TaskId = fullInfoAboutTask.TaskId;

        taskModel.ExtendedInfo.UserPhotos ??= [];
        if (fullInfoAboutTask.ExtendedInfo != null)
        {
            taskModel.ExtendedInfo.Description     = fullInfoAboutTask.ExtendedInfo.Description;
            taskModel.ExtendedInfo.PhotosRequired  = fullInfoAboutTask.ExtendedInfo.PhotosRequired;

            if (taskModel.Completed || taskModel.ModerationStatus == ModerationStatus.Approved)
                taskModel.ExtendedInfo.UserPhotos = fullInfoAboutTask.ExtendedInfo.UserPhotos ?? new List<Uri>();
        }

        await AppLogger.Info(
            $"EnsureExtendedInfoLoadedAsync: userTaskId={taskModel.UserTaskId}, " +
            $"status={taskModel.ModerationStatus}, completed={taskModel.Completed}, " +
            $"desc='{taskModel.ExtendedInfo.Description}', photos={taskModel.ExtendedInfo.UserPhotos.Count}");

        isTaskCompleted = taskModel.Completed || taskModel.ModerationStatus == ModerationStatus.Approved;

        BindingContext = new TaskDetailsViewModel(taskModel);
        if (BindingContext is TaskDetailsViewModel vm)
        {
            vm.IsCompletedView = isTaskCompleted;
            vm.ModerationStatus = taskModel.ModerationStatus;
        }
    } 
    
    private async void OnSendClick(object? sender, EventArgs e)
    {
        var vm = BindingContext as TaskDetailsViewModel;
        if (vm?.IsSending == true)
            return;
        List<string> tempCompressedPaths = new();

        try
        {
            var taskStatus = await RefreshServerStatusAsync(vm);
            switch (taskStatus)
            {
                case ModerationStatus.Pending:
                    await AppAlerts.TaskOnModeration();
                    return;
                case ModerationStatus.Approved:
                    await AppAlerts.TaskCompleted();
                    return;
            }

            var required = ExtendedInfo.PhotosRequired;
            var localPaths = ImageSourceFinder.GetUserPhotosPath(taskModel, needLocal:true, needExisting:true);
            var count = localPaths.Count;
            
            if (count < required && required > 0)
            {
                await AppAlerts.NotEnoughPhotos(required, count);
                return;
            }

            if (count == 0)
            {
                await AppAlerts.NoPhotos();
                return;
            }

            if (vm != null) vm.IsSending = true;
            
            tempCompressedPaths = await CompressionUtils.JpegToPaths(localPaths);

            var ok = await taskApi.SubmitAsync(taskModel.UserTaskId.ToString(), tempCompressedPaths);
            if (!ok)
            {
                await AppAlerts.TaskSendFailed();
                return;
            }
            await AppAlerts.TaskSendSuccess();

            UpdateLocalStatus(ModerationStatus.Pending, vm);
            AppEventHub.RequestRefresh(AppRefreshReason.TaskSent);
            
            _ = BuildPhotosGrid();
            await Navigation.PopAsync();
        }
        catch (HttpRequestException ex)
        {
            await AppLogger.Warn($"Сетевая ошибка при отправке задания: {ex.Message}");
            await AppAlerts.TaskSendHttpError();
        }
        catch (Exception ex)
        {
            await AppLogger.Error(ex.ToString());
            await AppAlerts.UniversalError(ex.Message);
        }
        finally
        {
            ImageSourceFinder.CleanupTempFiles(tempCompressedPaths);
            if (vm != null) vm.IsSending = false;
        }
    }
    
    
    private async Task<ModerationStatus> RefreshServerStatusAsync(TaskDetailsViewModel? vm)
    {
        var serverRaw = await taskApi.GetModerationStatusAsync(taskModel.UserTaskId.ToString());
        var serverStatus = ModerationStatusConvertor.MapModeration(serverRaw);

        UpdateLocalStatus(serverStatus, vm);
        return serverStatus;
    }
    
    private void UpdateLocalStatus(ModerationStatus status, TaskDetailsViewModel? vm)
    {
        taskModel.ModerationStatus = status;
        if (vm != null) vm.ModerationStatus = status;
    }


    private async Task BuildPhotosGrid()
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

        var paths = ImageSourceFinder.GetUserPhotosPath(taskModel);

        var required = ExtendedInfo.PhotosRequired;
        var totalSlots = required > 0 ? required : Math.Max(paths.Count + 1, 1);
        if (required == -1 && taskModel.ModerationStatus != ModerationStatus.Approved && taskModel.ModerationStatus != ModerationStatus.Pending)
            totalSlots = taskModel.ExtendedInfo!.UserPhotos.Count + 1;
        if (taskModel.ModerationStatus is ModerationStatus.Approved or ModerationStatus.Pending)
            totalSlots = taskModel.ExtendedInfo!.UserPhotos.Count;
        var rows = (int)Math.Ceiling(totalSlots / (double)Columns);
        for (var r = 0; r < rows; r++)
            PhotoShotsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var j = 0;
        foreach (var path in paths.Take(totalSlots))
            AddPhotoTile(path, j++);

        if (isTaskCompleted) return;
        for (; j < totalSlots; j++)
            AddAddTile(j);
    }
    

    private void AddTileAt(int index, Resources.Components.PhotoTile tile)
    {
        var rows = index / Columns;
        var columns = index % Columns;

        Grid.SetRow(tile, rows);
        Grid.SetColumn(tile, columns);
        PhotoShotsGrid.Children.Add(tile);
    }

    private void AddPhotoTile(string path, int index)
    {
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

        AddTileAt(index, tile);
    }

    private void AddAddTile(int index)
    {
        var add = new Resources.Components.PhotoTile
        {
            IsAddTile = true,
            TileSize = TileSize
        };

        add.AddTapped += OnAddPhotoTapped;

        AddTileAt(index, add);
    }


    private async Task OnPhotoTappedCore(string path)
    {
        var actions = isTaskCompleted || taskModel.ModerationStatus is ModerationStatus.Pending
            ? new[] { "Просмотреть" }
            : new[] { "Просмотреть", "Заменить", "Удалить" };

        var choice = await DisplayActionSheet("Фото", "Отмена", null, actions);
        if (string.IsNullOrEmpty(choice) || choice == "Отмена") return;

        switch (choice)
        {
            case "Просмотреть":
                await ShowPreviewAsync(path);
                break;

            case "Заменить" when !isTaskCompleted || taskModel.ModerationStatus is not ModerationStatus.Pending:
            {
                await ReplacePhotoAsync(path);
                break;
            }

            case "Удалить" when !isTaskCompleted || taskModel.ModerationStatus is not ModerationStatus.Pending:
            {
                await DeletePhotoAsync(path);
                return;
            }
        }
    }

    private async Task DeletePhotoAsync(string path)
    {
        var ok = await AppAlerts.DeletePhoto();
        if (!ok) return;

        var removed = ExtendedInfo.UserPhotos.RemoveAll(p => ImageSourceFinder.GetPathFromUri(p) == path) > 0;
        if (removed) ImageSourceFinder.TryDeleteLocal(path);
        _ = BuildPhotosGrid();
    }

    private async Task ReplacePhotoAsync(string path)
    {
        var oldPathIndex = ExtendedInfo.UserPhotos.FindIndex(p => ImageSourceFinder.GetPathFromUri(p) == path);
        var oldPhoto = ExtendedInfo.UserPhotos[oldPathIndex];
        var newFile = await PhotoService.PickOrCaptureAsync(this);
        if (newFile == null) return;
                    
        var newPath = await PhotoService.SaveFileResultAsync(newFile, TaskKey);
        if (oldPathIndex >= 0)
        {
            ImageSourceFinder.TryDeleteLocal(ImageSourceFinder.GetPathFromUri(oldPhoto));

            ExtendedInfo.UserPhotos[oldPathIndex] = new Uri(newPath);
            _ = BuildPhotosGrid();
        }
    }

    private async void OnAddPhotoTapped(object? sender, EventArgs e)
    {
        if (isTaskCompleted || taskModel.ModerationStatus is ModerationStatus.Pending)
        {
            await AppAlerts.CanNotAddPhoto();
            return;
        }

        try
        {
            var current = ExtendedInfo.UserPhotos?.Count ?? 0;
            var required = ExtendedInfo.PhotosRequired;
            if ( required> 0 && current >= required)
            {
                await AppAlerts.PhotoLimit();
                return;
            }

            var file = await PhotoService.PickOrCaptureAsync(this);
            if (file == null) return;

            var savedPath = await PhotoService.SaveFileResultAsync(file, TaskKey);

            ExtendedInfo.UserPhotos!.Add(new Uri(savedPath));
            _ = BuildPhotosGrid();
        }
        catch (FeatureNotSupportedException)
        {
            await AppAlerts.DoesNotSupport();
        }
        catch (PermissionException)
        {
            await AppAlerts.NoPermission();
        }
        catch (Exception ex)
        {
            await AppAlerts.UniversalError(ex.Message);
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
        close.Clicked += (_, _) => Navigation.PopModalAsync();
        await Navigation.PushModalAsync(page, true);
    }
    
    private async void OnAddManyPhotosClicked(object? sender, EventArgs e)
    {
        if (isTaskCompleted || taskModel.ModerationStatus == ModerationStatus.Pending)
        {
            await AppAlerts.CanNotAddPhoto();
            return;
        }

        try
        {
            var required = ExtendedInfo.PhotosRequired;
            var already = ExtendedInfo.UserPhotos?.Count ?? 0;

            var remaining = required > 0 ? Math.Max(0, required - already) : int.MaxValue;
            if (remaining == 0)
            {
                await AppAlerts.PhotoLimit();
                return;
            }

            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Выберите фото",
                FileTypes = FilePickerFileType.Images
            });

            var picked = results.ToList();
            if (picked == null || picked.Count == 0)
                return;

            var toAdd = picked.Take(remaining).ToList();
            foreach (var file in toAdd)
            {
                var localPath = await PhotoService.SavePickedFileToCacheAsync(file);

                ExtendedInfo.UserPhotos.Add(new Uri(localPath));
            }
            _ = BuildPhotosGrid();

            if (required > 0 && results.Count() >= toAdd.Count)
                await AppAlerts.PhotoLimit();
        }
        catch (Exception ex)
        {
            await AppLogger.Error(ex.ToString());
            await AppAlerts.UniversalError(ex.Message);
        }
    }
}
