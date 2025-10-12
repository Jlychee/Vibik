using Microsoft.Extensions.Logging;
using Vibik.Core.Application.Interfaces;
using Vibik.Core.Domain;

namespace Vibik.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> logger;
    private readonly HttpClient httpClient;

    public UserRepository(ILogger<UserRepository> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        logger.LogInformation("Получаем пользователя из БД: {Username}", username);
        
        try
        {
            // TODO: Здесь будет вызов к вашему API для получения пользователя
            // Пример: var response = await httpClient.GetAsync($"api/users/{username}");
            
            // Заглушка - возвращаем null, чтобы вы могли реализовать свою логику
            await Task.Delay(50); // Имитация сетевого запроса
            
            logger.LogInformation("Пользователь {Username} не найден в БД", username);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении пользователя {Username} из БД", ex.Message);
            throw;
        }
    }

    public async Task<User> CreateAsync(User user)
    {
        logger.LogInformation("Создаем пользователя в БД: {Username}", user.Username);
        
        try
        {
            // TODO: Здесь будет вызов к вашему API для создания пользователя
            // Пример: var response = await httpClient.PostAsJsonAsync("api/users", user);
            
            await Task.Delay(50); // Имитация сетевого запроса
            
            logger.LogInformation("Пользователь {Username} успешно создан в БД", user.Username);
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании пользователя {Username} в БД", ex.Message);
            throw;
        }
    }

    public async Task UpdateAsync(User user)
    {
        logger.LogInformation("Обновляем пользователя в БД: {Username}", user.Username);
        
        try
        {
            // TODO: Здесь будет вызов к вашему API для обновления пользователя
            // Пример: var response = await httpClient.PutAsJsonAsync($"api/users/{user.Name}", user);
            
            await Task.Delay(50); // Имитация сетевого запроса
            
            logger.LogInformation("Пользователь {Username} успешно обновлен в БД", user.Username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении пользователя {Username} в БД", ex.Message);
            throw;
        }
    }

    public async Task DeleteAsync(string username)
    {
        logger.LogInformation("Удаляем пользователя из БД: {Username}", username);
        
        try
        {
            // TODO: Здесь будет вызов к вашему API для удаления пользователя
            // Пример: var response = await httpClient.DeleteAsync($"api/users/{username}");
            
            await Task.Delay(50); // Имитация сетевого запроса
            
            logger.LogInformation("Пользователь {Username} успешно удален из БД", username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении пользователя {Username} из БД", ex.Message);
            throw;
        }
    }
}