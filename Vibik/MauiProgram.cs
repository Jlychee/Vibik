using Microsoft.Extensions.Logging;
using Vibik.Core.Application.Interfaces;
using Vibik.Core.Application.Services;
using Vibik.Infrastructure.Repositories;
using Vibik.Infrastructure.Services;

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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Регистрация сервисов
        //RegisterServices(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // HTTP клиент для работы с API
        services.AddHttpClient();

        // Репозитории
        services.AddScoped<IUserRepository, UserRepository>();

        // Сервисы
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ISessionManager, SessionManager>();

        // Логирование
        services.AddLogging();
    }
}