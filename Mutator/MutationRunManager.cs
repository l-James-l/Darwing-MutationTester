using Core.Interfaces;
using Models;

namespace Mutator;

/// <summary>
/// This class if responsible for something probably
/// </summary>
public class MutationRunManager : IMutationRunManager
{
    private ISolutionProvider _solutionProvider;
    public MutationRunManager(ISolutionProvider solutionProvider)
    {
        ArgumentNullException.ThrowIfNull(solutionProvider);

        _solutionProvider = solutionProvider;
    }

    public void Run(InitialTestRunInfo testRunInfo)
    {
        // A mutation run must:
        // - Discover all source code files within projects being mutated.
        // - traverse the syntax trees of each file to discover mutation oppertuniteis.
        // - Once all potential mutations have been disovered, apply them from the bottom up as to reduce the chance
        //                  of mutations altering the tree in ways that prevent other discovered mutations.
        // - Once all mutations have been applied to a project, emit a new dll
        // - If emmitting the dll causes build errors, find where the error occured and remove mutations there.
        // - Update other projects that are dependent on the new dll
        // - Activate mutants 1 by 1 (TODO multiple at a time?) and run all tests (TODO only covering tests)
        // - Report if the mutant was killed or not

        ArgumentNullException.ThrowIfNull(testRunInfo);

        

    }
}
