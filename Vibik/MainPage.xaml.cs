using Microsoft.Extensions.Logging;
using Vibik.Core.Application.Services;

namespace Vibik;

public partial class MainPage : ContentPage
{
    private readonly ISessionManager sessionManager;
    private readonly ILogger<MainPage> logger;

    // public MainPage(ISessionManager sessionManager, ILogger<MainPage> logger)
    // {
    //     this.sessionManager = sessionManager;
    //     this.logger = logger;
    //     InitializeComponent();
    // }
        public MainPage()
    {

        InitializeComponent();
    }
    
    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Map());
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopToRootAsync();
        }
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }
}