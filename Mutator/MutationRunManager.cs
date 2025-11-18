using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Models;
using Mutator.MutationImplementations;
using Serilog;

namespace Mutator;

/// <summary>
/// This class if responsible for something probably
/// </summary>
public class MutationRunManager : IMutationRunManager
{
    private ISolutionProvider _solutionProvider;
    private IMutationDiscoveryManager _mutationDiscoveryManager;

    /// <summary>
    /// All discovered mutations, keyed by file path
    /// </summary>
    private List<DiscoveredMutation> _discoveredMutations = new();

    private Solution? _mutatedSolution;

    public MutationRunManager(ISolutionProvider solutionProvider, IMutationDiscoveryManager mutationDiscoveryManager)
    {
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(mutationDiscoveryManager);

        _solutionProvider = solutionProvider;
        _mutationDiscoveryManager = mutationDiscoveryManager;
    }

    public void Run(InitialTestRunInfo testRunInfo)
    {
        // A mutation run must:
        // - traverse the syntax trees of each file to discover mutation oppertuniteis.
        // - Once all potential mutations have been disovered, apply them from the bottom up as to reduce the chance
        //                  of mutations altering the tree in ways that prevent other discovered mutations.
        // - Once all mutations have been applied to a project, emit a new dll
        // - If emmitting the dll causes build errors, find where the error occured and remove mutations there.
        // - Update other projects that are dependent on the new dll
        // - Activate mutants 1 by 1 (TODO multiple at a time?) and run all tests (TODO only covering tests)
        // - Report if the mutant was killed or not

        ArgumentNullException.ThrowIfNull(testRunInfo);

        _mutatedSolution = null;
        _discoveredMutations.Clear();

        foreach (IProjectContainer project in _solutionProvider.SolutionContiner.SolutionProjects)
        {
            foreach ((DocumentId documentId, SyntaxTree tree) in project.SyntaxTrees)
            {
                Log.Information($"Discovering mutations for {tree.FilePath}.");

                List<DiscoveredMutation> discoveredMutations = new List<DiscoveredMutation>();
                SyntaxNode muatedRoot = TraverseSyntxNode(discoveredMutations, tree.GetRoot());
                Log.Information($"Discovered {discoveredMutations.Count} mutations for {tree.FilePath}.");

                // At this stage this dictionary is only to track muations which have been applied.
                // By applying the document ID here, we remove the need for the recrsion to be aware of the document its traversing.
                discoveredMutations.ForEach(x => x.Document = documentId);
                _discoveredMutations.AddRange(discoveredMutations);

                ApplyDiscoveredMutationsToDocument(documentId, muatedRoot);
            }
        }
    }

    private void ApplyDiscoveredMutationsToDocument(DocumentId documentId, SyntaxNode mutatedRoot)
    {
        // If this is the first file being mutated, the mutated solution will be null, so just copy over the unmutated solution.
        _mutatedSolution ??= _solutionProvider.SolutionContiner.Solution;

        // This creates a new solution instance from the _mutatedSolution, rather than applying the changes directly to it.
        _mutatedSolution = _mutatedSolution.WithDocumentSyntaxRoot(documentId, mutatedRoot);

        //Output the full mutated file to a debug log. 
        Log.Debug(mutatedRoot.ToFullString());
    }

    private SyntaxNode TraverseSyntxNode(List<DiscoveredMutation> mutations, SyntaxNode node)
    {
        //TODO: should probably include some recursion protection here.

        //Itterate/ mutate children first to achieve depth first search.
        Dictionary<SyntaxNode, SyntaxNode> mutatedChildren = new();
        foreach (SyntaxNode child in node.ChildNodes())
        {
            SyntaxNode childAfterTraversal = TraverseSyntxNode(mutations, child);
            mutatedChildren.Add(child, childAfterTraversal);
        }

        //Replace all the nodes children with thier mutated counterparts. Note that because we do a depth first search, this will include
        //all mutation to children of the children and so on...
        node = node.ReplaceNodes(node.ChildNodes(), (x, _) =>
        {
            if (mutatedChildren.TryGetValue(x, out var mutated))
            {
                return mutated;
            }
            return x;
        });

        // Once we have completed mutation checks for all the nodes children, we can try and mutate the node itself
        return TryMutateNode(mutations, node);
    }

    private SyntaxNode TryMutateNode(List<DiscoveredMutation> mutations, SyntaxNode node)
    {
        if (_mutationDiscoveryManager.CanMutate(node, out IMutationImplementation? mutator))
        {
            try
            {
                SyntaxNode mutatedNode = mutator.Mutate(node);
                mutations.Add(new DiscoveredMutation
                {
                    OriginalNode = node,
                    MutatedNode = mutatedNode,
                });
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
