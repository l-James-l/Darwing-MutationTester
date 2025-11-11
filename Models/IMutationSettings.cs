namespace Models;

public interface IMutationSettings
{
    /// <summary>
    /// Profile supplied with solution.
    /// </summary>
    public SolutionProfileData? SolutionProfileData { get; set; }
    
    /// <summary>
    /// Is the application being run in develop mode.
    /// </summary>
    public bool DevMode { get; set; }

    /// <summary>
    /// Path to sln file being mutated.
    /// </summary>
    public string SolutionPath { get; set; }

    /// <summary>
    /// User can specify which projects are test projects.
    /// </summary>
    public List<string> TestProjectNames { get; set; }
}
