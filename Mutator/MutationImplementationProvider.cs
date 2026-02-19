using Microsoft.CodeAnalysis;
using Models;
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
    private readonly IMutationSettings _settings;

    public MutationImplementationProvider(IEnumerable<IMutationImplementation> availableMutations, IMutationSettings settings)
    {
        _availableMutations = availableMutations;
        _settings = settings;
    }

    public bool CanMutate(SyntaxNode node, [NotNullWhen(true)] out IMutationImplementation? mutator)
    {
        mutator = null;
        if (node.HasAnnotation(Annotations.DontMutateAnnotation))
        {
            return false;
        }

        mutator = _availableMutations.FirstOrDefault(x => node.GetType() == x.RequiredNodeType && node.IsKind(x.Kind) 
                && !_settings.DisabledMutationTypes.Contains(x.Mutation));
        return mutator is not null;
    }
}