using Models.Events;
using Serilog;

public class CLIApp
{
    private readonly IEventAggregator _eventAggregator;

    public CLIApp(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    public void Run(string[] args)
    {
        Log.Information("Running Darwing CLI application...");
        // Implement CLI logic here
        Console.Write("\nProvide path to an '.sln' file:    ");

        string? path = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(path) && args.Any(x => x == "--dev")) 
        {
            //We are in dev mode, so we can use a test path if one want provided.
            path = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";
        }

        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload(path));
    }
}