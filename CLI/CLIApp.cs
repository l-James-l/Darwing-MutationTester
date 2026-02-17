using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Mutator;
using Serilog;
using System.ComponentModel;
using System.Reflection;

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
    private const string ReportCommand = "--report"; 

    private readonly IMutationSettings _mutationSettings;
    private readonly IStatusTracker _statusTracker;
    private readonly ISolutionLoader _solutionLoader;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly IMutationRunInitiator _mutationRunInitiator;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;
    private readonly ISolutionProvider _solutionProvider;
    private readonly ICancellationTokenWrapper _cancelationToken;


    public CLIApp(IMutationSettings mutationSettings, IStatusTracker statusTracker,
        ICancelationTokenFactory cancelationTokenFactory, ISolutionLoader solutionLoader,
        ISolutionBuilder solutionBuilder, IMutationRunInitiator mutationRunInitiator, 
        IMutationDiscoveryManager mutationDiscoveryManager, ISolutionProvider solutionProvider)
    {
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(cancelationTokenFactory);
        ArgumentNullException.ThrowIfNull(solutionLoader);
        ArgumentNullException.ThrowIfNull(solutionBuilder);
        ArgumentNullException.ThrowIfNull(mutationRunInitiator);

        _mutationSettings = mutationSettings;
        _statusTracker = statusTracker;
        _solutionLoader = solutionLoader;
        _solutionBuilder = solutionBuilder;
        _mutationRunInitiator = mutationRunInitiator;
        _mutationDiscoveryManager = mutationDiscoveryManager;
        _solutionProvider = solutionProvider;
        _cancelationToken = cancelationTokenFactory.Generate();
    }

    public void Run(string[] args)
    {
        Log.Information("Running Darwing CLI application...");

        _mutationSettings.ParseCliArgs(args);

        if (!string.IsNullOrWhiteSpace(_mutationSettings.SolutionPath))
        {
            // If sln specified in command line, load it. Don't care about response here as not possible to get one.
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
            Log.Debug($"CLI received user command: {command}");
            
            if (command.IndexOf(" ") is int separator && separator > 0)
            {
                string commandType = command.Substring(0, separator);
                //Adding 1 is safe here because we have removed trailing whitespace,
                //so even if there is a single white space character, we know there is a character after it.
                string[] commandParams = command.Substring(separator + 1).Split(" ");
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
                SolutionBuilderCommand(out response);
                break;
            case ReloadCommand:
                SolutionLoaderCommand([_mutationSettings.SolutionPath], out response);
                break;
            case TestCommand:
                //run mutation testing
                InitiateTestSession(commandParams, out response);
                break;
            case SettingsCommand:
                // Amend a setting
                break;
            case ReportCommand:
                ReportResults(commandParams);
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
            response = $"'{LoadCommand}' command takes only 1 parameter, received with {commandParams.Length}.";
            return;
        }
        
        string path = commandParams[0];
        _solutionLoader.Load(path);
    }

    private void SolutionBuilderCommand(out string? response)
    {
        response = null;
        if (_statusTracker.CheckStatus(DarwingOperation.LoadSolution) is not OperationStates.Succeeded)
        {
            response = $"No solution has been loaded. Use the '{LoadCommand}' command and then try again";
            return;
        }
        if (_statusTracker.CheckStatus(DarwingOperation.BuildSolution) is not OperationStates.NotStarted)
        {
            Log.Warning($"Rebuilding the solution does not not reload changes made to the source code since the last load, but they will be included in the build. To include changes use command '{ReloadCommand}'");
        }
        _solutionBuilder.InitialBuild();
    }

    private void InitiateTestSession(string[] commandParams, out string? response)
    {
        response = null;

        // Event subscriber will also check these, but by checking them here we can provide feedback to the user.
        if (_statusTracker.CheckStatus(DarwingOperation.LoadSolution) is not OperationStates.Succeeded)
        {
            response = $"No solution has been loaded. Use the '{LoadCommand}' command and then try again";
            return;
        }
        else if (_statusTracker.CheckStatus(DarwingOperation.BuildSolution) is not OperationStates.Succeeded)
        {
            response = $"The previous attempt to build the solution failed. Fix errors and try again.";
            return;
        }

        PropertyInfo[] propertyInfos = typeof(IMutationSettings).GetProperties();
        foreach (string setting in commandParams)
        {
            Log.Information("parsing setting override: {setting}", setting);
            string[] settingPair = setting.Split("=");
            if (settingPair.Length != 2)
            {
                Log.Warning("Could not parse setting override: {setting}. Setting overrides should be in the format 'SettingName=SettingValue'", setting);
                continue;
            }
            string settingName = settingPair[0];
            string settingStringValue = settingPair[1];

            //get the property corresponding to the setting name
            PropertyInfo? propertyInfo = propertyInfos.FirstOrDefault(x => string.Equals(x.Name, settingPair.First(), StringComparison.CurrentCultureIgnoreCase));
            if (propertyInfo is null)
            {
                Log.Warning("Could not find setting with name: {name}", settingName);
                continue;
            }

            try
            {
                object? parsedValue = ParseValue(settingStringValue, propertyInfo.PropertyType);
                propertyInfo.SetValue(_mutationSettings, parsedValue);
                Log.Information("overridden setting: {name}", settingName);
                HandleProjectTypeSettingUpdate(propertyInfo.Name, parsedValue is List<string> list ? list : []);
            }
            catch
            {
                Log.Error("Could not parse setting: {name}, with value: {value}", settingName, settingStringValue);
            }
        }

        _mutationRunInitiator.Run();

        int totalCount = _mutationDiscoveryManager.DiscoveredMutations.Count(x => x.Status.IncludeInTotalCount());
        int killedCount = _mutationDiscoveryManager.DiscoveredMutations.Count(x => x.Status.IncludeInKilledCount());
        int survivedCount = _mutationDiscoveryManager.DiscoveredMutations.Count(x => x.Status.IncludeInSurvivedCount());
        Log.Information("Mutation testing complete. Total mutants discovered: {TotalCount}", totalCount);
        Log.Information("Killed mutants: {KilledCount}", killedCount);
        Log.Information("Survived mutants: {SurvivedCount}", survivedCount);

        Log.Information("For more detailed reporting, use the --report command.");
    }

    private void HandleProjectTypeSettingUpdate(string settingName, List<string> newValues)
    {
        if (settingName is nameof(IMutationSettings.SourceCodeProjects) or nameof(IMutationSettings.TestProjects) or nameof(IMutationSettings.IgnoreProjects))
        {
            // If the project type settings have been updated, we need to update the project types for each project, as they are used to determine which mutators to apply to each project.
            foreach (string projName in newValues)
            {
                if (_solutionProvider.SolutionContainer.AllProjects.FirstOrDefault(x => projName.ToLower() == x.Name.ToLower()) is { } changedProject)
                {
                    changedProject.ProjectType = (settingName) switch
                    {
                        nameof(IMutationSettings.TestProjects) => ProjectType.Test,
                        nameof(IMutationSettings.SourceCodeProjects) => ProjectType.Source,
                        nameof(IMutationSettings.IgnoreProjects) => ProjectType.Ignore, 
                        _ => changedProject.ProjectType
                    };
                    Log.Information("Project {ProjectName} set to type {type}", changedProject.Name, changedProject.ProjectType);
                }
            }
        }
    }

    private object? ParseValue(string value, Type targetType)
    {
        // 1. Handle Lists (e.g., [proj1, proj2])
        if (targetType == typeof(List<string>))
        {
            return value.Trim('[', ']')
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
        }

        // 2. Handle Enums
        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, true);
        }

        // 3. Handle Primitives (int, bool, string, etc.)
        TypeConverter converter = TypeDescriptor.GetConverter(targetType);
        return converter.ConvertFromInvariantString(value);
    }

    private void ReportResults(string[] commandParams)
    {
        IEnumerable<IGrouping<string, DiscoveredMutation>> allGroupings = _mutationDiscoveryManager.DiscoveredMutations.GroupBy(x => Path.GetFileName(x.LineSpan.Path));
        if (commandParams.Length == 0)
        {
            // No params means report on everything.
            Log.Information("Reporting on all tests:");
            foreach (IGrouping<string, DiscoveredMutation> fileGrouping in allGroupings)
            {
                Log.Information("File: {FileName}", fileGrouping.Key);
                foreach (DiscoveredMutation mutation in fileGrouping)
                {
                    Log.Information("Line {LineNumber}: {Status}", mutation.LineSpan.StartLinePosition.Line + 1, mutation.Status);
                    Log.Information("Original code: {OriginalCode}", mutation.OriginalNode.ToString().Trim());
                    Log.Information("Mutated code: {MutatedCode}", mutation.MutatedNode.ToString().Trim());
                    Log.Information(""); // Blank line for readability.
                }
            }
        }
        else
        {
            foreach (string item in commandParams)
            {
                IGrouping<string, DiscoveredMutation>? fileGrouping = allGroupings.FirstOrDefault(x => x.Key == item);
                if (fileGrouping is not null)
                {
                    Log.Information("File: {FileName}", fileGrouping.Key);
                    foreach (DiscoveredMutation mutation in fileGrouping)
                    {
                        Log.Information("Line {LineNumber}: {Status}", mutation.LineSpan.StartLinePosition.Line + 1, mutation.Status);
                        Log.Information("Original code: {OriginalCode}", mutation.OriginalNode.ToString().Trim());
                        Log.Information("Mutated code: {MutatedCode}", mutation.MutatedNode.ToString().Trim());
                        Log.Information(""); // Blank line for readability.
                    }
                }
            }
        }
    }

    private void HelpOutputCommand()
    {
        Console.Write($"""
        {LoadCommand}:      Provide a path to a solution file so that Darwing can load the source code and perform a build.
        {BuildCommand}:     Will build the solution at the previously provided solution location. Note that will not reload any changes made to the solutions source code since the last build into Darwing, but as the build is performed 'in place', the build will include them. In the instance changes to the source code have been made, please use '{ReloadCommand}'.
        {ReloadCommand}:    Will reload the source code for the already loaded solution.
        {TestCommand}:      Will start a mutation run on the loaded code base. Note a loaded solution with a successful build are required. Can specify settings to override with the format: 'SettingNme=SettingValue'
        {SettingsCommand}:  Change the specified setting to the specified value.
        {ReportCommand}:    Report on results. If no params specified, all mutations reported. Can specify file names, so that only mutations in that file are reported.
        {QuitCommand}:      Terminate the application.
        {HelpCommand}:      Help command.
        """);
    }
}