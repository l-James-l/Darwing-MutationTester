using Models.Enums;

namespace Models.SharedInterfaces;

/// <summary>
/// Contains methods to track the status of various operations.
/// Used to check when operations can start and to update/check their status.
/// </summary>
public interface IStatusTracker
{
    /// <summary>
    /// Will return true if the operation can start, false otherwise.
    /// </summary>
    public bool TryStartOperation(DarwingOperation operation);

    /// <summary>
    /// When an operation is finished, this method is called to update its status.
    /// </summary>
    public void FinishOperation(DarwingOperation operation, bool success);

    /// <summary>
    /// Will return the current status of the operation.
    /// </summary>
    public OperationStates CheckStatus(DarwingOperation operation);
}
