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
    public List<SpecifcMutation> DisabledMutationTypes { get; set; }


    /// <summary>
    /// Sets the profile which was loaded.
    /// Once loaded and settings assigned, the profile will then only be used/changed if the user want to save the profile with the current settings,
    /// which will then update the profile with the current settings and save it to a file.
    /// </summary>
    public void UpdateProfile(SolutionProfileData? solutionProfileData);
}
