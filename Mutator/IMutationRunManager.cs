using Models;

namespace Mutator;

public interface IMutationRunManager
{
    void Run(InitialTestRunInfo testRunInfo);
}