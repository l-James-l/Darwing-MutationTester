using Microsoft.CodeAnalysis;
using Models;

namespace Mutator;

public interface IMutationDiscoveryManager
{
    List<DiscoveredMutation> DiscoveredMutations { get; }

    void RediscoverMutationsInTree(SyntaxNode mutatedRoot);

}