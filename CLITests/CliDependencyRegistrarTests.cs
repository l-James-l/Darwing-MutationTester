using CLI;
using Core.Startup;
using CoreTests.Startup;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CLITests;
public class CliDependencyRegistrarTests : DepencyRegisrationTestsHelper
{
    [SetUp]
    public void Setup()
    {
        _services = Substitute.For<IServiceCollection>();
    }

    [Test]
    public void GivenCliConstructed_ThenAllDependenciesRegistered()
    {
        //Arrange
        DependencyRegistrar registrar = new CliDependencyRegistrar(_services!);

        //Act
        registrar.Build();

        //Assert
        AssertBasicRegistartion<CLIApp>();
    }
}
