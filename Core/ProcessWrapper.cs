
using Core.Interfaces;
using System.Diagnostics;

namespace Core;

/// <summary>
/// Mockable container for a process
/// </summary>
public class ProcessWrapper : Process, IProcessWrapper
{
    private bool _processCompleted = false;

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

    public bool Success => _processCompleted && ExitCode == 0;

    public List<string> Output { get; } = new List<string>();

    public List<string> Errors { get; } = new List<string>();

    public TimeSpan Duration { get; private set; } = TimeSpan.MaxValue;

    public bool StartAndAwait(double? timeout)
    {
        if (timeout.HasValue)
        {
            return StartAndAwait(TimeSpan.FromSeconds(timeout.Value));
        }

        // Caller didn't specify a timeout, so use a reasonably high one
        return StartAndAwait(TimeSpan.FromHours(1));
    }

    public bool StartAndAwait(TimeSpan timeout)
    {
        return StartAndAwaitAsync(timeout)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// We use an async method here to be able to use WaitForExitAsync with a cancellation token, 
    /// and allow for better timeout handling.
    /// Note: still not flawless. Still getting hanging testhosts - see CR
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    private async Task<bool> StartAndAwaitAsync(TimeSpan timeout)
    {
        using CancellationTokenSource cts = new (timeout);

        Stopwatch stopwatch = Stopwatch.StartNew();

        Start();

        // Begin reading output and error streams if redirected.
        // Otherwise we can fill up the buffers and cause a deadlock.
        if (StartInfo.RedirectStandardOutput)
        {
            BeginOutputReadLine();
        }
        if (StartInfo.RedirectStandardError)
        {
            BeginErrorReadLine();
        }

        try
        {
            await WaitForExitAsync(cts.Token);
            _processCompleted = true;
        }
        catch (OperationCanceledException)
        {
            
        }

        // Try and kill the process if it is still running after timeout
        if (!HasExited)
        {
            Kill(entireProcessTree: true);
        }

        stopwatch.Stop();
        Duration = stopwatch.Elapsed;
        
        return ExitCode == 0;
    }

}
