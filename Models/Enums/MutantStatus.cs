namespace Models.Enums;

public enum MutantStatus
{
    //-----------------------------
    // The following are all invalid mutations that should not be included in the report
    // -----------------------------

    /// <summary>
    /// Mutation has been applied, but we dont know where it is because we either haven't rediscovered it yet,
    /// or weren't able to rediscover it.
    /// </summary>
    Discovered,

    /// <summary>
    /// Mutant has been removed due to it causing build errors.
    /// </summary>
    CausedBuildError,

    /// <summary>
    /// Indicates a mutant that was applied to a file, and then wont be tested as that line in the file 
    /// has been set to not be mutated.
    /// </summary>
    /// <remarks>
    /// Used when there are some lines in the file that are being mutated, but since during mutation we dont know line spans,
    /// we cant say if the mutation is in the accepted range or not.
    /// </remarks>
    IgnoreOutsideRange,

    //-----------------------------
    // The following are all valid mutations that should be included in the report
    // ----------------------------

    /// <summary>
    /// Where the settings specify that multiple mutations on the same line should be ignored, this status is used to mark the mutants that have been ignored.
    /// They are not tested, but we want to keep track of them so we can report on how many mutants were ignored due to this setting.
    /// </summary>
    IgnoredMultipleOnLine,

    /// <summary>
    /// Mutation has been applied and we have successfully rediscover it.
    /// Mutant is ready for testing.
    /// </summary>
    Available,

    /// <summary>
    /// Mutant hsa been applied, but is not covered by any tests, so dont test it and treat it as survived.
    /// </summary>
    NoCoverage,

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
    Killed,

    /// <summary>
    /// Mutant was killed by a timeout, i.e., its test process ran out
    /// </summary>
    KilledByTimeOut
}

public static class MutationStatusMethods
{
    public static bool IncludeInReport(this MutantStatus mutantStatus)
    {
        return mutantStatus >= MutantStatus.IgnoredMultipleOnLine;
    }

    public static bool IncludeInTotalCount(this MutantStatus mutantStatus)
    {
        return mutantStatus >= MutantStatus.Available;
    }

    public static bool IncludeInKilledCount(this MutantStatus mutantStatus)
    {
        return mutantStatus is MutantStatus.Killed or MutantStatus.KilledByTimeOut;
    }

    public static bool IncludeInSurvivedCount(this MutantStatus mutantStatus)
    {
        return mutantStatus is MutantStatus.Survived or MutantStatus.NoCoverage;
    }
}

