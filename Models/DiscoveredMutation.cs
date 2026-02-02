using Microsoft.CodeAnalysis;
using Models.Enums;
using Models.Events;
using Models.Exceptions;

namespace Models;

/// <summary>
/// Struct to contain data around a single discovered mutation
/// </summary>
public class DiscoveredMutation
{
    /// <summary>
    /// We give the mutant model the event aggregator so that it can notify subscribers itself when it properties have changes,
    /// rather than trusting that every class that alters/can alter a mutation, will then publish the notification.
    /// </summary>
    private readonly IEventAggregator _eventAggregator;

    public DiscoveredMutation(SyntaxAnnotation id, SyntaxNode original, SyntaxNode mutationSwitcher, SyntaxNode mutatedNode,
        IEventAggregator eventAggregator, MutationCategory category, SpecifcMutation type)
    {
        ID = id;
        OriginalNode = original;
        MutationSwitcher = mutationSwitcher;
        MutatedNode = mutatedNode;
        Category = category;
        SpecificType = type;

        Status = MutantStatus.Discovered; // Default for new mutations
        _lineSpan = null; // On creation, we dont know
        _document = null; // On creation, we dont know

        _eventAggregator = eventAggregator;
    }

    private void NotifyMutationUpdated()
    {
        if (_document is null || _lineSpan is null)
        {
            // We dont notify until we have enough information to be useful
            return;
        }
        _eventAggregator.GetEvent<MutationUpdated>().Publish(ID);
    }

    /// <summary>
    /// The mutation identifier 
    /// Note: we dont notify when this is updated because after creation this should never change
    /// </summary>
    public SyntaxAnnotation ID { get; set; }

    /// <summary>
    /// Original unmutated node
    /// Note: we dont notify when this is updated because after creation this should never change
    /// </summary>
    public SyntaxNode OriginalNode { get; set; }

    /// <summary>
    /// The actual node that will be embedded in the syntax tree, that contains the original node, and the mutated node,
    /// inside a ternary statement
    /// </summary>
    public SyntaxNode MutationSwitcher {get; set; }

    /// <summary>
    /// What the original node was mutated into
    /// </summary>
    public SyntaxNode MutatedNode { get; set; }

    /// <summary>
    /// Gets the category of the mutation represented by this instance.
    /// </summary>
    public MutationCategory Category { get; }

    /// <summary>
    /// Gets the specific mutation type associated with this instance.
    /// </summary>
    public SpecifcMutation SpecificType { get; }

    /// <summary>
    /// The ID of the document the mutation occurred in.
    /// </summary>
    public DocumentId Document 
    { 
        get => _document ?? throw new PropertyNotAssignedException("Attempted to access mutation document before it assigned");
        set
        {
            _document = value;
            NotifyMutationUpdated();
        }
    }
    private DocumentId? _document;

    /// <summary>
    /// The line and position on the line that the mutation occurs
    /// </summary>
    public FileLinePositionSpan LineSpan
    {
        get => _lineSpan ?? throw new PropertyNotAssignedException("Attempted to access mutation line span before it assigned");
        set
        {
            _lineSpan = value;
            NotifyMutationUpdated();
        }
    }
    private FileLinePositionSpan? _lineSpan;

    /// <summary>
    /// Represents the current state of the mutation
    /// </summary>
    public MutantStatus Status 
    { 
        get; 
        set
        {
            field = value;
            NotifyMutationUpdated();
        }
    }
}
