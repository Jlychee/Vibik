using Vibik.Resources.Components;

namespace Vibik;

public partial class MainPage
{
    private int level;
    private int coins;
    private bool showCompleted;
    private ImageSource? weatherImage;

    public ImageSource? WeatherImage
    {
        get => weatherImage;
        set { if (weatherImage == value) return; weatherImage = value; OnPropertyChanged(nameof(WeatherImage)); }

    }

    public int Level
    {
        get => level;
        set { if (level == value) return; level = value; OnPropertyChanged(nameof(Level)); }
    }

    public int Coins
    {
        get => coins;
        set { if (coins == value) return; coins = value; OnPropertyChanged(nameof(Coins)); }
    }

    public bool ShowCompleted
    {
        get => showCompleted;
        set { if (showCompleted == value) return; showCompleted = value; OnPropertyChanged(nameof(ShowCompleted)); ApplyFilter(); }
    }

    // #ЗАГЛУШКА: вытаскивать из погодного апи
    public string WeatherTemp  => "25°";
    public string WeatherInfoAboutSky => "Облачно";
    public string WeatherInfoAboutFallout => "Осадков не ожидается";

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        Level = GetUserLevel();
        Coins = GetUserCoins();
        WeatherImage = ImageSource.FromFile("cloudy_weather.png");
        GenerateTaskCards();
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
    // #ЗАГЛУШКА: вытаскивать из БД
    private int GetUserLevel() => 10;

    // #ЗАГЛУШКА: вытаскивать из БД
    private int GetUserCoins() => 15;

    private void GenerateTaskCards()
    {
        // #ЗАГЛУШКА: забирать задания из БД
        var tasks = new[]
        {
            new { Title = "Медовые шесть",    Days = 0, Cost = 15, Swap =  5, Icon = "default_task_photo.png" },
            new { Title = "Лесной сбор",      Days = 3, Cost = 20, Swap = 12, Icon = "default_task_photo.png" },
            new { Title = "Тыквенный пряник", Days = 7, Cost = 30, Swap = 35, Icon = "default_task_photo.png" },
            new { Title = "Семена акации",    Days = 1, Cost = 10, Swap =  8, Icon = "default_task_photo.png" },
        };

        CardsHost.Children.Clear();

        foreach (var task in tasks)
        {
            var item = new Vibik.Core.Domain.TaskItem
            {
                OwnerName = "Vibik",
                Title = task.Title,
                TaskName = task.Title,
                Description = "Заглушка описания",
                PathToExampleCollage = "example_collage.png"
            };
            item.SetDayPassed(task.Days);
            item.SetAward(task.Cost);
            item.SetSwapCost(task.Swap);
            item.SetRequiredPhotoCount(1);
            var card = new TaskCard
            {
                Item = item,
                Title = item.TaskName,
                DaysPassed = item.DayPassed,
                Cost = item.Award,
                SwapCost = item.SwapCost,
                RefreshCommand = new Command(() =>
                    DisplayAlert("Смена задания", $"Заменить: {task.Title}", "OK"))
            };
            card.HorizontalOptions = LayoutOptions.Fill;
            if (!string.IsNullOrWhiteSpace(task.Icon))
                card.IconSource = ImageSource.FromFile(task.Icon);

            CardsHost.Children.Add(card);
        }
    }

    private void ApplyFilter()
    {
        // TODO: сюда — логика показа/скрытия выполненных заданий
        // пример: проходишься по CardsHost.Children и меняешь IsVisible у карточек
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
        await Navigation.PushAsync(new ProfilePage());
    }
}
