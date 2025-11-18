using Microsoft.Extensions.Logging;
using Vibik.Services;

namespace Vibik;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        builder.Services.AddSingleton<IUserApi>(sp =>
        {
            var client = sp.GetRequiredService<HttpClient>();
            return new UserApi(client, useStub: true);
        });

        builder.Services.AddSingleton<IUserApi, UserApi>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistrationPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}