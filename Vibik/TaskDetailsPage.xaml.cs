using System.Text.RegularExpressions;
using Vibik.Core.Domain;

namespace Vibik;

public partial class TaskDetailsPage
{
    private const int TileSize = 120;

    private readonly TaskItem task;

    public TaskDetailsPage(TaskItem task)
    {
        InitializeComponent();
        this.task = task ?? throw new ArgumentNullException(nameof(task));
        BindingContext = new ViewModel(this.task);
        BuildPhotosGrid();
    }

    private async void OnSendClick(object? sender, EventArgs e)
    {
        if (task.RequiredPhotoCount > 0 && task.PhotoPaths.Count < task.RequiredPhotoCount)
        {
            await DisplayAlert("Недостаточно фото",
                $"Нужно минимум {task.RequiredPhotoCount}. Сейчас: {task.PhotoPaths.Count}.", "OK");
            return;
        }

        // #ЗАГЛУШКА: отправлять в тгбот и все такое
        await DisplayAlert("Готово", "Задание отправлено (заглушка).", "OK");
    }

    
    private void BuildPhotosGrid()
    {
        PhotoShotsGrid.Children.Clear();
        PhotoShotsGrid.ColumnDefinitions.Clear();
        PhotoShotsGrid.RowDefinitions.Clear();
        PhotoShotsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var paths = task.PhotoPaths ?? new List<string>();
        var col = 0;

        foreach (var path in paths)
        {
            PhotoShotsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var tile = CreatePhotoTile(path);
            Grid.SetRow(tile, 0);
            Grid.SetColumn(tile, col++);
            PhotoShotsGrid.Children.Add(tile);
        }

        if (task.RequiredPhotoCount > 0 && paths.Count >= task.RequiredPhotoCount) return;
        PhotoShotsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var add = CreateAddTile();
        Grid.SetRow(add, 0);
        Grid.SetColumn(add, col);
        PhotoShotsGrid.Children.Add(add);
    }

    private View CreatePhotoTile(string path)
    {
        var image = new Image
        {
            Source = File.Exists(path) ? ImageSource.FromFile(path) : "example_collage.png",
            WidthRequest = TileSize,
            HeightRequest = TileSize,
            Aspect = Aspect.AspectFill
        };

        var frame = new Frame
        {
            Content = image,
            Padding = 0,
            CornerRadius = 12,
            HasShadow = true,
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(0, 0, 24, 0),
            BindingContext = path
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnPhotoTapped;
        frame.GestureRecognizers.Add(tap);

        return frame;
    }

    private View CreateAddTile()
    {
        var plus = new Label
        {
            Text = "＋",
            FontSize = 48,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var caption = new Label
        {
            Text = "Добавить",
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { plus, caption }
        };

        var frame = new Frame
        {
            Content = stack,
            WidthRequest = TileSize,
            HeightRequest = TileSize,
            Padding = 0,
            CornerRadius = 12,
            HasShadow = true,
            BorderColor = Colors.Gray,
            BackgroundColor = Colors.Transparent,
            Margin = new Thickness(0, 0, 24, 0)
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnAddPhotoTapped;
        frame.GestureRecognizers.Add(tap);

        return frame;
    }


    private async void OnAddPhotoTapped(object? sender, EventArgs e)
    {
        try
        {
            if (task.RequiredPhotoCount > 0 && task.PhotoPaths.Count >= task.RequiredPhotoCount)
            {
                await DisplayAlert("Лимит", "Достигнуто максимальное количество фото.", "OK");
                return;
            }

            var file = await PickOrCapturePhotoAsync();
            if (file == null) return;

            var savedPath = await SaveFileResultAsync(file, TaskKey);
            task.AddPhotoPath(savedPath);
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

    private async void OnPhotoTapped(object? sender, EventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not string path) return;

        var choice = await DisplayActionSheet("Фото", "Отмена", null, "Просмотреть", "Заменить", "Удалить");
        if (string.IsNullOrEmpty(choice) || choice == "Отмена") return;

        try
        {
            if (choice == "Просмотреть")
            {
                await ShowPreviewAsync(path);
            }
            else if (choice == "Заменить")
            {
                var newFile = await PickOrCapturePhotoAsync();
                if (newFile == null) return;

                var newPath = await SaveFileResultAsync(newFile, TaskKey);
                if (task.ReplacePhotoPath(path, newPath) && File.Exists(path))
                {
                    try { File.Delete(path); } catch { /* ignore */ }
                }
                BuildPhotosGrid();
            }
            else if (choice == "Удалить")
            {
                var ok = await DisplayAlert("Удалить фото?", "Это действие необратимо.", "Удалить", "Отмена");
                if (!ok) return;

                if (task.RemovePhotoPath(path) && File.Exists(path))
                {
                    try { File.Delete(path); } catch { /* ignore */ }
                }
                BuildPhotosGrid();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
    
    private async Task<FileResult?> PickOrCapturePhotoAsync()
    {
        var action = await DisplayActionSheet("Добавить фото", "Отмена", null, "Сделать фото", "Выбрать из галереи");
        if (string.IsNullOrEmpty(action) || action == "Отмена") return null;

        if (action == "Сделать фото")
        {
            if (!await EnsurePermission<Permissions.Camera>())
            {
                await DisplayAlert("Нет доступа", "Разрешите доступ к камере.", "OK");
                return null;
            }
            return await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"task_{TaskKey}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
            });
        }

        _ = await EnsurePermission<Permissions.Photos>();
        return await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Выберите фото" });
    }

    private static async Task<string> SaveFileResultAsync(FileResult file, string taskKey)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var dir = Path.Combine(FileSystem.AppDataDirectory, "tasks", taskKey);
        Directory.CreateDirectory(dir);

        var name = $"photo_{DateTime.Now:yyyyMMdd_HHmmssfff}{ext}";
        var dst = Path.Combine(dir, name);

        await using var srcStream = await file.OpenReadAsync();
        await using var dstStream = File.Create(dst);
        await srcStream.CopyToAsync(dstStream);

        return dst;
    }

    private string TaskKey =>
        Slug($"{task.OwnerName}_{task.DisplayName}".Trim().Length > 0
            ? $"{task.OwnerName}_{task.DisplayName}"
            : (!string.IsNullOrWhiteSpace(task.Title) ? task.Title : "task"));

    private static string Slug(string s)
    {
        s = (s ?? string.Empty).ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-_]+", "_");
        return s.Trim('_', '-');
    }

    private static async Task<bool> EnsurePermission<T>() where T : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<T>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<T>();
        return status == PermissionStatus.Granted;
    }

    private async Task ShowPreviewAsync(string path)
    {
        var img = new Image { Source = ImageSource.FromFile(path), Aspect = Aspect.AspectFit };
        var close = new Button { Text = "Закрыть", HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, 12, 12, 0) };

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

    private sealed class ViewModel
    {
        public string TaskName { get; }
        public string Description { get; }
        public ImageSource ExampleCollage { get; }

        public ViewModel(TaskItem task)
        {
            TaskName = string.IsNullOrWhiteSpace(task.DisplayName) ? task.Title : task.DisplayName;
            Description = task.Description ?? string.Empty;

            ExampleCollage = (!string.IsNullOrWhiteSpace(task.PathToExampleCollage) &&
                              File.Exists(task.PathToExampleCollage))
                ? ImageSource.FromFile(task.PathToExampleCollage)
                : ImageSource.FromFile("example_collage.png");
        }
    }
}

