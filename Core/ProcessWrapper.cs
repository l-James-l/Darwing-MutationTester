
using System.Diagnostics;

namespace Core;

/// <summary>
/// Mockable container for a process
/// </summary>
public class ProcessWrapper : Process, IProcessWrapper
{
    public ProcessWrapper(ProcessStartInfo startInfo) : base()
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        StartInfo = startInfo;
        if (startInfo.RedirectStandardOutput)
        {
            OutputDataReceived += (x, y) =>
            {
                if (y.Data is not null) 
                { 
                    Output.Add(y.Data); 
                } 
            };
        }

        if (startInfo.RedirectStandardError)
        {
            ErrorDataReceived += (x, y) =>
            {
                if (y.Data is not null)
                {
                    Errors.Add(y.Data);
                }
            };
        }
    }

    public bool Success => ExitCode == 0;

    public List<string> Output { get; } = new List<string>();

    public List<string> Errors { get; } = new List<string>();

    public bool StartAndAwait(TimeSpan timeout)
    {
        Start();
        return WaitForExit(timeout);
    }
}

public interface IProcessWrapper
{
    public bool StartAndAwait(TimeSpan timeout);

    bool Success { get; }

    List<string> Output { get; }

    List<string> Errors { get; }
}