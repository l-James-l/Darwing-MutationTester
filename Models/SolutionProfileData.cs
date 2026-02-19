using Models.Enums;

namespace Models;

/// <summary>
/// Data class for solution profile information.
/// Data read from yml file in solution root folder.
/// 
/// IMPORTANT: When any new properties are added to this class, they should be made nullable and default to null.
/// The logic in SolutionProfileDeserializer should be updated to only assign values from the profile if they are not null. 
/// This allows for backwards compatibility with existing solution profiles when new properties are added.
/// 
/// Ensure any new properties are added to <see cref="SolutionProfileDeserializer.AssignSettingsFromProfile"/>
/// and <see cref="SettingsViewModel.BuildNewProfileObject"/>
/// </summary>
public class SolutionProfileData
{
    /// <summary>
    /// List of names of the test projects in the solution.
    /// At present, this is treated as a comprehensive list of all test projects in the solution.
    /// </summary>
    public List<string>? TestProjects { get; set; } = null;

    /// <summary>
    /// The names of any source code projects that should be ignored and not mutated.
    /// </summary>
    public List<string>? IgnoreProjects { get; set; } = null;

    /// <summary>
    /// The names of source code projects. This is the default state for a loaded project so should only be specified if a project
    /// is wrongly being determined to be a test project, but then this is likely a symptom of another issue in the project.
    /// </summary>
    public List<string>? SourceCodeProjects { get; set; } = null;

    /// <summary>
    /// Allows disabling specific mutation types.
    /// </summary>
    public List<SpecificMutation>? DisabledMutationTypes { get; set; } = null;

    /// <summary>
    /// Only test a single mutation per line
    /// </summary>
    public bool? SingleMutantPerLine { get; set; } = null;

    /// <summary>
    /// Allows setting of custom timeouts for when a build process is considered failed.
    /// It is possible for builds to get stuck, so after some time we need to assume its failed.
    /// But some projects may just need longer to build.
    /// Value is in seconds.
    /// Default value is 30 seconds, which should be more than enough for most projects, but can be changed if needed.
    /// </summary>
    public int? BuildTimeout { get; set; } = null;

    /// <summary>
    /// Allows setting of a custom timeout for when a test run will be considered failed.
    /// A generic timeout cannot be because it is impossible to know how long a test run might take before it has been completed.
    /// Value is in seconds.
    /// Default value an hour.
    /// </summary>
    public int? TestRunTimeout { get; set; } = null;

    /// <summary>
    /// Will skip the stage after mutant discovery of running all tests with no mutant activated. Can save time but not recommended.
    /// </summary>
    public bool? SkipTestingNoActiveMutants { get; set; } = null;

    /// <summary>
    /// If true, will use the more advanced project type analysis which uses build analysis to determine the project type.
    /// This is more accurate, but also significantly increases the time taken to load the solution, so is not on by default.
    /// </summary>
    public bool? UseAdvancedProjectTypeAnalysis { get; set; } = null;

    /// <summary>
    /// The branch name of the default branch to use for comparisons while establishing the diff to test
    /// </summary>
    public string? DefaultGitComparisonBranch { get; set; } = null;

    /// <summary>
    /// The scaler to apply to the timeout for test runs when we have information from an initial test run with no active mutants.
    /// e.g., if the initial test run took 10 minutes, and the scaler is 1.5, then the timeout for test runs will be 15 minutes.
    /// </summary>
    public double? MutationTestTimeoutScaler { get; set; } = null;
}

