namespace Models;

public class MutationSettings : IMutationSettings
{
    /// <inheritdoc/>
    public bool DevMode { get; set; }

    /// <inheritdoc/>
    public string SolutionPath { get; set; } = "";

    /// <inheritdoc/>
    public List<string> TestProjectNames { get; set; } = [];
}
