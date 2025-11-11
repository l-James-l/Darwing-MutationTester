using Core.IndustrialEstate;
using CoreTests.Startup;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.Exceptions;

namespace Core.Startup;

public abstract class DependencyRegistrar
{
    protected readonly IServiceCollection Services;
    private ServiceProvider? _serviceProvider;

    public DependencyRegistrar(IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        Services = serviceCollection;
    }

    public IServiceProvider Build()
    { 
        if (_serviceProvider != null)
        {
            throw new InvalidOperationException("Service provider has already been built.");
        }

        RegisterDependencies();

        _serviceProvider = Services.BuildServiceProvider();

        //Get the logger configuration to ensure it's created at startup, thus logging is available immediately.
        _serviceProvider.GetService<EstablishLoggerConfiguration>();
        StartUpProcesses();

        return _serviceProvider;
    }

    protected virtual void RegisterLocalDependencies()
    {
        // For override in different interaces to core.
    }

    private void RegisterDependencies()
    {
        Services.AddSingleton<IEventAggregator, EventAggregator>();
        Services.AddSingleton<EstablishLoggerConfiguration>();
        Services.AddSingleton<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        Services.AddSingleton<IMutationSettings, MutationSettings>();
        Services.AddSingleton<IStartUpProcess, SolutionProfileDeserializer>();

        Services.RegisterManySingleton<SolutionPathProvidedAwaiter>(); //Startup process and solution provider.

        RegisterLocalDependencies();
    }

    private void StartUpProcesses()
    {
        if (_serviceProvider == null)
        {
            throw new RegistrationException("Attempted to register Start up process before creating the service provider.");
        }

        IEnumerable<IStartUpProcess> startUpProcesses = _serviceProvider.GetServices<IStartUpProcess>();
        foreach (IStartUpProcess process in startUpProcesses)
        {
            process.StartUp();
        }
    }
}
