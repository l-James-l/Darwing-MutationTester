using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Mutator;
using Serilog;
using System.Diagnostics;

namespace Core;

public class InitialTestRunner : IMutationRunInitiator
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _mutationSettings;
    private readonly IStatusTracker _statusTracker;
    private readonly IProcessWrapperFactory _processFactory;
    private IMutationDiscoveryManager _mutationDiscoveryManager;

    public InitialTestRunner(IEventAggregator eventAggregator, IMutationSettings mutationSettings, IStatusTracker statusTracker,
        IProcessWrapperFactory processFactory, IMutationDiscoveryManager mutationDiscoveryManager)
    {
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(mutationDiscoveryManager);

        _eventAggregator = eventAggregator;
        _mutationSettings = mutationSettings;
        _statusTracker = statusTracker;
        _processFactory = processFactory;
        _mutationDiscoveryManager = mutationDiscoveryManager;
    }
    
    /// <summary>
    /// When a mutation test run is started, the first step is running all unit test to ensure they all pass
    /// </summary>
    public void Run()
    {
        if (!_statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution))
        {
            return;
        }

        Log.Information("Starting initial test run before mutation begins.");

        InitialTestRunInfo testRunInfo = new();
        try
        {
            PerformTestRun(testRunInfo);
        }
        finally
        {
            _statusTracker.FinishOperation(DarwingOperation.TestUnmutatedSolution, testRunInfo.WasSuccesful);
            _eventAggregator.GetEvent<InitialTestRunCompleteEvent>().Publish(testRunInfo);
            if (testRunInfo.WasSuccesful)
            {
                _mutationDiscoveryManager.PerformMutationDiscovery();
            }
        }
    }

    private void PerformTestRun(InitialTestRunInfo testRunInfo)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"test {Path.GetFileName(_mutationSettings.SolutionPath)} --no-build",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(_mutationSettings.SolutionPath)
        };
        IProcessWrapper testRun = _processFactory.Create(startInfo);

        int? timeout = _mutationSettings.SolutionProfileData?.GeneralSettings.TestRunTimeout;
        bool processSuccess = testRun.StartAndAwait(timeout);

        foreach (string output in testRun.Output)
        {
            Log.Information(output);
        }

        if (!processSuccess || !testRun.Success)
        {
            Log.Error("Initial test run without mutations has failures. Cannot perform mutation testing.");
        }
        else
        {
            Log.Information("Initial test run successful, starting mutant discovery.");
            testRunInfo.WasSuccesful = true;
            testRunInfo.InitialRunDuration = testRun.Duration;
        }
    }
}
