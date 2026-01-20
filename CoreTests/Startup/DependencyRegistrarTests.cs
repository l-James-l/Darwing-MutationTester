using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Models.SharedInterfaces;
using Mutator;
using Mutator.MutationImplementations;
using NSubstitute;

namespace CoreTests.Startup;

internal class DependencyRegistrarTests : DepencyRegisrationTestsHelper
{
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
        DependencyRegistrar registrar = new TestRegistrar(_services!);

        //Act
        registrar.Build();

        //Assert
        AssertBasicRegistartion<ISolutionLoader, SolutionLoader>();
        AssertBasicRegistartion<ISolutionProvider, SolutionProvider>();
        AssertBasicRegistartion<EstablishLoggerConfiguration>();
        AssertBasicRegistartion<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        AssertBasicRegistartion<IEventAggregator, EventAggregator>();
        AssertBasicRegistartion<IStatusTracker, StatusTracker>();
        AssertBasicRegistartion<IMutationSettings, MutationSettings>();
        AssertBasicRegistartion<ISolutionProfileDeserializer, SolutionProfileDeserializer>();
        AssertBasicRegistartion<ISolutionBuilder, SolutionBuilder>();
        AssertBasicRegistartion<ICancelationTokenFactory, CancelationTokenFactory>();
        AssertBasicRegistartion<IProcessWrapperFactory, ProcessWrapperFactory>();
        AssertBasicRegistartion<IMutationRunInitiator, InitialTestRunner>();
        AssertRegisterManySingleton<MutatedSolutionTester>([typeof(IStartUpProcess), typeof(IMutatedSolutionTester)]);

        AssertBasicRegistartion<IMutationDiscoveryManager, MutationDiscoveryManager>();
        AssertBasicRegistartion<IMutationImplementationProvider, MutationImplementationProvider>();
        AssertBasicRegistartion<IStartUpProcess, MutatedProjectBuilder>();

        //IMutationImplementation's
        AssertMutatorRegistration<AddToSubtractMutator>();
        AssertMutatorRegistration<SubtractToAddMutator>();

        _services!.ReceivedWithAnyArgs(_expectedRegistrations).Add(default!);
    }
}


file class TestRegistrar : DependencyRegistrar
{
    public TestRegistrar(IServiceCollection serviceCollection) : base(serviceCollection)
    {
    }
}
