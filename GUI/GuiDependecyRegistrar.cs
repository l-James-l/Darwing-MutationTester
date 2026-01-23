using Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using GUI.ViewModels;
using GUI.Services;
using GUI.ViewModels.ElementViewModels;

namespace GUI;

public class GuiDependecyRegistrar : DependencyRegistrar
{
    public GuiDependecyRegistrar(IServiceCollection serviceCollection) : base(serviceCollection) { }

    protected override void RegisterLocalDependencies()
    {
        Services.AddSingleton<MainWindow>();

        RegisterViewModels();
        RegisterServices();
    }

    private void RegisterServices()
    {
        Services.AddSingleton<IFileSelectorService, FileSelectorService>();
    }

    private void RegisterViewModels()
    {
        Services.AddSingleton<MainWindowViewModel>();
        Services.AddSingleton<IDashBoardViewModel, DashBoardViewModel>();
        Services.AddSingleton<ISettingsViewModel, SettingsViewModel>();
        Services.AddSingleton<ISolutionExplorerViewModel, SolutionExplorerViewModel>();
        Services.AddSingleton<StatusBarViewModel>();
        Services.AddSingleton<MutationScoreByProjectViewModel>();
    }
}