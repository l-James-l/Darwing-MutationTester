namespace Models;

public class MutationSettings : IMutationSettings
{
    /// <inheritdoc/>
    public SolutionProfileData? SolutionProfileData { get; set; }

    /// <inheritdoc/>
    public bool DevMode { get; set; } = false;

    /// <inheritdoc/>
    public string SolutionPath { get; set; } = "";

    /// <inheritdoc/>
    public List<string> TestProjectNames { get; set; } = [];
}
