using Models.Enums;
using System.ComponentModel;

namespace Models;

/// <summary>
/// Data class for solution profile information.
/// Data read from yml file in solution root folder.
/// </summary>
public class SolutionProfileData
{
    /// <summary>
    /// List of names of the test projects in the solution.
    /// At present, this is treated as a comprehensive list of all test projects in the solution.
    /// </summary>
    public List<string> TestProjects { get; set; } = [];

    /// <summary>
    /// The names of any source code projects that should be ignored and not mutated.
    /// </summary>
    public List<string> IgnoreProjects { get; set; } = [];

    /// <summary>
    /// The names of source code projects. This is the default state for a loaded project so should only be specified if a project
    /// is wrongly being determined to be a test project, but then this is likely a symptom of another issue in the project.
    /// </summary>
    public List<string> SourceCodeProjects { get; set; } = [];

    /// <summary>
    /// Allows enabling or disabling specific mutation types.
    /// </summary>
    public Dictionary<SpecifcMutation, bool> SpecificMutations { get; set; } = new Dictionary<SpecifcMutation, bool>();

    /// <summary>
    /// Allows enabling or disabling entire mutation categories.
    /// Note that if a specific mutation type is enabled/disabled, that setting takes precedence over the category setting.
    /// </summary>
    public Dictionary<MutationCategory, bool> MutationCategories { get; set; } = new Dictionary<MutationCategory, bool>();

    /// <summary>
    /// Class containing more general settings.
    /// </summary>
    public SolutionProfileGeneralSettings GeneralSettings { get; set; } = new SolutionProfileGeneralSettings();
}

public class SolutionProfileGeneralSettings
{
    /// <summary>
    /// TODO: make this do something.
    /// </summary>
    [DefaultValue(true)]
    public bool SingleMutantPerLine { get; set; } = true;

    /// <summary>
    /// Allows setting of custom timeouts for when a build process is considered failed.
    /// It is possible for builds to get stuck, so after some time we need to assume its failed.
    /// But some projects may just need longer to build.
    /// Value is in seconds.
    /// </summary>
    public int? BuildTimeout { get; set; }

    /// <summary>
    /// Allows setting of a custom timeout for when a test run will be considered failed.
    /// A generic timeout cannot be because it is impossoble to know how long a test run might take before it has been completed.
    /// </summary>
    public int? TestRunTimeout { get; set; }

    /// <summary>
    /// Will skip the stage after mutant discovery of running all tests with no mutant activated. Can save time but not recommended.
    /// </summary>
    public bool SkipTestingNoActiveMutants = false;
}
