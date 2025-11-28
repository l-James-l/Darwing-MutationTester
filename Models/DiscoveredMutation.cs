using Microsoft.CodeAnalysis;
using Models.Enums;
using Models.Exceptions;

namespace Models;

/// <summary>
/// Struct to contain data around a single discovered mutation
/// </summary>
public class DiscoveredMutation
{
    public DiscoveredMutation(SyntaxAnnotation id, SyntaxNode origional, SyntaxNode mutated)
    {
        ID = id;
        OriginalNode = origional;
        MutatedNode = mutated;
        Status = MutantStatus.Discovered; // Default for new mutations
        _lineSpan = null; // On creation, we dont know
        _document = null; // On creation, we dont know
    }

    /// <summary>
    /// The mutation identifier 
    /// </summary>
    public SyntaxAnnotation ID { get; set; }

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
    public DocumentId Document 
    { 
        get => _document ?? throw new PropertyNotAssignedException("Attempted to access mutation document before it assigned");
        set => _document = value; 
    }
    private DocumentId? _document;

    /// <summary>
    /// The line and position on the line that the mutation occurs
    /// </summary>
    public FileLinePositionSpan LineSpan 
    { 
        get => _lineSpan ?? throw new PropertyNotAssignedException("Attempted to access mutation line span before it assigned"); 
        set => _lineSpan = value; 
    }
    private FileLinePositionSpan? _lineSpan;

    /// <summary>
    /// Represents the current state of the mutation
    /// </summary>
    public MutantStatus Status { get; set; }
}
