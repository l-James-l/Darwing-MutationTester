using Models;

namespace Core.Startup;

public class StartUpProcessManager
{
    private readonly IEnumerable<IStartUpProcess> _startUpProcesses;

    public StartUpProcessManager(IEnumerable<IStartUpProcess> startUpProcesses)
    {
        _startUpProcesses = startUpProcesses;
    }

    public void Initialize()
    {
        foreach (IStartUpProcess process in _startUpProcesses)
        {
            process.StartUp();
        }
    }
}