using Models.Enums;

namespace Models.Events;

/// <summary>
/// Published when the StatusTracker reports that the states of Darwing operations have changed.
/// </summary>
public class DarwingOperationStatesChangedEvent : PubSubEvent<DarwingOperation>
{
}