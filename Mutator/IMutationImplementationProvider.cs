using Microsoft.CodeAnalysis;
using Mutator.MutationImplementations;
using System.Diagnostics.CodeAnalysis;

namespace Mutator;

public interface IMutationImplementationProvider
{
    bool CanMutate(SyntaxNode node, [NotNullWhen(true)] out IMutationImplementation? mutator);
}
