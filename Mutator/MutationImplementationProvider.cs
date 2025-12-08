using Microsoft.CodeAnalysis;
using Mutator.MutationImplementations;
using System.Diagnostics.CodeAnalysis;

namespace Mutator;

/// <summary>
/// Takes an injection for every implementation of <see cref="IMutationImplementation"/> so that when checking nodes,
/// we can find the appropriate mutator, if one exists.
/// </summary>
public class MutationImplementationProvider : IMutationImplementationProvider
{
    private readonly IEnumerable<IMutationImplementation> _availableMutations;

    public MutationImplementationProvider(IEnumerable<IMutationImplementation> availableMutations)
    {
        _availableMutations = availableMutations;
    }

    public bool CanMutate(SyntaxNode node, [NotNullWhen(true)] out IMutationImplementation? mutator)
    {
        mutator = null;
        if (node.HasAnnotation(BaseMutationImplementation.DontMutateAnnotation))
        {
            return false;
        }

        mutator = _availableMutations.FirstOrDefault(x => node.GetType() == x.RequiredNodeType && node.IsKind(x.Kind));
        return mutator is not null;
    }
}