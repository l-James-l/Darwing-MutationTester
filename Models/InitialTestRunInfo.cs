namespace Models;

public class InitialTestRunInfo
{
    /// <summary>
    /// Was the initial, unmutated test run successful.
    /// </summary>
    public bool WasSuccesful { get; set; } = false;

    /// <summary>
    /// The time taken for the initial unmutated test run.
    /// </summary>
    public TimeSpan InitialRunDuration { get; set; }

    // TODO: Will contain coverage info aswell once implemeted so mutations can only run relevant tests.
}
