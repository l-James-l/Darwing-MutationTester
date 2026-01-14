using CLI;
using Microsoft.Extensions.DependencyInjection;
using Models.Exceptions;

/// <summary>
/// Main entry point for the CLI application.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        IServiceProvider serviceProvider = new CliDependencyRegistrar(new ServiceCollection()).Build();
        CLIApp cLIApp = serviceProvider.GetRequiredService<CLIApp>();
        cLIApp.Run(args);
    }
}
