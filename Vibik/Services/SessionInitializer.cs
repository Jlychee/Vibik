using Microsoft.Extensions.Logging;
using Vibik.Core.Application.Interfaces;
using Vibik.Core.Domain;

namespace Vibik.Services;

public class SessionInitializer
{
    private readonly ISessionService sessionService;
    private readonly ILogger<SessionInitializer> logger;
    private Session? currentSession;

    public SessionInitializer(
        ISessionService sessionService,
        ILogger<SessionInitializer> logger)
    {
        this.sessionService = sessionService;
        this.logger = logger;
    }

    public Session? CurrentSession => currentSession;

    public bool IsSessionActive => currentSession?.IsActive ?? false;

    public async Task<Session> InitializeSessionAsync()
    {
        logger.LogInformation("Запускаем инициализацию сессии");
        
        try
        {
            currentSession = await sessionService.InitializeSessionAsync();
            logger.LogInformation("Сессия успешно запущена для пользователя: {Username}", 
                currentSession.User.Name);
            return currentSession;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при запуске сессии");
            throw;
        }
    }

    public async Task EndSessionAsync()
    {
        if (currentSession == null)
        {
            logger.LogWarning("Попытка завершить несуществующую сессию");
            return;
        }

        logger.LogInformation("Завершаем сессию для пользователя: {Username}", 
            currentSession.User.Name);
        
        currentSession.Deactivate();
        currentSession = null;
        
        logger.LogInformation("Сессия успешно завершена");
        await Task.CompletedTask;
    }
}

