namespace Core.IndustrialEstate;

public class CancelationTokenFactory : ICancelationTokenFactory, IDisposable
{
    private List<CancellationTokenWrapper> _allTokens = new();

    public void Dispose()
    {
        _allTokens.ForEach(t => t.Dispose());
        GC.SuppressFinalize(this);
    }

    public ICancellationTokenWrapper Generate()
    {

        CancellationTokenWrapper cancellationTokenWrapper = new();
        _allTokens.Add(cancellationTokenWrapper);
        return cancellationTokenWrapper;
    }
}

public interface ICancelationTokenFactory
{
    /// <summary>
    /// DI factory for cancelation tokens.
    /// </summary>
    /// <returns></returns>
    ICancellationTokenWrapper Generate();
}

/// <summary>
/// By extending the CancelationTokenSource, and wrapping the IsCancellationRequested property, 
/// we can intercept when its checked
/// </summary>
public interface ICancellationTokenWrapper
{
    bool IsCancelled();

    void Cancel();
}

public class CancellationTokenWrapper : CancellationTokenSource,  ICancellationTokenWrapper
{
    public bool IsCancelled() => IsCancellationRequested;

    public new void Cancel() => base.Cancel();
}
