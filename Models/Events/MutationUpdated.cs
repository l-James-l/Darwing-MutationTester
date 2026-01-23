using Microsoft.CodeAnalysis;

namespace Models.Events;

/// <summary>
/// Event to be published by the mutation model when its created, and when any of its properties are updated.
/// </summary>
public class MutationUpdated : PubSubEvent<SyntaxAnnotation>
{

}