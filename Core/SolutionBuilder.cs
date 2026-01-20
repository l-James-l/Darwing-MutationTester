using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Core;

public class SolutionBuilder : ISolutionBuilder
{
    private readonly ISolutionProvider _solutionProvider;
    private readonly IProcessWrapperFactory _processFactory;
    private readonly IStatusTracker _statusTracker;
    private readonly IMutationSettings _mutationSettings;

    private const int _defaultProcessTimeout = 5;
    private TimeSpan _processTimeout = TimeSpan.FromSeconds(_defaultProcessTimeout);

    private readonly Regex _errorOutPutRegex = new(@"error (\S)*: ", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public SolutionBuilder(IMutationSettings settings, ISolutionProvider solutionProvider,
        IProcessWrapperFactory processFactory, IStatusTracker statusTracker)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(statusTracker);

        _mutationSettings = settings;
        _solutionProvider = solutionProvider;
        _processFactory = processFactory;
        _statusTracker = statusTracker;
    }

    public void InitialBuild()
    {
        if (!_statusTracker.TryStartOperation(DarwingOperation.BuildSolution))
        {
            return;
        }
        if (!_solutionProvider.IsAvailable || _solutionProvider.SolutionContainer is null)
        {
            // Should be impossible to reach this point, but just in case.
            _statusTracker.FinishOperation(DarwingOperation.BuildSolution, false);
            return;
        }

        _processTimeout = TimeSpan.FromSeconds(_mutationSettings.SolutionProfileData?.GeneralSettings.BuildTimeout ?? _defaultProcessTimeout);

        Log.Information("Performing initial build");
        List<IProjectContainer> failedBuilds = [];

        bool wasBuildSuccessful = false;
        try
        {
            TryBuildAllProjects(failedBuilds);

            if (failedBuilds.Count > 0 && !RetryFailedProjectBuilds(failedBuilds))
            {
                Log.Error("Solution build has failed. Cannot perform mutation testing.");
            }
            else
            {
                wasBuildSuccessful = true;
                Log.Information("Building of solution successful.");
            }
        }
        finally
        {
            // Ensure that the status is always updated, even if the build fails or an exception is thrown.
            _statusTracker.FinishOperation(DarwingOperation.BuildSolution, wasBuildSuccessful);
        }
    }

    private void TryBuildAllProjects(List<IProjectContainer> failedBuilds)
    {
        foreach (IProjectContainer proj in _solutionProvider.SolutionContainer.AllProjects)
        {
            Log.Information("Building project: {proj}.", proj.Name);

            var buildingProcess = GenerateBuildProcess(proj.CsprojFilePath);
            bool buildCompleted = buildingProcess.StartAndAwait(_processTimeout);

            if (buildCompleted && buildingProcess.Success)
            {
                Log.Information("{proj} build successful.", proj.Name);
                continue;
            }
            Log.Error("{proj} build failed.", proj.Name);
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
                Log.Error($"Failed to retore dependencies for {projToRetry.Name}");
                LogProcessOutput(projToRetry, restoreProcess);
            }

            Log.Information($"Rebuilding {projToRetry.Name}");

            IProcessWrapper buildRetryProcess = GenerateBuildProcess(projToRetry.CsprojFilePath);
            bool buildCompleted = buildRetryProcess.StartAndAwait(_processTimeout);

            if (!buildCompleted || !buildRetryProcess.Success)
            {
                // Second attempt at building the project failed. Return out, don't bother retrying any other failed projects.
                Log.Error($"Rebuilding {projToRetry.Name} failed.");
                LogProcessOutput(projToRetry, buildRetryProcess);
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
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"build {Path.GetFileName(path)}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(path)
        };

        return _processFactory.Create(startInfo);
    }

    private IProcessWrapper GenerateCleanProcess(string? path)
    {
        ProcessStartInfo startInfo = new()
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
        ProcessStartInfo startInfo = new()
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
