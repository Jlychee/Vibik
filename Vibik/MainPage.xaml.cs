namespace Vibik;

public partial class MainPage : ContentPage
{

    public MainPage()
    {
        InitializeComponent();
    }

    // private void OnCounterClicked(object? sender, EventArgs e)
    // {
    //     count++;
    //
    //     if (count == 1)
    //         CounterBtn.Text = $"Clicked {count} time";
    //     else
    //         CounterBtn.Text = $"Clicked {count} times";
    //
    //     SemanticScreenReader.Announce(CounterBtn.Text);
    // }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopToRootAsync();
        }
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        //await Navigation.PushAsync(new SearchPage());
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        //await Navigation.PushAsync(new ProfilePage());
    }
}