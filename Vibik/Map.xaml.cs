namespace Vibik;

public partial class Map
{
    public Map()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
