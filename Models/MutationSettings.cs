using Models.Enums;

namespace Models;

public class MutationSettings : IMutationSettings
{
    /// <inheritdoc/>
    private SolutionProfileData? _solutionProfileData;

    /// <inheritdoc/>
    public string SolutionPath { get; set; } = "";

    /// <inheritdoc/>
    public List<string> TestProjects { get; set; } = [];
    
    /// <inheritdoc/>
    public List<string> IgnoreProjects { get; set; } = [];

    /// <inheritdoc/>
    public List<string> SourceCodeProjects { get; set; } = [];

    /// <inheritdoc/>
    public bool SingleMutantPerLine { get; set; } = true;

    /// <inheritdoc/>
    public int TestRunTimeout { get; set; } = 1200;

    /// <inheritdoc/>
    public int BuildTimeout { get; set; } = 30;

    /// <inheritdoc/>
    public bool SkipTestingNoActiveMutants { get; set; } = false;

    /// <inheritdoc/>
    public List<SpecifcMutation> DisabledMutationTypes { get; set; } = [];

    /// <inheritdoc/>
    public void UpdateProfile(SolutionProfileData? solutionProfileData)
    {
        _solutionProfileData = solutionProfileData;
    }
}

