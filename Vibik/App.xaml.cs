using Microsoft.Extensions.Logging;
using Vibik.Services;

namespace Vibik;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override async void OnStart()
    {
        base.OnStart();
        await InitializeSessionAsync();
    }

    private async Task InitializeSessionAsync()
    {
        try
        {
            var sessionInitializer = ServiceHelper.GetService<SessionInitializer>();
            var logger = ServiceHelper.GetService<ILogger<App>>();
            
            logger.LogInformation("Начинаем инициализацию приложения");
            
            var session = await sessionInitializer.InitializeSessionAsync();
            
            logger.LogInformation("Приложение успешно инициализировано для пользователя: {Username}", 
                session.User.Name);
        }
        catch (Exception ex)
        {
            var logger = ServiceHelper.GetService<ILogger<App>>();
            logger.LogError(ex, "Ошибка при инициализации приложения");
            
            await HandleInitializationErrorAsync(ex);
        }
    }

    private async Task HandleInitializationErrorAsync(Exception ex)
    {
        // TODO: Реализовать обработку ошибок инициализации
        // Например, показать диалог с ошибкой или перейти на экран входа
        var logger = ServiceHelper.GetService<ILogger<App>>();
        logger.LogWarning("Обрабатываем ошибку инициализации: {Message}", ex.Message);
        
        await Task.CompletedTask;
    }
}
