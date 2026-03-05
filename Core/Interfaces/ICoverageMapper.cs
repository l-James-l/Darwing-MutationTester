namespace Core.Interfaces;

/// <summary>
/// Interface to the <see cref="CoverageMapper"/>
/// </summary>
public interface ICoverageMapper
{
    /// <summary>
    /// Maps the xml file located at <paramref name="xmlPath"/>
    /// </summary>
    /// <param name="xmlPath">The fully qualified path to the altcover report</param>
    /// <returns>Was the translation successful</returns>
    bool MapFullCoverage(string xmlPath);
}