using Core.Interfaces;
using Core.Startup;
using Microsoft.CodeAnalysis;
using Models;
using Models.Events;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Core;

public class ProjectBuilder : IStartUpProcess, IWasBuildSuccessfull
{
    private IEventAggregator _eventAggrgator;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IMutationSettings _mutationSettings;

    private const int _defaultProcessTimeout = 5;
    private TimeSpan _processTimeout = TimeSpan.FromSeconds(_defaultProcessTimeout);

    private readonly Regex _errorOutPutRegex = new(@"error (\S)*: ", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public bool WasLastBuildSuccessful { get; private set; } = false;

    public ProjectBuilder(IMutationSettings settings, IEventAggregator eventAggregator, ISolutionProvider solutionProvider)
    {
        _mutationSettings = settings;
        _eventAggrgator = eventAggregator;
        _solutionProvider = solutionProvider;
    }

    public void StartUp()
    {
        _eventAggrgator.GetEvent<RequestSolutionBuildEvent>().Subscribe(InitialBuild);
    }

    private void InitialBuild()
    {
        if (!_solutionProvider.IsAvailable || _solutionProvider.SolutionContiner is not { } solution)
        {
            return;
        }

        _processTimeout = TimeSpan.FromSeconds(_mutationSettings.SolutionProfileData?.GeneralSettings.BuildTimeout ?? _defaultProcessTimeout);

        // The build process will have exit code 1 if the build failed.
        Log.Information("Performing initial build");
        List<Project> failedBuilds = new();

        TryBuildAllProjects(solution, failedBuilds);
        
        if (failedBuilds.Count > 0 && !RetryFailedProjectBuilds(failedBuilds))
        {
            WasLastBuildSuccessful = false;
            Log.Error("Solution build has failed. Cannot perform mutation testing.");
        }
        else
        {
            WasLastBuildSuccessful = true;
            Log.Information("Building of solution succesful.");
        }
    }
    private void TryBuildAllProjects(SolutionContainer solution, List<Project> failedBuilds)
    {
        foreach (Project proj in solution.AllProjects)
        {
            Log.Information("Building project: " + proj.Name);

            var buildingProcess = GenerateBuildProcess(proj.FilePath);
            buildingProcess.Start();
            bool buildCompleted = buildingProcess.WaitForExit(_processTimeout);

            if (buildCompleted && buildingProcess.ExitCode == 0)
            {
                Log.Information($"{proj.Name} build succesful");
                continue;
            }
            Log.Information($"{proj.Name} build failed");
            failedBuilds.Add(proj);

            if (buildingProcess.StandardError.ReadToEnd() is string errorsOutput)
            {
                Log.Information($"Errors encountered while trying to build {proj.Name}. Will clean and retry");
                Log.Debug(errorsOutput);
            }

            while (buildingProcess.StandardOutput.ReadLine() is { } output)
            {
                if (_errorOutPutRegex.IsMatch(output))
                {
                    Log.Information(output);
                }
                else
                {
                    Log.Debug(output);
                }
            }
        }
    }

    private bool RetryFailedProjectBuilds(List<Project> failedBuilds)
    {
        Log.Information("Retrying builds for failed projects.");

        foreach (Project projToRetry in failedBuilds)
        {
            Log.Information($"Cleaning {projToRetry.Name}");
            //Clean the project
            Process cleaningProcess = GenerateCleanProcess(projToRetry.FilePath);
            cleaningProcess.Start();
            bool cleaningCompleted = cleaningProcess.WaitForExit(_processTimeout);
            if (!cleaningCompleted || cleaningProcess.ExitCode != 0)
            {
                Log.Information($"Failed to clean {projToRetry.Name}");
                return false;
            }

            Log.Information($"Restoring dependencies for {projToRetry.Name}");
            //Restore project dependencies
            Process restoreProcess = GenerateRestoreProcess(projToRetry.FilePath);
            restoreProcess.Start();
            bool restoreCompleted = restoreProcess.WaitForExit(_processTimeout);
            if (!restoreCompleted || cleaningProcess.ExitCode != 0)
            {
                Log.Information($"Failed to retore dependecies for {projToRetry.Name}");
                return false;
            }

            Log.Information($"Rebuilding {projToRetry.Name}");
            //Retry the build
            Process buildRetyProcess = GenerateBuildProcess(projToRetry.FilePath);
            buildRetyProcess.Start();
            bool buildCompleted = buildRetyProcess.WaitForExit(_processTimeout);

            if (!buildCompleted || buildRetyProcess.ExitCode != 0)
            {
                // Second attempt at building the project failed. Return out, dont bother retrying any other failed projects.
                Log.Information($"Rebuilding {projToRetry.Name} failed.");
                return false;
            }

            Log.Information($"Retrying build for {projToRetry.Name} was successful");
        }

        return true;
    }

    private Process GenerateBuildProcess(string? path)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build {Path.GetFileName(path)}",
                RedirectStandardError = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(path)
            }
        };
    }

    private Process GenerateCleanProcess(string? path)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"clean {Path.GetFileName(path)}",
                RedirectStandardError = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(path)
            }
        };
    }

    private Process GenerateRestoreProcess(string? path)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore {Path.GetFileName(path)}",
                RedirectStandardError = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(path)
            }
        };
    }
}
