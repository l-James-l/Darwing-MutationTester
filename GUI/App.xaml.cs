using GUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
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

        // This allows viewing of backend logs in real time when configured from launch settings
        if (e.Args.Length > 0 && e.Args[0] == "--console")
        {
            ShowConsole();
        }

        MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindowViewModel mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.Show();
    }

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    public void ShowConsole()
    {
        AllocConsole();
        Console.WriteLine("GUI launching console for debugging. Dev mode only.");
    }

}
