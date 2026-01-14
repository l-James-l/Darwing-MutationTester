using Core.IndustrialEstate;
using Core.Interfaces;
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
    private readonly IProcessWrapperFactory _processFactory;
    private readonly IMutationSettings _mutationSettings;

    private const int _defaultProcessTimeout = 5;
    private TimeSpan _processTimeout = TimeSpan.FromSeconds(_defaultProcessTimeout);

    private readonly Regex _errorOutPutRegex = new(@"error (\S)*: ", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public bool WasLastBuildSuccessful { get; private set; } = false;

    public ProjectBuilder(IMutationSettings settings, IEventAggregator eventAggregator, ISolutionProvider solutionProvider,
        IProcessWrapperFactory processFactory)
    {
        _mutationSettings = settings;
        _eventAggrgator = eventAggregator;
        _solutionProvider = solutionProvider;
        _processFactory = processFactory;
    }

    public void StartUp()
    {
        _eventAggrgator.GetEvent<RequestSolutionBuildEvent>().Subscribe(InitialBuild, ThreadOption.BackgroundThread, true);
    }

    private void InitialBuild()
    {
        WasLastBuildSuccessful = false;
        if (!_solutionProvider.IsAvailable || _solutionProvider.SolutionContiner is null)
        {
            return;
        }

        _processTimeout = TimeSpan.FromSeconds(_mutationSettings.SolutionProfileData?.GeneralSettings.BuildTimeout ?? _defaultProcessTimeout);

        // The build process will have exit code 1 if the build failed.
        Log.Information("Performing initial build");
        List<IProjectContainer> failedBuilds = new();

        TryBuildAllProjects(failedBuilds);
        
        if (failedBuilds.Count > 0 && !RetryFailedProjectBuilds(failedBuilds))
        {
            Log.Error("Solution build has failed. Cannot perform mutation testing.");
        }
        else
        {
            WasLastBuildSuccessful = true;
            Log.Information("Building of solution succesful.");
        }
    }

    private void TryBuildAllProjects(List<IProjectContainer> failedBuilds)
    {
        foreach (IProjectContainer proj in _solutionProvider.SolutionContiner.AllProjects)
        {
            Log.Information("Building project: " + proj.Name);

            var buildingProcess = GenerateBuildProcess(proj.CsprojFilePath);
            bool buildCompleted = buildingProcess.StartAndAwait(_processTimeout);

            if (buildCompleted && buildingProcess.Success)
            {
                Log.Information($"{proj.Name} build succesful");
                continue;
            }
            Log.Error($"{proj.Name} build failed");
            failedBuilds.Add(proj);
            LogProcessOutput(proj, buildingProcess);
        }
    }

    private bool RetryFailedProjectBuilds(List<IProjectContainer> failedBuilds)
    {
        Log.Information("Retrying builds for failed projects.");

        foreach (IProjectContainer projToRetry in failedBuilds)
        {
            Log.Information($"Cleaning {projToRetry.Name}");

            IProcessWrapper cleaningProcess = GenerateCleanProcess(projToRetry.CsprojFilePath);
            bool cleaningCompleted = cleaningProcess.StartAndAwait(_processTimeout);
            
            if (!cleaningCompleted || !cleaningProcess.Success)
            {
                Log.Error($"Failed to clean {projToRetry.Name}");
                LogProcessOutput(projToRetry, cleaningProcess);
            }

            Log.Information($"Restoring dependencies for {projToRetry.Name}");

            IProcessWrapper restoreProcess = GenerateRestoreProcess(projToRetry.CsprojFilePath);
            bool restoreCompleted = restoreProcess.StartAndAwait(_processTimeout);

            if (!restoreCompleted || !cleaningProcess.Success)
            {
                Log.Error($"Failed to retore dependecies for {projToRetry.Name}");
                LogProcessOutput(projToRetry, restoreProcess);
            }

            Log.Information($"Rebuilding {projToRetry.Name}");

            IProcessWrapper buildRetyProcess = GenerateBuildProcess(projToRetry.CsprojFilePath);
            bool buildCompleted = buildRetyProcess.StartAndAwait(_processTimeout);

            if (!buildCompleted || !buildRetyProcess.Success)
            {
                // Second attempt at building the project failed. Return out, dont bother retrying any other failed projects.
                Log.Error($"Rebuilding {projToRetry.Name} failed.");
                LogProcessOutput(projToRetry, buildRetyProcess);
                return false;
            }

            Log.Information($"Retrying build for {projToRetry.Name} was successful");
        }

        return true;
    }

    private void LogProcessOutput(IProjectContainer proj, IProcessWrapper process)
    {
        if (process.Errors.Count > 0)
        {
            Log.Information($"Errors encountered while trying to build {proj.Name}. Will clean and retry");
            process.Errors.ForEach(Log.Debug);
        }

        foreach (string output in process.Output)
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

    private IProcessWrapper GenerateBuildProcess(string? path)
    {
        ProcessStartInfo startinfo = new()
        {
            FileName = "dotnet",
            Arguments = $"build {Path.GetFileName(path)}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(path)
        };

        return _processFactory.Create(startinfo);
    }

    private IProcessWrapper GenerateCleanProcess(string? path)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"clean {Path.GetFileName(path)}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(path)
        };

        return _processFactory.Create(startInfo);
    }

    private IProcessWrapper GenerateRestoreProcess(string? path)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"restore {Path.GetFileName(path)}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(path)
        };

        return _processFactory.Create(startInfo);
    }
}
