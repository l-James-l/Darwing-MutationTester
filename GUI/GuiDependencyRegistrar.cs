using Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using GUI.ViewModels;
using GUI.Services;
using GUI.ViewModels.DashBoardElements;
using GUI.ViewModels.SolutionExplorerElements;

namespace GUI;

public class GuiDependencyRegistrar : DependencyRegistrar
{
    public GuiDependencyRegistrar(IServiceCollection serviceCollection) : base(serviceCollection) { }

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
        Services.AddSingleton<SummaryCountsViewModel>();
        Services.AddSingleton<FileExplorerViewModel>();
    }
}