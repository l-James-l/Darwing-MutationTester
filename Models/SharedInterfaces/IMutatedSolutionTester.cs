namespace Models.SharedInterfaces;

public interface IMutatedSolutionTester
{
    /// <summary>
    /// Run tests on all discovered mutants in the mutated solution
    /// </summary>
    void RunTestsOnMutatedSolution();
}
