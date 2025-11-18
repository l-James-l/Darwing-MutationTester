using Microsoft.CodeAnalysis;

namespace Mutator;

/// <summary>
/// Struct to contain data around a single discovered mutation
/// </summary>
internal struct DiscoveredMutation
{    
    /// <summary>
    /// Origional umutatetd node
    /// </summary>
    public SyntaxNode OriginalNode { get; set; }

    /// <summary>
    /// What the origional node was mutated into
    /// </summary>
    public SyntaxNode MutatedNode { get; set; }

    /// <summary>
    /// The ID of the document the mutation occured in.
    /// </summary>
    public DocumentId Document {  get; set; }
}
