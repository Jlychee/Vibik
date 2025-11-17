using Vibik.Resources.Components;
using Vibik.Services;
using TaskModel = Shared.Models.Task;

namespace Vibik;

public partial class MainPage
{
    private readonly ITaskApi taskApi;
    private readonly List<TaskModel> allTasks = [];

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

    public MainPage() : this(TaskApi.Create("https://localhost:5001/", useStub: true)) { }

    private MainPage(ITaskApi taskApi)
    {
        InitializeComponent();
        BindingContext = this;
        this.taskApi = taskApi;

        Level = GetUserLevel();
        Experience = GetUserExperience();

        WeatherImage = ImageSource.FromFile("cloudy_weather.png");
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

    // #ЗАГЛУШКА: вытаскивать из БД
    private int GetUserLevel() => 10;
    // #ЗАГЛУШКА: вытаскивать из БД
    private int GetUserExperience() => 15;
    
    private void ApplyFilter()
    {
        IEnumerable<TaskModel> filtered = allTasks;

        // заглушка фильтр по выполненным
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
        await Navigation.PushAsync(new MapPage());
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopToRootAsync();
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }
}
