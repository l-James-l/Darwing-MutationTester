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
    public List<string> TestProjects { get; set; } = new List<string>();

    /// <summary>
    /// If only specific projects should be mutated, their names can be provided here.
    /// </summary>
    public List<string>? ProjectsToMutate { get; set; }

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
    [DefaultValue(true)]
    public bool SingleMutantPerLine { get; set; } = true;

    /// <summary>
    /// Allows setting of custom timeouts for when a build process is considered failed.
    /// It is possible for builds to get stuck, so after some time we need to assume its failed.
    /// But some projects may just need longer to build.
    /// Value is in seconds.
    /// </summary>
    public int? BuildTimeout { get; set; }
}
