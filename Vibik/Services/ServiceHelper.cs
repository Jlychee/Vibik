using Microsoft.Extensions.DependencyInjection;

namespace Vibik.Services;

public static class ServiceHelper
{
    private static IServiceProvider? _serviceProvider;
    
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public static T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceProvider не инициализирован");
            
        return _serviceProvider.GetRequiredService<T>();
    }
}
