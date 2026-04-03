using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.Exceptions;
using Models.SharedInterfaces;
using Mutator;
using Mutator.MutationImplementations;
using Mutator.MutationImplementations.Arithmetic;
using Mutator.MutationImplementations.Logical;
using Mutator.MutationImplementations.Relational;
using Mutator.MutationImplementations.Unary;
using System.IO.Abstractions;

namespace Core.Startup;

public abstract class DependencyRegistrar : IDisposable
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
        Services.AddSingleton<IFileSystem, FileSystem>();
        Services.AddSingleton<IEventAggregator, EventAggregator>();
        Services.AddSingleton<EstablishLoggerConfiguration>();
        Services.AddSingleton<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        Services.AddSingleton<IMutationSettings, MutationSettings>();
        Services.AddSingleton<ISolutionProfileDeserializer, SolutionProfileDeserializer>();
        Services.AddSingleton<ISolutionBuilder, SolutionBuilder>();
        Services.AddSingleton<ICancelationTokenFactory, CancelationTokenFactory>();
        Services.AddSingleton<ISolutionLoader, SolutionLoader>();
        Services.AddSingleton<ISolutionProvider, SolutionProvider>();
        Services.AddSingleton<IMutationRunInitiator, InitialTestRunner>();
        Services.AddSingleton<IProcessWrapperFactory, ProcessWrapperFactory>();
        Services.AddSingleton<IStatusTracker, StatusTracker>();
        Services.AddSingleton<IGitDiffManager, GitDiffManager>();
        Services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
        Services.AddSingleton<IGeminiChatClientFactory, GeminiChatClientFactory>();
        Services.AddSingleton<IGeminiApiHandler, GeminiApiHandler>();
        Services.AddSingleton<ICoverageMapper, CoverageMapper>();

        RegisterMutators();

        RegisterLocalDependencies();
    }

    public DependencyRegistrar RegisterConfigurations()
    {
        IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets<GeminiApiHandler>() // Give this any class from the Core project so that it can find the sectrets file ID
                    .Build();

        Services.AddSingleton(config);
        Services.Configure<GeminiSettings>(config.GetSection("Gemini"));
        return this;
    }

    private void RegisterMutators()
    {
        Services.AddSingleton<IMutationDiscoveryManager, MutationDiscoveryManager>(); 
        Services.AddSingleton<IMutationImplementationProvider, MutationImplementationProvider>();
        Services.AddSingleton<IStartUpProcess, MutatedProjectBuilder>();
        Services.AddSingleton<IMutatedSolutionTester, MutatedSolutionTester>();

        //Specific implementations:
        //Arithmetic:
        Services.AddSingleton<IMutationImplementation, SubtractToAddMutator>();
        Services.AddSingleton<IMutationImplementation, AddToSubtractMutator>();
        Services.AddSingleton<IMutationImplementation, DivideToMultiplyMutator>();
        Services.AddSingleton<IMutationImplementation, MultiplyToDivideMutator>();

        //Relational
        Services.AddSingleton<IMutationImplementation, EqualToNotEqualMutator>();
        Services.AddSingleton<IMutationImplementation, NotEqualToEqualMutator>();
        Services.AddSingleton<IMutationImplementation, GreaterThanOrEqualToLessThanMutator>();
        Services.AddSingleton<IMutationImplementation, GreaterThanToLessThanOrEqualToMutator>();
        Services.AddSingleton<IMutationImplementation, LessThanOrEqualToGreaterThanMutator>();
        Services.AddSingleton<IMutationImplementation, LessThanToGreaterThanOrEqualToMutator>();

        //Unary
        Services.AddSingleton<IMutationImplementation, IncrementToDecrementMutator>();
        Services.AddSingleton<IMutationImplementation, DecrementToIncrementMutator>();

        //Logical
        Services.AddSingleton<IMutationImplementation, AndToOrMutator>();
        Services.AddSingleton<IMutationImplementation, OrToAndMutator>();
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

    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _serviceProvider?.Dispose();
        }

        _disposed = true;
    }
}
