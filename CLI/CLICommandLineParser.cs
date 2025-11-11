using Models;

public static class CLICommandLineParser
{
    private const string devModeFlag = "--dev";
    private const string slnPathFlag = "--sln";
    private const string testProjectFlag = "--test-projects";

    /// <summary>
    /// Will parse the provided CLI args and set the mutation settings accordingly.
    /// Doesn't do any validation of the args.
    /// </summary>
    /// <param name="args">The command line args</param>
    public static void ParseCliArgs(this IMutationSettings mutationSettings, string[] args)
    {
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(args);

        List<string> argsList = args.ToList();

        mutationSettings.DevMode = args.Any(x => x == devModeFlag);

        // Check for solution path flag. --sln <path>
        if (argsList.IndexOf(slnPathFlag) is int slnFlagIndex && slnFlagIndex != -1)
        {
            if (slnFlagIndex <  argsList.Count - 1)
            {
                mutationSettings.SolutionPath = argsList[slnFlagIndex + 1];
            }
        }

        // Check for test project names flag. --test-projects <name1> <name2> ...
        if (argsList.IndexOf(testProjectFlag) is int testProjectIndex && testProjectIndex != -1)
        {
            while (testProjectIndex++ < argsList.Count - 1 && !argsList[testProjectIndex].StartsWith("--"))
            {
                mutationSettings.TestProjectNames.Add(argsList[testProjectIndex]);
            }
        }
    }
}