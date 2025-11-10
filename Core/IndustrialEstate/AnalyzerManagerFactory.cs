using Buildalyzer;

namespace Core.IndustrialEstate;

public class AnalyzerManagerFactory : IAnalyzerManagerFactory
{
    /// <summary>
    /// This factory method creates an instance of AnalyzerManager for the provided solution path.
    /// Note that the solution path is not validated here, so should be validated before calling this method.
    /// </summary>
    /// <param name="slnPath">Local path the sln file.</param>
    /// <returns>new instance of the AnalyzerManager</returns>
    public IAnalyzerManager CreateAnalyzerManager(string slnPath)
    {
        ArgumentNullException.ThrowIfNull(slnPath);

        return new AnalyzerManager(slnPath);
    }
}
