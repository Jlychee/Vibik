namespace Vibik;

public partial class App : Application
{
    private readonly LoginPage loginPage;
    public App(LoginPage loginPage)
    {
        InitializeComponent();
        this.loginPage = loginPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var hasUser = Preferences.ContainsKey("current_user");
        var startPage = hasUser
            ? (Page)new AppShell()
            : new NavigationPage(loginPage);

        return new Window(startPage);
    }
}