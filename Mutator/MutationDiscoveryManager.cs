using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Mutator.MutationImplementations;
using Serilog;

namespace Mutator;

/// <summary>
/// This class if responsible for something probably
/// </summary>
public class MutationDiscoveryManager : IMutationDiscoveryManager
{
    private ISolutionProvider _solutionProvider;
    private IMutationImplementationProvider _mutationImplementationProvider;
    private readonly IStatusTracker _statusTracker;
    private readonly IMutatedProjectBuilder _mutatedProjectBuilder;

    /// <summary>
    /// All discovered mutations
    /// </summary>
    public List<DiscoveredMutation> DiscoveredMutations { get; } = new();

    private Solution? _mutatedSolution;

    public MutationDiscoveryManager(ISolutionProvider solutionProvider, IMutationImplementationProvider mutationImplementationProvider,
        IStatusTracker statusTracker, IMutatedProjectBuilder mutatedProjectBuilder)
    {
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(mutationImplementationProvider);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(mutatedProjectBuilder);

        _solutionProvider = solutionProvider;
        _mutationImplementationProvider = mutationImplementationProvider;
        _statusTracker = statusTracker;
        _mutatedProjectBuilder = mutatedProjectBuilder;
    }

    public void PerformMutationDiscovery()
    {
        // A mutation run must:
        // - traverse the syntax trees of each file to discover mutation opportunities.
        // - Once all potential mutations have been discovered, apply them from the bottom up as to reduce the chance
        //                  of mutations altering the tree in ways that prevent other discovered mutations.
        // - Once all mutations have been applied to a project, emit a new dll
        // - If emitting the dll causes build errors, find where the error occurred and remove mutations there.
        // - Update other projects that are dependent on the new dll
        // - Activate mutants 1 by 1 and run all tests (TODO only covering tests)
        // - Report if the mutant was killed or not

        if (!_statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants))
        {
            return;
        }

        _mutatedSolution = null;
        DiscoveredMutations.Clear();

        try
        {
            DiscoverMutants();
        }
        catch (Exception ex)
        {
            Log.Error("Error thrown while discovering mutants.", ex);
        }

        if (_mutatedSolution != null && _solutionProvider.SolutionContainer.Workspace.TryApplyChanges(_mutatedSolution))
        {
            // Because we have wrapped the projects and precomputed properties around them, we need to update these to match the mutated solution.
            _solutionProvider.SolutionContainer.RestoreProjects();
            _statusTracker.FinishOperation(DarwingOperation.DiscoveringMutants, true);
            _mutatedProjectBuilder.Build();
        }
        else
        {
            Log.Error("Failed to created mutated solution");
            _statusTracker.FinishOperation(DarwingOperation.DiscoveringMutants, false);
        }
    }

    private void DiscoverMutants()
    {
        foreach (IProjectContainer project in _solutionProvider.SolutionContainer.SolutionProjects)
        {
            foreach ((DocumentId documentId, SyntaxTree tree) in project.UnMutatedSyntaxTrees)
            {
                Log.Information($"Discovering mutations for {tree.FilePath}.");

                List<DiscoveredMutation> tempDiscoveredMutations = new List<DiscoveredMutation>();
                SyntaxNode mutatedRoot = TraverseSyntaxNodeForMutation(tree.GetRoot(), tempDiscoveredMutations);
                Log.Information($"Discovered {tempDiscoveredMutations.Count} mutations for {tree.FilePath}.");

                SyntaxTree mutatedTree = tree.WithRootAndOptions(mutatedRoot, tree.Options);

                // By applying the document ID here, we remove the need for the recursion to be aware of the document its traversing.
                tempDiscoveredMutations.ForEach(mutation => mutation.Document = documentId);
                DiscoveredMutations.AddRange(tempDiscoveredMutations);
                RediscoverMutationsInTree(mutatedRoot);

                ApplyDiscoveredMutationsToDocument(documentId, mutatedRoot);
            }
        }
    }

    /// <summary>
    /// Since each mutation we apply generates a whole new tree, the mutated nodes we've kept a reference to aren't the same nodes in our
    /// final mutated tree, so we need to rediscover them and where they are.
    /// This does mean we are traversing each file twice, but oh well, cant really do anything about it I don't think...
    /// </summary>
    public void RediscoverMutationsInTree(SyntaxNode syntaxNode)
    {
        if (DiscoveredMutations.FirstOrDefault(x => syntaxNode.HasAnnotation(x.ID)) is { MutatedNode: not null } rediscoveredMutation)
        {
            //Update the mutated node to the node in the actual mutated tree.
            rediscoveredMutation.MutatedNode = syntaxNode;
            //Only update the status to available if it was previously 'Discovered' to avoid making removed mutations show as available.
            if (rediscoveredMutation.Status == MutantStatus.Discovered)
            {
                rediscoveredMutation.Status = MutantStatus.Available;
            }
            // By assigning the line span here, we ensure that its based on the entire mutated tree.
            rediscoveredMutation.LineSpan = syntaxNode.SyntaxTree.GetLocation(syntaxNode.Span).GetLineSpan();
        }

        IEnumerable<SyntaxNode> children = syntaxNode.ChildNodes();
        foreach (SyntaxNode child in children)
        {
            RediscoverMutationsInTree(child);
        }
    }

    private void ApplyDiscoveredMutationsToDocument(DocumentId documentId, SyntaxNode mutatedRoot)
    {
        // If this is the first file being mutated, the mutated solution will be null, so just copy over the unmutated solution.
        _mutatedSolution ??= _solutionProvider.SolutionContainer.Solution;

        // This creates a new solution instance from the _mutatedSolution, rather than applying the changes directly to it.
        _mutatedSolution = _mutatedSolution.WithDocumentSyntaxRoot(documentId, mutatedRoot);

        //Output the full mutated file to a debug log. 
        Log.Debug(mutatedRoot.ToFullString());
    }

    private SyntaxNode TraverseSyntaxNodeForMutation(SyntaxNode node, List<DiscoveredMutation> mutations)
    {
        node = TryMutateNode(mutations, node);

        //Iterate/ mutate children first to achieve depth first search.
        Dictionary<SyntaxNode, SyntaxNode> mutatedChildren = new();
        foreach (SyntaxNode child in node.ChildNodes())
        {
            SyntaxNode childAfterTraversal = TraverseSyntaxNodeForMutation(child, mutations);
            mutatedChildren.Add(child, childAfterTraversal);
        }

        //Replace all the nodes children with their mutated counterparts. 
        node = node.ReplaceNodes(node.ChildNodes(), (x, _) =>
        {
            if (mutatedChildren.TryGetValue(x, out var mutated))
            {
                return mutated;
            }
            return x;
        });

        return node;
    }

    private SyntaxNode TryMutateNode(List<DiscoveredMutation> mutations, SyntaxNode node)
    {
        if (_mutationImplementationProvider.CanMutate(node, out IMutationImplementation? mutator))
        {
            try
            {
                (SyntaxNode mutatedNode, SyntaxAnnotation id) = mutator.Mutate(node);

                mutations.Add(new DiscoveredMutation(id, node, mutatedNode));

                Log.Debug($"Successfully discovered {mutator.Mutation} mutation.");
                return mutatedNode;
            }
            catch (MutationException ex)
            {
                Log.Error($"{mutator.GetType()} mutation implementation failed to create a mutated node. Error: {ex.Message}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error encountered while trying to create a mutation of type {mutator.Mutation}. {ex}");
            }
        }

        return node;
    }
}

