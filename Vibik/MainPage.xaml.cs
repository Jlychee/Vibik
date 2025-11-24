using System.Collections.ObjectModel;
using Core;
using Core.Application;
using Infrastructure.Api;
using Shared.Models;
using Vibik.Resources.Components;
using Task = System.Threading.Tasks.Task;
using TaskModel = Shared.Models.Task;

namespace Vibik;

public partial class MainPage
{
    private bool taskLoaded = false;
    private readonly Random random = new();
    private readonly ITaskApi taskApi;
    private readonly List<TaskModel> allTasks = [];
    private readonly IUserApi userApi;
    private readonly LoginPage loginPage;
    private readonly IWeatherApi weatherApi;

    private int level;
    private int experience;
    private ImageSource? weatherImage;

    public ImageSource? WeatherImage { get => weatherImage; set { weatherImage = value; OnPropertyChanged(); } }
    private string weatherTemp = "—";
    private string weatherInfoAboutSky = "Загружаем погоду...";
    private string weatherInfoAboutFallout = string.Empty;
    private WeatherInfo? lastWeather;

    public ObservableCollection<TaskModel> Tasks { get; } = new();

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

    public MainPage(ITaskApi taskApi, IUserApi userApi, LoginPage loginPage, IWeatherApi weatherApi)
    {
        InitializeComponent();
        BindingContext = this;
        this.taskApi = taskApi;
        this.userApi = userApi;
        this.weatherApi = weatherApi;
        this.loginPage = loginPage;
    }

    private void UpdateWeatherIcon(string condition)
    {
        var normalized = condition.ToLowerInvariant();
        WeatherImage = normalized switch
        {
            "clear" => ImageSource.FromFile("sunny_weather.svg"),
            "clouds" => ImageSource.FromFile("cloudy_weather.svg"),
            "rain" or "drizzle" => ImageSource.FromFile("rain_weather.svg"),
            "snow" => ImageSource.FromFile("snow_weather.svg"),
            "thunderstorm" => ImageSource.FromFile("storm_weather.svg"),
            _ => null
        };
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
                await DisplayAlert("Проблема с погодой", "Не удалось обновить погоду, показываем последнее значение.", "OK");
            }
            else
            {
                WeatherTemp = "—";
                WeatherInfoAboutSky = "Погода недоступна";
                WeatherInfoAboutFallout = "Проверьте подключение к интернету";
                WeatherImage = null;
                await DisplayAlert("Ошибка", $"Не удалось загрузить погоду: {ex.Message}", "OK");
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
        WeatherInfoAboutFallout = weather.Condition.ToLowerInvariant() switch
        {
            "rain" or "drizzle" => "Возможны осадки",
            "snow" => "Возможен снег",
            "thunderstorm" => "Вероятна гроза",
            _ => "Осадков не ожидается"
        };
        UpdateWeatherIcon(weather.Condition);
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserAsync();
        await LoadWeatherAsync();
        if (taskLoaded) return;
        await LoadTasksAsync();
        taskLoaded = true;
    }
    
    private async Task LoadUserAsync()
    {
        var userId = Preferences.Get("current_user", "");
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
            allTasks.Clear();
            allTasks.AddRange(tasks);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить задания: {ex.Message}", "OK");
            Tasks.Clear();
        }
    }
    
    private void ApplyFilter()
    {
        var  filtered = allTasks.ToList();
        var count = filtered.Count;
        if (count == 0)
        {
            noTasks = true;
            return;
        }
        
        if (count > 4) 
            count = 4;
         //  фильтр по выполненным
         // if (!ShowCompleted) { ... }

         CardsHost.Children.Clear();
        
         for (var i = 0; i < count; i++)
         {
             var card = new TaskCard
             {
                 TaskApi = taskApi,
                 Item = filtered[i],
                 Title = filtered[i].Name,
                 DaysPassed = filtered[i].DaysPassed(),
                 Cost = filtered[i].Reward,
                 SwapCost = filtered[i].Swap,
                 // RefreshCommand = new Command(async () =>
                 // {
                 //     var ok = await taskApi.SwapTaskAsync(task.TaskId);
                 //     if (!ok)
                 //     {
                 //         await DisplayAlert("Ошибка", "Не удалось сменить задание. Попробуйте позже.", "OK");
                 //         return;
                 //     }
                 //
                 //     await LoadTasksAsync();
                 //     await LoadUserAsync();
                 //     // тут менять кол-во монет 
                 //     
                 // }),
                     
                 HorizontalOptions = LayoutOptions.Fill
             };
             card.RefreshCommand = new Command(async () =>
             {
                 var current = card.Item;
                 if (current is null)
                     return;
                 
                 var confirmed = await DisplayAlert(
                     "Сменить задание",
                     $"Вы уверены, что хотите поменять задание за {card.SwapCost} опыта?",
                     "Да",
                     "Нет");
        
                 if (!confirmed)
                     return;
        
                 var currentTaskIds = CardsHost.Children
                     .OfType<TaskCard>()
                     .Select(c => c.Item?.TaskId)
                     .Where(id => !string.IsNullOrEmpty(id))
                     .ToHashSet();
        
                 var candidates = allTasks
                     .Where(t =>
                         !t.Completed &&
                         !currentTaskIds.Contains(t.TaskId))
                     .ToList();
        
                 if (candidates.Count == 0)
                 {
                     await DisplayAlert("Новых заданий нет",
                         "Сейчас нет заданий, которые вы ещё не делали и которых нет среди текущих.",
                         "OK");
                     return;
                 }
        
                 var next = candidates[random.Next(candidates.Count)];
        
                 card.Item = next;
                 card.Title = next.Name;
                 card.DaysPassed = next.DaysPassed();
                 card.Cost = next.Reward;
                 card.SwapCost = next.Swap;
             });
        
        
        
             // Если появится иконка у задач
             // if (!string.IsNullOrWhiteSpace(task.Icon))
             //     card.IconSource = ImageSource.FromFile(task.Icon);
        
             CardsHost.Children.Add(card);
        }
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
        await Navigation.PushAsync(new ProfilePage(userApi, loginPage));
    }

    private async void OnShareLatestLogClicked(object sender, EventArgs e)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(dir);

        var last = Directory.GetFiles(dir, "*.log")
            .OrderByDescending(f => f)   // имена вида vibik-YYYYMMDD.log или raw-http.log
            .FirstOrDefault();

        if (last is null)
        {
            await DisplayAlert("Логи", "Файл логов ещё не создан. Сначала сделай HTTP-запрос.", "OK");
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Vibik log",
            File  = new ShareFile(last)
        });
    }

}   
