namespace Models;

public class InitialTestRunInfo
{
    /// <summary>
    /// Was the initial, unmutated test run successful.
    /// </summary>
    public bool WasSuccesful { get; set; } = false;

    // TODO: Will contain coverage info aswell once implemeted so mutations can only run relevant tests.
}
