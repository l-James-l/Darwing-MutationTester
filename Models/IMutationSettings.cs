using Models.Enums;

namespace Models;

public interface IMutationSettings
{
    /// <summary>
    /// Path to sln file being mutated.
    /// </summary>
    public string SolutionPath { get; set; }

    /// <summary>
    /// User can specify which projects are test projects.
    /// </summary>
    public List<string> TestProjects { get; set; }
    
    /// <summary>
    /// The names of source code projects. This is the default state for a loaded project so should only be specified if a project
    /// is wrongly being determined to be a test project, but then this is likely a symptom of another issue in the project.
    /// </summary>
    public List<string> SourceCodeProjects { get; set; }

    /// <summary>
    /// The names of any source code projects that should be ignored and not mutated.
    /// </summary>
    public List<string> IgnoreProjects { get; set; }

    /// <summary>
    /// Should mutation be restricted to a single one per line
    /// </summary>
    public bool SingleMutantPerLine { get; set; }

    /// <summary>
    /// time out for a test run process
    /// </summary>
    public int TestRunTimeout { get; set; }

    /// <summary>
    /// Time out for a build process
    /// </summary>
    public int BuildTimeout { get; set; }

    /// <summary>
    /// After mutations have been applied to the solution, initial test run with no active mutants is performed
    /// to ensure that introducing mutations did not break the build or tests.
    /// This setting allows skipping that test run.
    /// Warning: Skipping this step may lead to misleading results if the mutated solution is not stable.
    /// </summary>
    public bool SkipTestingNoActiveMutants { get; set; }

    /// <summary>
    /// List of any disabled mutation types.
    /// </summary>
    public List<SpecificMutation> DisabledMutationTypes { get; set; }

    /// <summary>
    /// If true, will use the more advanced project type analysis which uses build analysis to determine the project type.
    /// This is more accurate, but also significantly increases the time taken to load the solution, so is not on by default.
    /// </summary>
    public bool UseAdvancedProjectTypeAnalysis { get; set; }

    /// <summary>
    /// The branch name of the default branch to use for comparisons while establishing the diff to test
    /// </summary>
    public string DefaultGitComparisonBranch { get; set; }

    /// <summary>
    /// The scaler to apply to the timeout for test runs when we have information from an initial test run with no active mutants.
    /// e.g., if the initial test run took 10 minutes, and the scaler is 1.5, then the timeout for test runs will be 15 minutes.
    /// </summary>
    double MutationTestTimeoutScaler { get; set; }

}
