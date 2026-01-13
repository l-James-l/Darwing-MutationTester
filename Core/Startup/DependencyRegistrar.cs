using Core.IndustrialEstate;
using Core.Interfaces;
using CoreTests.Startup;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.Exceptions;
using Mutator;
using Mutator.MutationImplementations;

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
        // For override in different interfaces to core.
    }

    private void RegisterDependencies()
    {
        Services.AddSingleton<IEventAggregator, EventAggregator>();
        Services.AddSingleton<EstablishLoggerConfiguration>();
        Services.AddSingleton<StartUpProcessManager>();
        Services.AddSingleton<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        Services.AddSingleton<IMutationSettings, MutationSettings>();
        Services.AddSingleton<ISolutionProfileDeserializer, SolutionProfileDeserializer>();
        Services.RegisterManySingleton<ProjectBuilder>(); //IStartupProcess && IWasBuildSuccessful
        Services.AddSingleton<ICancelationTokenFactory, CancelationTokenFactory>();
        Services.RegisterManySingleton<SolutionPathProvidedAwaiter>(); //IStartupProcess and ISolutionProvider.
        Services.AddSingleton<IStartUpProcess, InitialTestRunnner>();
        Services.AddSingleton<IProcessWrapperFactory, ProcessWrapperFactory>();

        RegisterMutators();

        RegisterLocalDependencies();
    }

    private void RegisterMutators()
    {
        Services.RegisterManySingleton<MutationDiscoveryManager>(); //IMutationRunInitiator and IMutationDiscoveryManager
        Services.AddSingleton<IMutationImplementationProvider, MutationImplementationProvider>();
        Services.AddSingleton<IStartUpProcess, MutatedProjectBuilder>();
        Services.AddSingleton<IStartUpProcess, MutatedSolutionTester>();

        //Specific implementations:
        Services.AddSingleton<IMutationImplementation, SubtractToAddMutator>();
        Services.AddSingleton<IMutationImplementation, AddToSubtractMutator>();
    }

    private void StartUpProcesses()
    {
        if (_serviceProvider == null)
        {
            throw new RegistrationException("Attempted to register Start up process before creating the service provider.");
        }

        _serviceProvider.GetRequiredService<StartUpProcessManager>().Initialize();
    }
}
