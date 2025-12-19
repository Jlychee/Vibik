using Microsoft.Extensions.Logging;
using Core.Interfaces;
using Infrastructure.Api;
using Infrastructure.Networking;
using Infrastructure.Services;
using Microsoft.Maui.Handlers;
using Vibik.Services;

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
        builder.Services.AddSingleton<TaskDetailsPage>();
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<RegistrationPage>();
        builder.Services.AddSingleton<ProfilePage>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IAuthNavigator, AuthNavigator>();

        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddTransient<RefreshHeaderHandler>();
        builder.Services.AddTransient<HttpLoggingHandler>();


        var backendBaseUri =
#if ANDROID
            //new Uri("http://158.160.105.104:5000");
            new Uri("http://158.160.126.197:5000");
            //new Uri("http://89.169.175.21:5000");
            EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            {
                handler.PlatformView.Background = null;
            });
#else
            //new Uri("https://158.160.105.104:5000");
            new Uri("http://158.160.126.197:5000");
        //new Uri("http://89.169.175.21:5000");
#endif
        builder.Services.AddHttpClient("AuthRefresh", client =>
        {
            client.BaseAddress = backendBaseUri;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddHttpMessageHandler<RefreshHeaderHandler>();
        builder.Services
            .AddHttpClient<ITaskApi, TaskApi>(client =>
            {
                client.BaseAddress = backendBaseUri;
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();

        builder.Services
            .AddHttpClient<IUserApi, UserApi>(client =>
            {
                client.BaseAddress = backendBaseUri;
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();
        builder.Services
            .AddHttpClient<IPhotoApi, PhotoApi>(client =>
            {
                client.BaseAddress = backendBaseUri;
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();
        builder.Services
            .AddHttpClient<IWeatherApi, WeatherApi>(client =>
            {
                client.BaseAddress = backendBaseUri;
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<HttpLoggingHandler>();


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}