namespace Vibik;

public partial class MainPage : ContentPage
{
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