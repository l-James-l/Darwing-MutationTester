using Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Title = "Darwing GUI";

        //new DependencyRegistrar(new ServiceCollection()).Build();
    }
}