using Models;

namespace Mutator;

public interface IMutationRunInitiator
{
    void Run(InitialTestRunInfo testRunInfo);
}
