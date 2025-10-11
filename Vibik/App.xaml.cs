using Microsoft.Extensions.Logging;
using Vibik.Core.Application.Services;
using Vibik.Core.Domain;

namespace Vibik;

public partial class App : Application
{
    private readonly ISessionService sessionService;
    private readonly ILogger<App> logger;
    private Session? currentSession;

    public App()
    {
        // this.sessionService = sessionService;
        // this.logger = logger;
        InitializeComponent();
    }

    // protected override async void OnStart()
    // {
    //     base.OnStart();
    //     //await InitializeSessionAsync();
    // }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private async Task InitializeSessionAsync()
    {
        try
        {
            logger.LogInformation("Начинаем инициализацию приложения");
            
            // Инициализируем сессию
            currentSession = await sessionService.InitializeSessionAsync();
            
            logger.LogInformation("Приложение успешно инициализировано для пользователя: {Username}", 
                currentSession.User.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при инициализации приложения");
            
            // Здесь можно показать пользователю сообщение об ошибке
            // или перейти на экран входа
            await HandleInitializationErrorAsync(ex);
        }
    }

    private async Task HandleInitializationErrorAsync(Exception ex)
    {
        // TODO: Реализовать обработку ошибок инициализации
        // Например, показать диалог с ошибкой или перейти на экран входа
        logger.LogWarning("Обрабатываем ошибку инициализации: {Message}", ex.Message);
        
        await Task.CompletedTask;
    }

    public Session? GetCurrentSession() => currentSession;
}