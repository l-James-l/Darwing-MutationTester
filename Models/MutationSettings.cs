namespace Models;

public class MutationSettings : IMutationSettings
{
    /// <inheritdoc/>
    public SolutionProfileData? SolutionProfileData { get; set; }

    /// <inheritdoc/>
    public string SolutionPath { get; set; } = "";

    /// <inheritdoc/>
    public List<string> TestProjects { get; set; } = [];
    
    /// <inheritdoc/>
    public List<string> IgnoreProjects { get; set; } = [];

    /// <inheritdoc/>
    public List<string> SourceCodeProjects { get; set; } = [];

    /// <inheritdoc/>
    public bool SkipTestingNoActiveMutants { get; set; } = false;
}
