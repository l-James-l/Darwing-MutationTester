using CoreTests.Startup;
using GUI;
using GUI.Services;
using GUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace GuiTests;

public class GuiDependencyRegistrarTests : DepencyRegisrationTestsHelper
{
    [SetUp]
    public void Setup()
    {
        _services = Substitute.For<IServiceCollection>();
    }

    [Test]
    public void GivenGuiConstructed_ThenDependenciesRegistered()
    {
        AssertBasicRegistartion<MainWindow>();
        AssertBasicRegistartion<MainWindowViewModel>();
        AssertBasicRegistartion<IDashBoardViewModel, DashBoardViewModel>();
        AssertBasicRegistartion<ISolutionExplorerViewModel, SolutionExplorerViewModel>();
        AssertBasicRegistartion<ISettingsViewModel, SettingsViewModel>();
        AssertBasicRegistartion<IFileSelectorService, FileSelectorService>();
    }
}
