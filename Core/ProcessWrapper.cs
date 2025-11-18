
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

    public TimeSpan Duration { get; private set; } = TimeSpan.MaxValue;

    public bool StartAndAwait(int? timeout)
    {
        if (timeout.HasValue)
        {
            return StartAndAwait(TimeSpan.FromSeconds(timeout.Value));
        }

        // Caller didnt specify a timeout, so use a reasonably high one
        return StartAndAwait(TimeSpan.FromHours(1));
    }

    public bool StartAndAwait(TimeSpan timeout)
    {
        Start();

        bool completed = WaitForExit(timeout);

        if (completed)
        {
            Duration = ExitTime - StartTime;
        }
        return completed;
    }
}

public interface IProcessWrapper
{
    public bool StartAndAwait(TimeSpan timeout);

    public bool StartAndAwait(int? timeout);

    bool Success { get; }

    List<string> Output { get; }

    List<string> Errors { get; }

    TimeSpan Duration { get; }
}