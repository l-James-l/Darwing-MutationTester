using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Events;
using Serilog;

namespace CLI;

public class CLIApp
{
    private const string LoadCommand = "--load";
    private const string BuildCommand = "--build";
    private const string ReloadCommand = "--reload";
    private const string TestCommand = "--test";
    private const string SettingsCommand = "--setting";
    private const string QuitCommand = "--quit";
    private const string HelpCommand = "--help";

    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _mutationSettings;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IWasBuildSuccessfull _buildSuccess;
    private readonly ICancellationTokenWrapper _cancelationToken;


    public CLIApp(IEventAggregator eventAggregator, IMutationSettings mutationSettings, ISolutionProvider solutionProvider,
        ICancelationTokenFactory cancelationTokenFactory, IWasBuildSuccessfull buildSuccess)
    {
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(cancelationTokenFactory);
        ArgumentNullException.ThrowIfNull(buildSuccess);

        _eventAggregator = eventAggregator;
        _mutationSettings = mutationSettings;
        _solutionProvider = solutionProvider;
        _buildSuccess = buildSuccess;
        _cancelationToken = cancelationTokenFactory.Generate();
    }

    public void Run(string[] args)
    {
        Log.Information("Running Darwing CLI application...");

        _mutationSettings.ParseCliArgs(args);

        if (!string.IsNullOrWhiteSpace(_mutationSettings.SolutionPath))
        {
            // If sln specified in command line, load it. Dont care about response here as not possible to get one.
            SolutionLoaderCommand([_mutationSettings.SolutionPath], out _);
        }

        MainLoop();
    }

    private void MainLoop()
    {
        //Using the DI'd cancelation token allows the process to be cancelled from unit tests.
        while (!_cancelationToken.IsCancelled())
        {
            Console.Write("\nDarwing > ");

            // Command should be of format "--type param1 param2...".
            // Should not be multiple on a single line.
            string command = Console.ReadLine() ?? "";
            command = command.Trim();
            Log.Debug($"CLI recieved user command: {command}");
            
            if (command.IndexOf(" ") is int seperator && seperator > 0)
            {
                string commandType = command.Substring(0, seperator);
                //Adding 1 is safe here because we have removed trailing whitespace,
                //so even if there is a single white spcae character, we know there is a character after it.
                string[] commandParams = command.Substring(seperator + 1).Split(" ");
                ParseCommand(commandType, commandParams);
            }
            else
            {
                ParseCommand(command, []);
            }
        }
    }

    private void ParseCommand(string commandType, string[] commandParams)
    {
        string? response = null;
        switch (commandType)
        {
            case LoadCommand:
                SolutionLoaderCommand(commandParams, out response);
                break;
            case BuildCommand:
                //rerun initial build - for when a project was loaded, failed the build, and they want to retry it.
                SolutionBuilderCommand(commandParams, out response);
                break;
            case ReloadCommand:
                SolutionLoaderCommand([_mutationSettings.SolutionPath], out response);
                break;
            case TestCommand:
                //run mutation testing
                InitiateTestSession(commandParams, out response);
                break;
            case SettingsCommand:
                // Ammend a setting
                break;
            case HelpCommand:
                // Output available commands
                HelpOutputCommand();
                break;
            case QuitCommand:
                _cancelationToken.Cancel();
                Log.Information("User requested to exit program. Terminating.");
                break;
            default:
                Log.Information($"Unrecognised command '{commandType}'. Use '{HelpCommand}' to view available commands.");
                break;
        }

        if (response != null)
        {
            Log.Information(response);
        }
    }

    private void SolutionLoaderCommand(string[] commandParams, out string? response)
    {
        response = null;

        if (commandParams.Length != 1)
        {
            response = $"'{LoadCommand}' command takes only 1 paramater, recived with {commandParams.Length}.";
            return;
        }

        string path = commandParams[0];

        _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Publish(new SolutionPathProvidedPayload(path));
    }

    private void SolutionBuilderCommand(string[] commandParams, out string? response)
    {
        response = null;
        if (_solutionProvider.IsAvailable)
        {
            Log.Warning($"Rebuilding the solution does not not reload changes made to the source code since the last load, but they will be included in the build. To indlude changes use command '{ReloadCommand}'");
            _eventAggregator.GetEvent<RequestSolutionBuildEvent>().Publish();
        }
        else
        {
            response = $"No solution has been loaded. Use the '{LoadCommand}' command and then try again";
        }
    }

    private void InitiateTestSession(string[] commandParams, out string? response)
    {
        response = null;

        // Event subscriber will also check these, but by checking them here we can provide feedback to the user.
        if (!_solutionProvider.IsAvailable)
        {
            response = $"No solution has been loaded. Use the '{LoadCommand}' command and then try again";
            return;
        }
        else if (!_buildSuccess.WasLastBuildSuccessful)
        {
            response = $"The previous attempt to build the solution failed. Fix errors and try again.";
            return;
        }

        // TODO parse command params so we can update any overriden settings.
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Publish();
    }

    private void HelpOutputCommand()
    {
        Console.Write($"""
        {LoadCommand}:      Provide a path to a solution file so that Darwing can load the source code and perform a build.
        {BuildCommand}:     Will build the solution at the previsouly provided solution location. Note that will not reload any changes made to the solutions source code since the last build into Darwing, but as the build is perfomred 'in place', the build will include them. In the instance changes to the source code have been made, please use '{ReloadCommand}'.
        {ReloadCommand}:    Will reload the source code for the already loaded solution.
        {TestCommand}:      Will start a mutation run on the loaded code base. Note a loaded solution with a successful build are required.
        {SettingsCommand}:  Change the specified setting to the specified value.
        {QuitCommand}:      Terminate the application.
        {HelpCommand}:      Help command.
        """);
    }
}