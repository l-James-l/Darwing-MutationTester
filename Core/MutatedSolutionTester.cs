using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Mutator;
using Mutator.MutationImplementations;
using Serilog;
using System.Diagnostics;

namespace Core;

public class MutatedSolutionTester : IMutatedSolutionTester
{
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;
    private readonly IProcessWrapperFactory _processFactory;
    private readonly IMutationSettings _mutationSettings;
    private readonly IStatusTracker _statusTracker;
    private readonly ISolutionProvider _solutionProvider;

    public MutatedSolutionTester(IMutationDiscoveryManager mutationDiscoveryManager, IProcessWrapperFactory processFactory, 
        IMutationSettings mutationSettings, IStatusTracker statusTracker, ISolutionProvider solutionProvider)
    {
        ArgumentNullException.ThrowIfNull(mutationDiscoveryManager);
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(statusTracker);

        _mutationDiscoveryManager = mutationDiscoveryManager;
        _processFactory = processFactory;
        _mutationSettings = mutationSettings;
        _statusTracker = statusTracker;
        _solutionProvider = solutionProvider;
    }

    public void RunTestsOnMutatedSolution()
    {
        if (!_statusTracker.TryStartOperation(DarwingOperation.TestMutants))
        {
            return;
        }

        try
        {
            bool completed = TestAllMutants();
            _statusTracker.FinishOperation(DarwingOperation.TestMutants, completed);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while testing mutants.");
            _statusTracker.FinishOperation(DarwingOperation.TestMutants, false);
        }
    }

    private bool TestAllMutants()
    {
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
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool DoInitialTestWithNoActiveMutants()
    {
        if (_mutationSettings.SkipTestingNoActiveMutants)
        {
            Log.Warning("Skipping preliminary test run with no active mutants as per configuration.");
            return true;
        }

        Log.Information("Performing a preliminary test run on the mutated solution, with no active mutants to ensure all tests still pass.");

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"test {Path.GetFileName(_mutationSettings.SolutionPath)} --no-build --no-restore -- --stop-on-failure",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(_mutationSettings.SolutionPath),
        };
        IProcessWrapper testRun = _processFactory.Create(startInfo);

        bool processSuccess = testRun.StartAndAwait(_mutationSettings.TestRunTimeout);

        testRun.Output.ForEach(Log.Debug);
        testRun.Errors.ForEach(Log.Error);

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

    // TODO: It would be better to open a single test host and feed it tests, because starting the test host is now the most taxing part of testing each mutant
    // TODO: Look into running tests on multiple threads
    private bool TestMutant(DiscoveredMutation mutant)
    {
        Log.Information("Testing mutant {mutant} in {file}.", mutant.MutatedNode.ToString(), mutant.OriginalNode.SyntaxTree.FilePath);

        mutant.Status = MutantStatus.TestOngoing;

        List<TestInfo> testsToRun = GetTestsToRun(mutant);
        // Have to run the tests on a project by project basis otherwise the testhost gets confused and cant find tests.
        foreach (IGrouping<IProjectContainer, TestInfo> testInfos in testsToRun.GroupBy(x => x.TestProject))
        {
            IProcessWrapper testRun = CreateTestProcess(mutant, testInfos);
            // Add a generous 10 seconds to the timeout to allow the testhost to start. This should only take 2-4 seconds.
            TimeSpan totalRunTime = GetRunTime(testsToRun) + TimeSpan.FromSeconds(10);

            bool processSuccess = testRun.StartAndAwait(totalRunTime);

            testRun.Output.ForEach(Log.Debug);
            testRun.Errors.ForEach(Log.Error);

            if (!processSuccess)
            {
                mutant.Status = MutantStatus.KilledByTimeOut;
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

        // No tests ran
        mutant.Status = MutantStatus.NoCoverage;
        return false;
    }

    private TimeSpan GetRunTime(List<TestInfo> testsToRun)
    {
        // Get the total run time of all the tests we need to run + a scaler percentage + an additional second per test.
        return TimeSpan.FromSeconds(testsToRun.Select(x => x.Duration).Sum(x => x.TotalSeconds) * _mutationSettings.MutationTestTimeoutScaler + testsToRun.Count);
    }

    private IProcessWrapper CreateTestProcess(DiscoveredMutation mutant, IEnumerable<TestInfo> testsToRun)
    {
        // FullyQualifiedName~Test1|FullyQualifiedName=Test2.
        // using ~ so that testcases are run
        // TODO: if this gets really long it could break it, but should be exceedingly rare that many tests hit a single line
        IEnumerable<string> filterParts = testsToRun.Select(t => $"FullyQualifiedName~{t.RelativePath}");

        IProcessWrapper testProcess = _processFactory.Create(new()
        {
            FileName = "dotnet",
            Arguments = $"test --no-build --no-restore " +
                        $"--filter \"{string.Join("|", filterParts)}\" "+
                        $"-- --stop-on-failure  -- RunConfiguration.TestSessionTimeout={(int)GetRunTime([.. testsToRun]).TotalMilliseconds}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = testsToRun.First().TestProject.DirectoryPath,
            EnvironmentVariables =
            {
                [Annotations.ActiveMutationIndex] = mutant.ID.Data
            }
        });
        Log.Debug($"{testProcess}");
        return testProcess;
    }

    private List<TestInfo> GetTestsToRun(DiscoveredMutation mutant)
    {
        SourceCodeFileContainer? file = _solutionProvider.SolutionContainer.FindFile(mutant.Document);
        if (file == null)
        {
            Log.Error("Mutant file not found. Setting no coverage.");
            mutant.Status = MutantStatus.NoCoverage;
            return [];
        }
        if (!file.LineToTestMapping.TryGetValue(mutant.LineSpan.StartLinePosition.Line+1, out List<TestInfo>? testsToRun) ||
            testsToRun.Count == 0)
        {
            Log.Information("No coverage for mutant.");
            mutant.Status = MutantStatus.NoCoverage;
            return [];
        }
        return testsToRun;
    }
}