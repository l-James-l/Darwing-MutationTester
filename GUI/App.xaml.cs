using GUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        IServiceProvider serviceProvider = new GuiDependecyRegistrar(new ServiceCollection()).Build();
        MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindowViewModel mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.Show();
    }

}
