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
        //Create a new container to pass to the DependencyRegistrar

        IServiceProvider serviceProvider = new CliDependencyRegistrar(new ServiceCollection()).Build();
        CLIApp cLIApp = serviceProvider.GetService<CLIApp>() ?? throw new FatalException("Failed to resolve CLI");
        cLIApp.Run(args);
    }
}
