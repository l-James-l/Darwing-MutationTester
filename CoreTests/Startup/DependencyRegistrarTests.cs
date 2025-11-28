using CLI;
using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Mutator;
using Mutator.MutationImplementations;
using NSubstitute;

namespace CoreTests.Startup;

internal class DependencyRegistrarTests
{
    private IServiceCollection _services;
    private int _expectedRegistrations;

    [SetUp]
    public void Setup()
    {
        _services = Substitute.For<IServiceCollection>();
        _expectedRegistrations = 0;
    }

    [Test]
    public void GivenConstructed_ThenAllDependenciesRegistered()
    {
        //Arrange
        DependencyRegistrar registrar = new TestRegistrar(_services);

        //Act
        registrar.Build();

        //Assert
        AssertRegisterManySingleton<SolutionPathProvidedAwaiter>([typeof(IStartUpProcess), typeof(ISolutionProvider)]);
        AssertBasicRegistartion<EstablishLoggerConfiguration>();
        AssertBasicRegistartion<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        AssertBasicRegistartion<IEventAggregator, EventAggregator>();
        AssertBasicRegistartion<IMutationSettings, MutationSettings>();
        AssertBasicRegistartion<ISolutionProfileDeserializer, SolutionProfileDeserializer>();
        AssertRegisterManySingleton<ProjectBuilder>([typeof(IStartUpProcess), typeof(IWasBuildSuccessfull)]);
        AssertBasicRegistartion<ICancelationTokenFactory, CancelationTokenFactory>();
        AssertBasicRegistartion<IProcessWrapperFactory, ProcessWrapperFactory>();
        AssertBasicRegistartion<IStartUpProcess, InitialTestRunnner>();

        AssertRegisterManySingleton<MutationDiscoveryManager>([typeof(IMutationRunInitiator), typeof(IMutationDiscoveryManager)]);
        AssertBasicRegistartion<IMutationImplementationProvider, MutationImplementationProvider>();
        AssertBasicRegistartion<IStartUpProcess, MutatedProjectBuilder>();

        //IMutationImplementation's
        AssertMutatorRegistration<AddToSubtractMutator>();
        AssertMutatorRegistration<SubtractToAddMutator>();

        _services.ReceivedWithAnyArgs(_expectedRegistrations).Add(default!);
    }


    [Test]
    public void GivenCliConstructed_ThenAllDependenciesRegistered()
    {
        //Arrange
        DependencyRegistrar registrar = new TestCliRegistrar(_services);

        //Act
        registrar.Build();

        //Assert
        AssertBasicRegistartion<CLIApp>();
    }

    private void AssertMutatorRegistration<T>() => AssertBasicRegistartion<IMutationImplementation, T>(true);

    private void AssertBasicRegistartion<T>(bool isSingleton = true) => AssertBasicRegistartion<T, T>(isSingleton);

    private void AssertBasicRegistartion<T1, T2>(bool isSingleton = true)
    {
        _expectedRegistrations++;

        _services.Received(1).Add(Arg.Is<ServiceDescriptor>(x => 
        x.Lifetime == (isSingleton ? ServiceLifetime.Singleton : ServiceLifetime.Transient)
        && x.ImplementationType == typeof(T2)
        && x.ServiceType == typeof(T1)));
    }

    private void AssertRegisterManySingleton<T>(Type[] baseTypes)
    {
        AssertBasicRegistartion<T>();
        foreach (Type type in baseTypes)
        {
            _expectedRegistrations++;

            _services.Received().Add(Arg.Is<ServiceDescriptor>(x =>
            x.Lifetime == ServiceLifetime.Singleton
            && x.ServiceType == type
            && x.ImplementationFactory != null));
        }
        //TODO: Further validate the implementation factory creates the correct instance. Dont currently know how to do this.
        //This also means that where multiple classes are registered against the same class, cant assert this.
    }
}

file class TestCliRegistrar : CliDependencyRegistrar
{
    public TestCliRegistrar(IServiceCollection serviceCollection) : base(serviceCollection)
    {
    }
}

file class TestRegistrar : DependencyRegistrar
{
    public TestRegistrar(IServiceCollection serviceCollection) : base(serviceCollection)
    {
    }
}
