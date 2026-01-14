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
        AssertBasicRegistartion<IStartUpProcess, MutatedSolutionTester>();

        AssertRegisterManySingleton<MutationDiscoveryManager>([typeof(IMutationRunInitiator), typeof(IMutationDiscoveryManager)]);
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
