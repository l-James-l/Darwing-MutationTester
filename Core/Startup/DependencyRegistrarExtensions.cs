using Microsoft.Extensions.DependencyInjection;
using Models.Exceptions;

namespace Core.Startup;

public static class DependencyRegistrarExtensions
{
    public static void RegisterManySingleton<T>(this IServiceCollection services) where T : class
    {
        ArgumentNullException.ThrowIfNull(services);

        Type[] types = typeof(T).GetInterfaces();
        if (types.Length == 0)
        {
            return;
        }

        services.AddSingleton<T>();
        foreach (Type type in types)
        {
            services.AddSingleton(type, provider => provider.GetService<T>() ?? throw new RegistrationException(typeof(T), type));
        }
    }
}
