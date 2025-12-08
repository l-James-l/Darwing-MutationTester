using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.Events;
using Mutator.MutationImplementations;
using Serilog;
using System.Diagnostics;

namespace Mutator;

public class MutatedSolutionTester : IStartUpProcess
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;
    private readonly IProcessWrapperFactory _processFactory;
    private readonly IMutationSettings _mutationSettings;

    private InitialTestRunInfo? _initialTestRunInfo;
    
    public MutatedSolutionTester(IEventAggregator eventAggregator, IMutationDiscoveryManager mutationDiscoveryManager, IProcessWrapperFactory processFactory, 
        IMutationSettings mutationSettings)
    {
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(mutationDiscoveryManager);
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(mutationSettings);

        _eventAggregator = eventAggregator;
        _mutationDiscoveryManager = mutationDiscoveryManager;
        _processFactory = processFactory;
        _mutationSettings = mutationSettings;
    }

    public void StartUp()
    {
        _eventAggregator.GetEvent<InitialTestRunCompleteEvent>().Subscribe(x => _initialTestRunInfo = x);
        _eventAggregator.GetEvent<TestMutatedSolutionEvent>().Subscribe(RunTestsOnMutatedSolution);
    }

    private void RunTestsOnMutatedSolution()
    {
        if (_initialTestRunInfo == null)
        {
            Log.Error("Initial test run info was never recieved. Cannot do mutation testing based on coverage or execution time. " +
                "All test will now be run for each mutation which may result in slower execution.");
        }

        if (DoInitialTestWithNoActiveMutants())
        {
            IEnumerable<DiscoveredMutation> availableMutants = _mutationDiscoveryManager.DiscoveredMutations.Where(x => x.Status is MutantStatus.Available);

            int survivedMutants = 0;
            int testedMutantCount = availableMutants.Count();
            foreach (DiscoveredMutation mutant in availableMutants)
            {
                if (!TestMutant(mutant))
                {
                    survivedMutants++;
                }
            }

            Log.Information("Mutation testing complete. {survived} mutants survived out of {total} tested.", survivedMutants, testedMutantCount);
        }

    }

    private bool DoInitialTestWithNoActiveMutants()
    {
        Log.Information("Performing a preliminary test run on the mutated solution, with no active mutants to ensure all tests still pass.");

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"test {Path.GetFileName(_mutationSettings.SolutionPath)} --no-build",
            RedirectStandardError = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(_mutationSettings.SolutionPath),
        };
        IProcessWrapper testRun = _processFactory.Create(startInfo);

        bool processSuccess = StartProcess(testRun);

        foreach (string output in testRun.Output)
        {
            Log.Debug(output);
        }

        if (!processSuccess || !testRun.Success)
        {
            Log.Error("Introducing mutations caused tests to fail, cannot proceed with mutation testing.");
            //TODO, may be able to determine which mutants caused the failure and remove them from the pool.
            return false;
        }
        else
        {
            Log.Information("Preliminary test run successful, starting testing of individual mutants.");
            return true;
        }
    }

    private bool StartProcess(IProcessWrapper testRun)
    {
        //If we have initial test run info, use that to determine a timeout, otherwise fall back to the default timeout.
        bool processSuccess;
        if (_initialTestRunInfo is not null)
        {
            //use the origional test run plus 15%. TODO make this configurable
            processSuccess = testRun.StartAndAwait(_initialTestRunInfo.InitialRunDuration * 1.15);
        }
        else
        {
            double? defaultTimeout = _mutationSettings.SolutionProfileData?.GeneralSettings.TestRunTimeout;
            processSuccess = testRun.StartAndAwait(defaultTimeout);
        }

        return processSuccess;
    }

    private bool TestMutant(DiscoveredMutation mutant)
    {
        Log.Information("Testing mutant {mutant} in {file}.", mutant.MutatedNode.ToString(), mutant.OriginalNode.SyntaxTree.FilePath);

        mutant.Status = MutantStatus.TestOngoing;

        //TODO add filter for covered test cases
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"test {Path.GetFileName(_mutationSettings.SolutionPath)} --no-build",
            RedirectStandardError = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(_mutationSettings.SolutionPath),
            EnvironmentVariables =
            {
                [BaseMutationImplementation.ActiveMutationIndex] = mutant.ID.Data
            }
        };
        IProcessWrapper testRun = _processFactory.Create(startInfo);

        //TODO per test basis once coverage based testing is introduced
        bool processSuccess = StartProcess(testRun);

        foreach (string output in testRun.Output)
        {
            Log.Information(output);
        }

        //TODO improve reporting so we get more than just a count of killed/survived
        if (!processSuccess)
        {
            mutant.Status = MutantStatus.Killed;
            Log.Information("Mutation killed by introducing infinite test run.");
            return true;
        }
        if (!testRun.Success)
        {
            mutant.Status = MutantStatus.Killed;
            //TODO - should be able to say which tests failed.
            Log.Information("Mutation killed by failing test.");
            return true;
        }
        else
        {
            mutant.Status = MutantStatus.Survived;
            Log.Warning("Mutant survived.");
            return false;
        }
    }
}