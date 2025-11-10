using Microsoft.Extensions.DependencyInjection;

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

public class RegistrationException : Exception
{
    public RegistrationException(string msg) : base(msg)
    {
        
    }

    public RegistrationException(Type t1, Type t2) : base($"Failed to register insance of {t1} against {t2}")
    {
    }
}