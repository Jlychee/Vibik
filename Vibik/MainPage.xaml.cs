using Core.Interfaces;

namespace Vibik;

public partial class MainPage : IMainPageView
{
    public MainPageViewModel ViewModel { get; }

    public MainPage(
        ITaskApi taskApi,
        IUserApi userApi,
        LoginPage loginPage,
        IWeatherApi weatherApi,
        IAuthService authService)
    {
        InitializeComponent();

        ViewModel = new MainPageViewModel(
            this,
            taskApi,
            userApi,
            loginPage,
            weatherApi,
            authService);

        BindingContext = ViewModel;
    }

    // IMainPageView
    Layout IMainPageView.CardsHost => CardsHost;
    Task IMainPageView.DisplayAlert(string title, string message, string cancel)
        => DisplayAlert(title, message, cancel);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.OnDisappearing();
    }

    private async void OnMapClicked(object sender, EventArgs e)
        => await ViewModel.NavigateToMapAsync();

    private async void OnHomeClicked(object sender, EventArgs e)
        => await ViewModel.NavigateHomeAsync();

    private async void OnProfileClicked(object sender, EventArgs e)
        => await ViewModel.NavigateToProfileAsync();

    private void OnCloseModerationBannerClicked(object sender, EventArgs e)
        => ViewModel.CloseModerationBanner();

    // Оставляю как было (UI/Share), чтобы “больше ничего не трогать”
    private async void OnShareLatestLogClicked(object sender, EventArgs e)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(dir);

        var last = Directory.GetFiles(dir, "*.log")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (last is null)
        {
            await DisplayAlert("Логи", "Файл логов ещё не создан. Сначала сделай HTTP-запрос.", "OK");
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Vibik log",
            File = new ShareFile(last)
        });
    }
}
