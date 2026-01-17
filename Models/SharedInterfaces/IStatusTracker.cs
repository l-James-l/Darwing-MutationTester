using Models.Enums;

namespace Models.SharedInterfaces;

public interface IStatusTracker
{
    public bool TryStartOperation(DarwingOperation operation);

    public void FinishOperation(DarwingOperation operation, bool success);

    public OperationStates CheckStatus(DarwingOperation operation);
}
