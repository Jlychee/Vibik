using Core;
using Core.Application;
using Infrastructure.Api;
using Microsoft.Extensions.Logging;

namespace Vibik;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<RegistrationPage>();
        builder.Services.AddSingleton<ProfilePage>();
        builder.Services.AddSingleton<HttpClient>();

        builder.Services.AddSingleton<ITaskApi, TaskApi>();
        builder.Services.AddSingleton<IUserApi, UserApi>();
        builder.Services.AddSingleton<HttpClient>();
        


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}