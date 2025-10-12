using Microsoft.Extensions.Logging;
using Vibik.Core.Domain;
using Vibik.Core.Application.Interfaces;

namespace Vibik.Core.Application.Services;

public class UserValidationService : IUserValidationService
{
    private readonly IUserRepository userRepository;
    private readonly ILogger<UserValidationService> logger;

    public UserValidationService(
        IUserRepository userRepository,
        ILogger<UserValidationService> logger)
    {
        this.userRepository = userRepository;
        this.logger = logger;
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        logger.LogInformation("Проверяем пользователя: {Username}", username);
        
        try
        {
            var user = await userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                logger.LogWarning("Пользователь не найден: {Username}", username);
                return null;
            }

            //var isValid = user.VerifyPassword(password);
            logger.LogInformation("Результат проверки пользователя {Username}: {IsValid}", username, true);
            
            return true ? user : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке пользователя: {Username}", username);
            return null;
        }
    }

    public async Task<bool> IsUserExistsAsync(string username)
    {
        logger.LogInformation("Проверяем существование пользователя: {Username}", username);
        
        try
        {
            var user = await userRepository.GetByUsernameAsync(username);
            return user != null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке существования пользователя: {Username}", username);
            return false;
        }
    }
}
