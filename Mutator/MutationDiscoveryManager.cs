using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Mutator.MutationImplementations;
using Serilog;

namespace Mutator;

/// <summary>
/// This class if responsible for something probably
/// </summary>
public class MutationDiscoveryManager : IMutationDiscoveryManager
{
    private readonly ISolutionProvider _solutionProvider;
    private readonly IMutationImplementationProvider _mutationImplementationProvider;
    private readonly IStatusTracker _statusTracker;
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _settings;

    /// <summary>
    /// All discovered mutations
    /// </summary>
    public List<DiscoveredMutation> DiscoveredMutations { get; } = new();

    private Solution? _mutatedSolution;

    public MutationDiscoveryManager(ISolutionProvider solutionProvider, IMutationImplementationProvider mutationImplementationProvider,
        IStatusTracker statusTracker, IEventAggregator eventAggregator, IMutationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(mutationImplementationProvider);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(eventAggregator);

        _solutionProvider = solutionProvider;
        _mutationImplementationProvider = mutationImplementationProvider;
        _statusTracker = statusTracker;
        _eventAggregator = eventAggregator;
        _settings = settings;
    }

    /// <inheritdoc/>
    public void PerformMutationDiscovery()
    {
        // A mutation run must:
        // - traverse the syntax trees of each file to discover mutation opportunities.
        // - Once all the child nodes of a node have been traversed, attempt to mutate the node itself.
        // - Once all mutations have been applied to a project, emit a new dll
        // - If emitting the dll causes build errors, find where the error occurred and remove mutations there.
        // - Update other projects that are dependent on the new dll
        // - Activate mutants 1 by 1 and run all tests (TODO only covering tests)
        // - Report if the mutant was killed or not

        if (!_statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants))
        {
            Log.Error($"Attempted to start invalid operation {DarwingOperation.DiscoveringMutants}.");
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
            _eventAggregator.GetEvent<BuildMutatedSolution>().Publish();
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
            foreach (SourceCodeFileContainer file in project.FileCollection)
            {
                if (!file.LinesToMutate.Any())
                {
                    continue;
                }
                Log.Information($"Discovering mutations for {file.Path}.");

                List<DiscoveredMutation> discoveredMutationsInFile = new List<DiscoveredMutation>();
                SyntaxNode mutatedRoot = TraverseSyntaxNodeForMutation(file.SyntaxTree.GetRoot(), discoveredMutationsInFile);
                Log.Information($"Discovered {discoveredMutationsInFile.Count} mutations for {file.Path}.");

                SyntaxTree mutatedTree = file.SyntaxTree.WithRootAndOptions(mutatedRoot, file.SyntaxTree.Options);
                file.MutatedTree = mutatedTree;

                // By applying the document ID here, we remove the need for the recursion to be aware of the document its traversing.
                discoveredMutationsInFile.ForEach(mutation => mutation.Document = file.DocumentId);
                DiscoveredMutations.AddRange(discoveredMutationsInFile);
                RediscoverMutationsInTree(mutatedRoot);
                
                if (discoveredMutationsInFile.Any(x => x.Status is MutantStatus.Discovered))
                {
                    Log.Error($"Unable to rediscover a mutation(s) in {file.Path}. It will not be tested and cannot be removed if it causes build errors.");
                }
                if (_settings.SingleMutantPerLine)
                {
                    HandleIgnoringBasedOnPosition(discoveredMutationsInFile, file.LinesToMutate);
                }

                ApplyDiscoveredMutationsToDocument(file.DocumentId, mutatedRoot);
            }
        }
    }

    private void HandleIgnoringBasedOnPosition(List<DiscoveredMutation> discoveredMutationsInFile, FileLineCollection linesToMutate)
    {
        //Must be called AFTER rediscovering mutations in tree to ensure line spans are correct.
        if (_settings.SingleMutantPerLine)
        {
            IEnumerable<IGrouping<int, DiscoveredMutation>> lineGroupings = discoveredMutationsInFile.GroupBy(mutation => mutation.LineSpan.StartLinePosition.Line);
            List<IGrouping<int, DiscoveredMutation>> linesWithMultipleMutants = [.. lineGroupings.Where(group => group.Count() > 1)];
        
            linesWithMultipleMutants.ForEach(group =>
            {
                //Keep the first mutation and ignore the rest.
                //TODO: This is pretty naive,
                //it would be better to have some sort of priority system to determine which mutation to keep rather than just keeping the first one we come across.
                group.Skip(1).ToList().ForEach(mutation =>
                {
                    mutation.Status = MutantStatus.IgnoredMultipleOnLine;
                    Log.Debug($"Ignoring mutation {mutation.ID} on line {mutation.LineSpan.StartLinePosition.Line} in {mutation.OriginalNode.SyntaxTree.FilePath} because there is already another mutation on that line and the setting to only allow one mutant per line is enabled.");
                });
            });
        }

        // Ignore mutatns which shouldn't have been made, but we didn't know shouldnt have been made at the time
        discoveredMutationsInFile.Where(x => !linesToMutate.ContainsLine(x.LineSpan.StartLinePosition.Line)).ToList().ForEach(mutation =>
        {
            mutation.Status = MutantStatus.IgnoreOutsideRange;
            Log.Debug($"Ignoring mutation {mutation.ID} on line {mutation.LineSpan.StartLinePosition.Line} in {mutation.OriginalNode.SyntaxTree.FilePath} since it is outside range of mutation.");
        });
    }

    /// <summary>
    /// Since each mutation we apply generates a whole new tree, the mutated nodes we've kept a reference to aren't the same nodes in our
    /// final mutated tree, so we need to rediscover them and where they are.
    /// This does mean we are traversing each file twice, but oh well, cant really do anything about it I don't think...
    /// </summary>
    public void RediscoverMutationsInTree(SyntaxNode syntaxNode)
    {
        if (DiscoveredMutations.FirstOrDefault(x => syntaxNode.HasAnnotation(x.ID)) is { MutationSwitcher: not null } rediscoveredMutation)
        {
            //Update the mutated node to the node in the actual mutated tree.
            rediscoveredMutation.MutationSwitcher = syntaxNode;
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
                (SyntaxNode mutationSwitcher, SyntaxAnnotation id, SyntaxNode mutatedNode) = mutator.Mutate(node);

                mutations.Add(new DiscoveredMutation(id, node, mutationSwitcher, mutatedNode, _eventAggregator, mutator.Category, mutator.Mutation));

                Log.Debug($"Successfully discovered {mutator.Mutation} mutation.");
                return mutationSwitcher;
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

