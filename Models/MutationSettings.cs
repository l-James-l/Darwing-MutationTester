namespace Models;

public class MutationSettings : IMutationSettings
{
    /// <inheritdoc/>
    public SolutionProfileData? SolutionProfileData { get; set; }

    /// <inheritdoc/>
    public string SolutionPath { get; set; } = "";

    /// <inheritdoc/>
    public List<string> TestProjectNames { get; set; } = [];
}
