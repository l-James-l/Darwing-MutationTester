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


    public DiscoveredMutation(SyntaxAnnotation id, SyntaxNode original, SyntaxNode mutated, IEventAggregator eventAggregator)
    {
        ID = id;
        OriginalNode = original;
        _mutatedNode = mutated;
        _status = MutantStatus.Discovered; // Default for new mutations
        _lineSpan = null; // On creation, we dont know
        _document = null; // On creation, we dont know

        _eventAggregator = eventAggregator;
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
    /// What the original node was mutated into
    /// </summary>
    public SyntaxNode MutatedNode 
    { 
        get => _mutatedNode; 
        set  
        {
            _mutatedNode = value;
            _eventAggregator.GetEvent<MutationUpdated>().Publish(ID);
        }
    }
    private SyntaxNode _mutatedNode;

    /// <summary>
    /// The ID of the document the mutation occurred in.
    /// </summary>
    public DocumentId Document 
    { 
        get => _document ?? throw new PropertyNotAssignedException("Attempted to access mutation document before it assigned");
        set
        {
            _document = value;
            _eventAggregator.GetEvent<MutationUpdated>().Publish(ID);
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
            _eventAggregator.GetEvent<MutationUpdated>().Publish(ID);
        }
    }
    private FileLinePositionSpan? _lineSpan;

    /// <summary>
    /// Represents the current state of the mutation
    /// </summary>
    public MutantStatus Status 
    { 
        get => _status; 
        set
        {
            _status = value;
            _eventAggregator.GetEvent<MutationUpdated>().Publish(ID);
        }
    }
    private MutantStatus _status;
}
