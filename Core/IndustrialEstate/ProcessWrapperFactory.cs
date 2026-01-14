using Core.Interfaces;
using System.Diagnostics;

namespace Core.IndustrialEstate;

/// <summary>
/// By using a factory pattern to create process wrapped in an interface, we can mock the process results in unit testing.
/// </summary>
public class ProcessWrapperFactory: IProcessWrapperFactory
{
    public IProcessWrapper Create(ProcessStartInfo processStartInfo)
    {
        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = false;

        return new ProcessWrapper(processStartInfo);
    }
}

public interface IProcessWrapperFactory
{
    IProcessWrapper Create(ProcessStartInfo processStartInfo);
}
