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
using System.Reflection;

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
        AssertBasicRegistration<ISolutionLoader, SolutionLoader>();
        AssertBasicRegistration<ISolutionProvider, SolutionProvider>();
        AssertBasicRegistration<EstablishLoggerConfiguration>();
        AssertBasicRegistration<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        AssertBasicRegistration<IEventAggregator, EventAggregator>();
        AssertBasicRegistration<IStatusTracker, StatusTracker>();
        AssertBasicRegistration<IMutationSettings, MutationSettings>();
        AssertBasicRegistration<ISolutionProfileDeserializer, SolutionProfileDeserializer>();
        AssertBasicRegistration<ISolutionBuilder, SolutionBuilder>();
        AssertBasicRegistration<ICancelationTokenFactory, CancelationTokenFactory>();
        AssertBasicRegistration<IProcessWrapperFactory, ProcessWrapperFactory>();
        AssertBasicRegistration<IMutationRunInitiator, InitialTestRunner>();
        AssertBasicRegistration<IMutatedSolutionTester, MutatedSolutionTester>();
        AssertBasicRegistration<IGitDiffManager, GitDiffManager>();
        AssertBasicRegistration<IGeminiChatClientFactory, GeminiChatClientFactory>();
        AssertBasicRegistration<IGeminiApiHandler, GeminiApiHandler>();
        AssertBasicRegistration<ICoverageMapper, CoverageMapper>();
        AssertBasicRegistration<IMutationDiscoveryManager, MutationDiscoveryManager>();
        AssertBasicRegistration<IMutationImplementationProvider, MutationImplementationProvider>();
        AssertBasicRegistration<IStartUpProcess, MutatedProjectBuilder>();

        //Assert all IMutationImplementation's are registered
        List<Type> implementations = Assembly.GetAssembly(typeof(IMutationImplementation))!
            .GetTypes()
            .Where(t => typeof(IMutationImplementation).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();
        foreach (Type mutationImplementation in implementations)
        {
            AssertBasicRegistration(typeof(IMutationImplementation), mutationImplementation);
        }

        _services!.ReceivedWithAnyArgs(_expectedRegistrations).Add(default!);
    }
}


file class TestRegistrar : DependencyRegistrar
{
    public TestRegistrar(IServiceCollection serviceCollection) : base(serviceCollection)
    {
    }
}
