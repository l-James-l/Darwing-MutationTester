using Microsoft.CodeAnalysis;
using Models;

namespace Mutator;

public interface IMutationDiscoveryManager
{
    /// <summary>
    /// Provides access to all discovered mutations
    /// </summary>
    List<DiscoveredMutation> DiscoveredMutations { get; }

    /// <summary>
    /// Called to traverse all source code files and discover mutation opportunities.
    /// Will then call to build the mutated solution <see cref="MutatedProjectBuilder"/>. 
    /// </summary>
    void PerformMutationDiscovery();

    /// <summary>
    /// Since each mutation we apply generates a whole new tree, the mutated nodes we've kept a reference to aren't the same nodes in our
    /// final mutated tree, so we need to rediscover them and where they are.
    /// This does mean we are traversing each file twice, but oh well, cant really do anything about it I don't think...
    /// </summary>
    void RediscoverMutationsInTree(SyntaxNode mutatedRoot);

}