using CLI;
using Core;
using Core.IndustrialEstate;
using Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CoreTests.Startup;

internal class DependencyRegistrarTests
{
    private IServiceCollection _services;

    [SetUp]
    public void Setup()
    {
        _services = Substitute.For<IServiceCollection>();
    }

    [Test]
    public void GivenConstructed_ThenAllDependenciesRegistered()
    {
        //Arrange
        DependencyRegistrar registrar = new TetsRegistrar(_services);

        //Act
        registrar.Build();

        //Assert
        AssertRegisterManySingleton<SolutionPathProvidedAwaiter>([typeof(IStartUpProcess), typeof(ISolutionProvider)]);
        AssertBasicRegistartion<EstablishLoggerConfiguration>();
        AssertBasicRegistartion<IAnalyzerManagerFactory, AnalyzerManagerFactory>();
        AssertBasicRegistartion<IEventAggregator, EventAggregator>();
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

    private void AssertBasicRegistartion<T>(bool isSingleton = true) => AssertBasicRegistartion<T, T>(isSingleton);

    private void AssertBasicRegistartion<T1, T2>(bool isSingleton = true)
    {
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
            _services.Received(1).Add(Arg.Is<ServiceDescriptor>(x =>
            x.Lifetime == ServiceLifetime.Singleton
            && x.ServiceType == type
            && x.ImplementationFactory != null));
        }
        //TODO: Further validate the implementation factory creates the correct instance. Dont currently know how to do this.
    }
}

file class TestCliRegistrar : CliDependencyRegistrar
{
    public TestCliRegistrar(IServiceCollection serviceCollection) : base(serviceCollection)
    {
    }
}

file class TetsRegistrar : DependencyRegistrar
{
    public TetsRegistrar(IServiceCollection serviceCollection) : base(serviceCollection)
    {
    }
}
