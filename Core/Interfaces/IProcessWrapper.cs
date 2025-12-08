namespace Core.Interfaces;

public interface IProcessWrapper
{
    public bool StartAndAwait(TimeSpan timeout);

    public bool StartAndAwait(double? timeout);

    bool Success { get; }

    List<string> Output { get; }

    List<string> Errors { get; }

    TimeSpan Duration { get; }
}