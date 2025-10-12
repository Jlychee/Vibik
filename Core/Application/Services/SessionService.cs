using Microsoft.Extensions.Logging;
using Vibik.Core.Domain;
using Vibik.Core.Application.Interfaces;

namespace Vibik.Core.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUserValidationService userValidationService;
    private readonly IMapInitializationService mapInitializationService;
    private readonly IConfigurationService configurationService;
    private readonly ILogger<SessionService> logger;

    public SessionService(
        IUserValidationService userValidationService,
        IMapInitializationService mapInitializationService,
        IConfigurationService configurationService,
        ILogger<SessionService> logger)
    {
        this.userValidationService = userValidationService;
        this.mapInitializationService = mapInitializationService;
        this.configurationService = configurationService;
        this.logger = logger;
    }

    public async Task<Session> InitializeSessionAsync()
    {
        logger.LogInformation("Начинаем инициализацию сессии");

        try
        {
            // Получаем данные пользователя из конфигурации
            var userConfig = await configurationService.GetUserConfigurationAsync();
            
            // Проверяем пользователя в базе данных и получаем объект пользователя
            var user = await ValidateUserAsync(userConfig.Username, userConfig.Password);
            if (user == null)
            {
                throw new InvalidOperationException("Неверные данные пользователя");
            }

            // Инициализируем карту
            var mapConfig = await InitializeMapAsync();

            // Создаем сессию с объектом пользователя
            var session = new Session(user, mapConfig);
            
            logger.LogInformation("Сессия успешно инициализирована для пользователя: {Username}", user.Username);
            return session;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при инициализации сессии");
            throw;
        }
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        logger.LogInformation("Проверяем пользователя: {Username}", username);
        
        try
        {
            return await userValidationService.ValidateUserAsync(username, password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке пользователя: {Username}", username);
            return null;
        }
    }

    public async Task<MapConfiguration> InitializeMapAsync()
    {
        logger.LogInformation("Инициализируем карту");
        
        try
        {
            return await mapInitializationService.InitializeMapAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при инициализации карты");
            throw;
        }
    }
}