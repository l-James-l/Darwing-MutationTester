namespace Models.Enums;

public enum MutantStatus
{
    /// <summary>
    /// Mutation has been applied, but we dont know where it is because we either haven't rediscovered it yet,
    /// or weren't able to rediscover it.
    /// </summary>
    Discovered,

    /// <summary>
    /// Mutation has been applied and we have successfully rediscover it.
    /// Mutant is ready for testing.
    /// </summary>
    Available,

    /// <summary>
    /// Mutant has been removed due to it causing build errors.
    /// </summary>
    CausedBuildError,

    /// <summary>
    /// Mutant is currently active and a test run is ongoing.
    /// </summary>
    TestOngoing,

    /// <summary>
    /// Mutant has been tested and it survived - no tests failed.
    /// </summary>
    Survived,

    /// <summary>
    /// Mutant has been tested and it was killed - failed unit tests.
    /// </summary>
    Killed
}
