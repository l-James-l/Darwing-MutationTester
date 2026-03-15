using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Mutator;
using Serilog;
using System.Diagnostics;

namespace Core;

public class InitialTestRunner : IMutationRunInitiator
{
    private readonly IMutationSettings _mutationSettings;
    private readonly IStatusTracker _statusTracker;
    private readonly IProcessWrapperFactory _processFactory;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;
    private readonly ISolutionProvider _solutionProvider;
    private readonly ICoverageMapper _coverageMapper;
    private const string _originalBinariesSaveFolder = "DarwingOriginalSavedBinaries";
    private const string _coverageReportName = "DarwingCoverage.xml";

    public InitialTestRunner(IMutationSettings mutationSettings, IStatusTracker statusTracker,
        IProcessWrapperFactory processFactory, IMutationDiscoveryManager mutationDiscoveryManager, ISolutionProvider solutionProvider,
        ICoverageMapper coverageMapper)
    {
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(processFactory);
        ArgumentNullException.ThrowIfNull(mutationDiscoveryManager);
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(coverageMapper);
        
        _mutationSettings = mutationSettings;
        _statusTracker = statusTracker;
        _processFactory = processFactory;
        _mutationDiscoveryManager = mutationDiscoveryManager;
        _solutionProvider = solutionProvider;
        _coverageMapper = coverageMapper;
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

        try
        {
            bool allTestsPassed = TestSolution();
            _statusTracker.FinishOperation(DarwingOperation.TestUnmutatedSolution, allTestsPassed);
            if (allTestsPassed)
            {
                _mutationDiscoveryManager.PerformMutationDiscovery();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "exception occurred while running initial tests.");
            _statusTracker.FinishOperation(DarwingOperation.TestUnmutatedSolution, false);
        }
    }

    private bool TestSolution()
    {
        //Install the global tool. If already installed, this will just return true.
        IProcessWrapper installProcess = _processFactory.Create(new ProcessStartInfo() 
        {
            FileName = "dotnet", 
            Arguments = "tool install -g altcover.global" 
        });

        bool installComplete = installProcess.StartAndAwait(TimeSpan.FromSeconds(60));
        if (!installComplete || !installProcess.Success)
        {
            installProcess.Output.ForEach(Log.Debug);
            installProcess.Errors.ForEach(Log.Error);
            Log.Error("Unable to install altcover. Testing cannot be done using coverage.");
            return false;
        }

        bool allTestsPassed = true;
        if (installComplete)
        {
            _solutionProvider.SolutionContainer.TestProjects.ForEach(testProject => allTestsPassed &= TestProject(testProject));
        }
        return allTestsPassed;
    }

    private bool TestProject(IProjectContainer testProject)
    {
        Log.Information($"Doing Initial Tests for {testProject.Name}.");
        string reportPath = Path.Combine(testProject.OutputDirectory, _coverageReportName);
        EnsureNothingLeftOver(testProject,reportPath);

        string altCoverArgs =
            $"--inplace --save " + // override the binaries in place, while saving the originals elsewhere.
            $"--linecover --all " + // Track line-to-test coverage, and track all hits rather than one and done
            $"-c \"[Test]\" -c \"[Fact]\" -c \"[Theory]\" -c \"[TestMethod]\" " + //Attributes that define a test so we know what test ran each line
            $"--inputDirectory \"{testProject.OutputDirectory}\" " + // The bin folder of the test project
            $"--outputDirectory \"{_originalBinariesSaveFolder}\" " + // The folder to save the original binaries in
            $"--report \"DarwingCoverage.xml\" " + // Where to save the report
            $"-- " + // Embeds the test command
            $"dotnet test \"{testProject.CsprojFilePath}\" --no-build --no-restore -- --stop-on-failure";

        IProcessWrapper testingProcess = _processFactory.Create(new ProcessStartInfo
        {
            FileName = "altcover",
            Arguments = altCoverArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = testProject.OutputDirectory
        });

        bool completed = testingProcess.StartAndAwait(TimeSpan.FromSeconds(_mutationSettings.TestRunTimeout));

        testingProcess.Output.ForEach(Log.Debug);
        testingProcess.Errors.ForEach(Log.Error);

        if (!completed || !testingProcess.Success)
        {
            Log.Error($"Testing {testProject.Name} failed. Cannot perform mutation testing.");
            return false;
        }

        Log.Information("Testing complete, trying to compile coverage");

        bool success = ExtractCoverageInfo(testProject, reportPath);

        if (!Restore(testProject))
        {
            Log.Warning("Could not restore original binaries from coverage, they will still be in place during mutations. Shouldn't affect results.");
        }
        return success;
    }

    public bool Restore(IProjectContainer testProject)
    {
        try
        {
            string backupDirectory = Path.Combine(testProject.OutputDirectory, _originalBinariesSaveFolder);

            if (!Directory.Exists(backupDirectory))
            {
                // If the backup doesn't exist, we can't restore. 
                return false;
            }

            // Get all items in the backup, including nested items
            foreach (string backupItem in Directory.GetFileSystemEntries(backupDirectory, "*", SearchOption.AllDirectories))
            {
                // Create the target path by replacing the backup root with the bin root
                string relativePath = Path.GetRelativePath(backupDirectory, backupItem);
                string targetPath = Path.Combine(testProject.OutputDirectory, relativePath);

                if (Directory.Exists(backupItem))
                {
                    // Ensure the sub-directory exists in the destination, does nothing if it already exists.
                    Directory.CreateDirectory(targetPath);
                }
                else
                {
                    // It's a file, so copy it back across.
                    File.Copy(backupItem, targetPath, true);
                }
            }

            // Remove the AltCover-specific recorder DLL if it's in the root
            // AltCover often drops this next to your DLLs during instrumentation
            string recorderDll = Path.Combine(testProject.OutputDirectory, "AltCover.Recorder.g.dll");
            if (File.Exists(recorderDll))
            {
                File.Delete(recorderDll);
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while trying to restore original binaries from coverage.");
            return false;
        }
        
    }

    private void EnsureNothingLeftOver(IProjectContainer testProject, string reportPath)
    {
        // In case the teardown wasn't completed properly from a previous run, delete any leftover artifacts to prevent errors.
        string saveDir = Path.Combine(testProject.OutputDirectory, _originalBinariesSaveFolder);
        if (Directory.Exists(saveDir))
        {
            Log.Warning($"Had to delete a left over save directory: {saveDir}.");
            Directory.Delete(saveDir, true);
        }
        if (File.Exists(reportPath))
        {
            Log.Warning($"Had to delete a left over report file: {reportPath}.");
            File.Delete(reportPath);
        }
    }

    private bool ExtractCoverageInfo(IProjectContainer testProject, string reportPath)
    {
        IProcessWrapper collectProcess = _processFactory.Create(new ProcessStartInfo
        {
            FileName = "altcover",
            Arguments = $"runner --collect --recorderDirectory \"{testProject.OutputDirectory}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = testProject.OutputDirectory
        });
        bool collectionComplete = collectProcess.StartAndAwait(TimeSpan.FromSeconds(10));

        collectProcess.Output.ForEach(Log.Debug);
        collectProcess.Errors.ForEach(Log.Error);

        if (!collectionComplete || !collectProcess.Success)
        {
            Log.Error($"Collect process failed for {testProject.Name}.");
            return false;
        }
        
        if (_coverageMapper.MapFullCoverage(reportPath))
        {
            Log.Information($"Converge successfully collated for {testProject.Name}.");
            return true;
        }
        return false;
    }
}
