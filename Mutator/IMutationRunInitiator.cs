using Models;

namespace Mutator;

public interface IMutationRunInitiator
{
    /// <summary>
    /// Starts the mutation testing process.
    /// The first step is an unmutated test run to establish a baseline.
    /// </summary>
    void Run();
}
