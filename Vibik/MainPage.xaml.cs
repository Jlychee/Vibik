using Core;
using Core.Application;
using Infrastructure.Api;
using Vibik.Resources.Components;
using TaskModel = Shared.Models.Task;

namespace Vibik;

public partial class MainPage
{
    private readonly ITaskApi taskApi;
    private readonly List<TaskModel> allTasks = [];
    private readonly IUserApi userApi;
    private readonly LoginPage loginPage;

    private int level;
    private int experience;
    private ImageSource? weatherImage;

    public ImageSource? WeatherImage { get => weatherImage; set { weatherImage = value; OnPropertyChanged(); } }
    public int Level { get => level; set { level = value; OnPropertyChanged(); } }
    public int Experience { get => experience; set { experience = value; OnPropertyChanged(); } }

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

    // #ЗАГЛУШКА: вытаскивать из погодного апи
    public string WeatherTemp => "25°";
    public string WeatherInfoAboutSky => "Облачно";
    public string WeatherInfoAboutFallout => "Осадков не ожидается";
    
    public MainPage(ITaskApi taskApi, IUserApi userApi, LoginPage loginPage)
    {
        InitializeComponent();
        BindingContext = this;
        this.taskApi = taskApi;
        this.userApi = userApi;
        WeatherImage = ImageSource.FromFile("cloudy_weather.png");
        this.loginPage = loginPage;
    }

    // #ЗАГЛУШКА: маппинг погодного кода на картинку
    private void UpdateWeatherIcon(string condition)
    {
        WeatherImage = condition switch
        {
            "Clear" => ImageSource.FromFile("sunny_weather.png"),
            "Clouds" => ImageSource.FromFile("cloudy_weather.png"),
            "Rain" or "Drizzle" => ImageSource.FromFile("rain_weather.png"),
            "Snow" => ImageSource.FromFile("snow_weather.png"),
            "Thunderstorm" => ImageSource.FromFile("storm_weather.png"),
            _ => null
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTasksAsync();
        await LoadUserAsync();
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
        }
    }
    
    private void ApplyFilter()
    {
        IEnumerable<TaskModel> filtered = allTasks;

        //  фильтр по выполненным
        // if (!ShowCompleted) { ... }

        CardsHost.Children.Clear();

        foreach (var task in filtered)
        {
            var card = new TaskCard
            {
                TaskApi = taskApi,
                Item = task,
                Title = task.Name,
                DaysPassed = task.DaysPassed(),
                Cost = task.Reward,
                SwapCost = task.Swap,
                RefreshCommand = new Command(() =>
                    DisplayAlert("Смена задания", $"Заменить: {task.Name}", "OK")),
                HorizontalOptions = LayoutOptions.Fill
            };

            // Если появится иконка у задач
            // if (!string.IsNullOrWhiteSpace(task.Icon))
            //     card.IconSource = ImageSource.FromFile(task.Icon);

            CardsHost.Children.Add(card);
        }
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Map());
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
}   
