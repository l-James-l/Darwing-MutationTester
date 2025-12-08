namespace Models.Events;

/// <summary>
/// Event to publish once all mutations have been applied and to a solution and now we need to build the new dlls
/// </summary>
public class BuildMutatedSolutionEvent : PubSubEvent
{
    
}
