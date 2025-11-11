using Models;
using Models.Events;
using Serilog;

namespace CLI;

public class CLIApp
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _mutationSettings;

    public CLIApp(IEventAggregator eventAggregator, IMutationSettings mutationSettings)
    {
        _eventAggregator = eventAggregator;
        _mutationSettings = mutationSettings;
    }

    public void Run(string[] args)
    {
        Log.Information("Running Darwing CLI application...");

        _mutationSettings.ParseCliArgs(args);

        if (string.IsNullOrWhiteSpace(_mutationSettings.SolutionPath))
        {
            Console.Write("\nProvide path to an '.sln' file:    ");

            string? path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path) && _mutationSettings.DevMode)
            {
                //We are in dev mode, so we can use a test path if one want provided.
                path = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";
            }
            else if (string.IsNullOrWhiteSpace(path))
            {
                Log.Error("No solution path provided. Exiting application.");
                return;
            }

            _mutationSettings.SolutionPath = path;
        }

        // We pulish the event with the sln path in the payload, rather than relying on the mutation settings directly, so that
        // in the instance a new sln path is provided later, we can handle that too.
        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload(_mutationSettings.SolutionPath));
    }
}