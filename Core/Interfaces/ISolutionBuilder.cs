namespace Core.Interfaces;

public interface ISolutionBuilder
{
    /// <summary>
    /// Will build the unmutated solution given that one is available in the solution provider.
    /// </summary>
    public void InitialBuild();
}
