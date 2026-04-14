using Models;
using Models.Enums;

namespace CLI;

public static class CLICommandLineParser
{
    private const string slnPathFlag = "--sln";
    private const string testProjectFlag = "--test-projects";
    private const string sourceProjectFlag = "--source-projects";
    private const string ignoreProjectFlag = "--ignore-projects";


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

        // Check for solution path flag. --sln <path>
        if (argsList.IndexOf(slnPathFlag) is int slnFlagIndex && slnFlagIndex != -1)
        {
            if (slnFlagIndex < argsList.Count - 1)
            {
                mutationSettings.SolutionPath = argsList[slnFlagIndex + 1];
            }
        }

        // Check for test project names flag. --test-projects <name1> <name2> ...
        if (argsList.IndexOf(testProjectFlag) is int testProjectIndex && testProjectIndex != -1)
        {
            while (testProjectIndex++ < argsList.Count - 1 && !argsList[testProjectIndex].StartsWith("--"))
            {
                mutationSettings.TestProjects.Add(argsList[testProjectIndex]);
            }
        }
        if (argsList.IndexOf(sourceProjectFlag) is int sourceProjectIndex && sourceProjectIndex != -1)
        {
            while (sourceProjectIndex++ < argsList.Count - 1 && !argsList[sourceProjectIndex].StartsWith("--"))
            {
                mutationSettings.SourceCodeProjects.Add(argsList[sourceProjectIndex]);
            }
        }
        if (argsList.IndexOf(ignoreProjectFlag) is int ignoreProjectIndex && ignoreProjectIndex != -1)
        {
            while (ignoreProjectIndex++ < argsList.Count - 1 && !argsList[ignoreProjectIndex].StartsWith("--"))
            {
                mutationSettings.IgnoreProjects.Add(argsList[ignoreProjectIndex]);
            }
        }
    }
}