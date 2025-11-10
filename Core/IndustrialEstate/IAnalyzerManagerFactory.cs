using Buildalyzer;

namespace Core.IndustrialEstate;

public interface IAnalyzerManagerFactory
{
    public IAnalyzerManager CreateAnalyzerManager(string slnPath);
}