using Vibik.Utils;

namespace Vibik;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        AppLogger.Initialize(FileSystem.AppDataDirectory);

    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}