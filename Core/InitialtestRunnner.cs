using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Events;
using Mutator;
using Serilog;
using System.Diagnostics;

namespace Core;

public class InitialTestRunnner : IStartUpProcess
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _mutationSettings;
    private readonly IWasBuildSuccessfull _wasBuildSuccessfull;
    private readonly IProcessWrapperFactory _processFactory;
    private IMutationRunInitiator _mutationRunManager;

    public InitialTestRunnner(IEventAggregator eventAggregator, IMutationSettings mutationSettings, IWasBuildSuccessfull wasBuildSuccessfull,
        IProcessWrapperFactory processFactory, IMutationRunInitiator mutationRunManager)
    {
        _eventAggregator = eventAggregator;
        _mutationSettings = mutationSettings;
        _wasBuildSuccessfull = wasBuildSuccessfull;
        _processFactory = processFactory;
        _mutationRunManager = mutationRunManager;
    }

    public void StartUp()
    {
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Subscribe(InitialTestRun);
    }
    
    /// <summary>
    /// When a mutaiotn test run is started, the first step is running all unit test to ensure they all pass
    /// </summary>
    private void InitialTestRun()
    {
        InitialTestRunInfo testRunInfo = new InitialTestRunInfo();

        //By checking we have a succesful build, we implicitly know that there is a solution loaded.
        if (!_wasBuildSuccessfull.WasLastBuildSuccessful)
        {
            Log.Error("Attempted to start a mutation run without a successful build");
            return;
        }
        
        Log.Information("Starting initial test run before mutation begins.");

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"test {Path.GetFileName(_mutationSettings.SolutionPath)} --no-build",
            RedirectStandardError = true,
            UseShellExecute = false,
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
            Log.Error("Initial test run wihout mutations has failures. Cannot perform mutation testing.");
        }
        else
        {
            Log.Information("Initial test run successful, starting mutant discovery.");
            testRunInfo.WasSuccesful = true;
            testRunInfo.InitialRunDuration = testRun.Duration;
            _mutationRunManager.Run(testRunInfo);
        }
    }
}
