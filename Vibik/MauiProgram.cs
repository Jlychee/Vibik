using Microsoft.Extensions.Logging;
using Vibik.Core.Application.Services;
using Vibik.Infrastructure.Repositories;
using Vibik.Infrastructure.Services;
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Регистрация сервисов
        RegisterServices(builder.Services);

        var app = builder.Build();
        
        // Устанавливаем ServiceProvider для ServiceHelper
        ServiceHelper.SetServiceProvider(app.Services);
        
        return app;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // HTTP клиент для работы с API
        services.AddHttpClient();

        // Репозитории
        services.AddScoped<UserRepository>();

        // Сервисы конфигурации
        services.AddScoped<ConfigurationService>();

        // Сервисы валидации
        services.AddScoped<UserValidationService>();

        // Основной сервис сессии
        services.AddScoped<SessionService>();

        // Инициализатор сессии
        services.AddScoped<SessionInitializer>();

        // Логирование
        services.AddLogging();
    }
}