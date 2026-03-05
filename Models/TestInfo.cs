namespace Models;

/// <summary>
/// Container class for info about an individual test case in the solution under test.
/// </summary>
public class TestInfo
{
    /// <summary>
    /// The test project that contains the test
    /// </summary>
    public IProjectContainer TestProject { get; }

    /// <summary>
    /// The path to the test relative to the sln file
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// How long did the test take to run
    /// </summary>
    public TimeSpan Duration { get; }

    public TestInfo(IProjectContainer testProject, string relativePath, TimeSpan duration)
    {
        TestProject = testProject;
        RelativePath = relativePath;
        Duration = duration;
    }
}