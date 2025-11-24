using Core;
using Core.Interfaces;
using Shared.Models;
using Utils;
using Task = System.Threading.Tasks.Task;

namespace Vibik;

public partial class TaskDetailsPage
{
    private const int TileSize = 120;

    private readonly Shared.Models.Task task;
    private readonly ITaskApi taskApi;


    private TaskExtendedInfo ExtendedInfo => task.ExtendedInfo;

    public TaskDetailsPage(Shared.Models.Task task, ITaskApi taskApi)
    {
        InitializeComponent();
        this.task = task ?? throw new ArgumentNullException(nameof(task));
        this.taskApi = taskApi ?? throw new ArgumentNullException(nameof(taskApi));
        this.task.ExtendedInfo.UserPhotos ??= [];
        BindingContext = new ViewModel(this.task);
        LoadSavedPhotos();
        BuildPhotosGrid();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSavedPhotos();
        BuildPhotosGrid();
    }

    private async void OnSendClick(object? sender, EventArgs e)
    {
        var required = ExtendedInfo.PhotosRequired;
        var count = ExtendedInfo.UserPhotos.Count;

        if (required > 0 && count < required)
        {
            await DisplayAlert("Недостаточно фото",
                $"Нужно минимум {required}. Сейчас: {count}.", "OK");
            return;
        }

        var localPaths = ExtendedInfo.UserPhotos
            .Select(p => p.Url)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Where(IsLocalPath)
            .Where(File.Exists)
            .ToList();

        if (localPaths.Count == 0)
        {
            await DisplayAlert("Нет фото", "Добавьте хотя бы одно фото", "OK");
            return;
        }

        try
        {
            var ok = await taskApi.SubmitAsync(task.TaskId ?? TaskKey, localPaths);
            if (!ok)
            {
                await DisplayAlert("Ошибка", "Не удалось отправить задание. Проверьте сеть и попробуйте ещё раз.",
                    "OK");
                return;
            }

            await DisplayAlert("Готово", "Задание отправлено!", "OK");
            task.Completed = true;
            foreach (var p in localPaths) TryDeleteLocal(p);
            BuildPhotosGrid();
        }
        catch (Exception exception)
        {
            await DisplayAlert("Ошибка", exception.Message, "OK");
        }
    }
    
    private const int Columns = 3;
    private const double CellGap = 24;

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
            .Select(p => p.Url)
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

        for (; i < totalSlots; i++)
            AddAddTile(i);
    }
    
    private void LoadSavedPhotos()
    {
        var saved = PhotoService.GetSavedPhotos(TaskKey);

        ExtendedInfo.UserPhotos.RemoveAll(p =>
        {
            if (string.IsNullOrWhiteSpace(p.Url)) return false;
            if (!IsLocalPath(p.Url)) return false;
            return !File.Exists(p.Url);
        });

        var existing = ExtendedInfo.UserPhotos
            .Where(p => !string.IsNullOrWhiteSpace(p.Url))
            .Select(p => p.Url)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in saved.Where(File.Exists))
        {
            if (existing.Add(path))
                ExtendedInfo.UserPhotos.Add(new PhotoModel { Url = path });
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
        var choice = await DisplayActionSheet("Фото", "Отмена", null, "Просмотреть", "Заменить", "Удалить");
        if (string.IsNullOrEmpty(choice) || choice == "Отмена") return;
        switch (choice)
        {
            case "Просмотреть":
                await ShowPreviewAsync(path);
                break;

            case "Заменить":
            {
                var newFile = await PhotoService.PickOrCaptureAsync(this);
                if (newFile == null) return;

                var newPath = await PhotoService.SaveFileResultAsync(newFile, TaskKey);
                var idx = ExtendedInfo.UserPhotos.FindIndex(p => p.Url == path);
                if (idx >= 0)
                {
                    TryDeleteLocal(ExtendedInfo.UserPhotos[idx].Url);

                    ExtendedInfo.UserPhotos[idx] = new PhotoModel { Url = newPath };
                    BuildPhotosGrid();
                }
                break;
            }

            case "Удалить":
            {
                var ok = await DisplayAlert("Удалить фото?", "Это действие необратимо.", "Удалить", "Отмена");
                if (!ok) return;

                var removed = ExtendedInfo.UserPhotos.RemoveAll(p => p.Url == path) > 0;
                if (removed) TryDeleteLocal(path);
                BuildPhotosGrid();
                break;
            }
        }
    }

    private async void OnAddPhotoTapped(object? sender, EventArgs e)
    {
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

            ExtendedInfo.UserPhotos!.Add(new PhotoModel { Url = savedPath });
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
        !string.IsNullOrWhiteSpace(task.TaskId)
            ? task.TaskId
            : PhotoService.Slug(string.IsNullOrWhiteSpace(task.Name) ? "task" : task.Name);

    private static bool IsLocalPath(string urlOrPath)
    {
        if(Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
            return !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return true;
    }

    private static void TryDeleteLocal(string path)
    {
        try
        {
            if(IsLocalPath(path) && !File.Exists(path))
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
