using Buildalyzer;
using Microsoft.CodeAnalysis;
using Models.Enums;
using Serilog;

namespace Models;


/// <summary>
/// Storing project information in a container allows tests to mock project info without loading a real project.
/// </summary>
public class ProjectContainer : IProjectContainer
{
    private Project _project;

    public ProjectContainer(Project project, IProjectAnalyzer projectAnalyzer, bool advancedTypeAnalysis)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(project.FilePath);

        _project = project;
        CsprojFilePath = project.FilePath;
        DirectoryPath = Path.GetDirectoryName(_project.FilePath) ??
            CsprojFilePath.Remove(CsprojFilePath.Count() - _project.Name.Count());

        Log.Information("Determining type of project: {proj}", Name);
        if (advancedTypeAnalysis)
        {
            DetermineTypeFromBuildAnalysis(projectAnalyzer);
        }
        else
        {
            DetermineTypeFromCsprojAnalysis();
        }

        //TODO: maybe throwing here isnt the most robust solution.
        DllFilePath = project.OutputFilePath ?? throw new Exception($"Could not establish the output file path for {Name}");
    }

    public ProjectId ID => _project.Id;

    public string CsprojFilePath { get; }

    public string DirectoryPath { get; }

    public string Name => _project.Name;

    public string AssemblyName => _project.AssemblyName;

    public string DllFilePath { get; }

    public SourceCodeFileCollection FileCollection { get; } = new();

    public ProjectType ProjectType { get; set; }

    public Compilation? GetCompilation() => _project.GetCompilationAsync().GetAwaiter().GetResult();

    public void UpdateFromMutatedProject(Project proj)
    {
        if (proj.Id == _project.Id)
        {
            _project = proj;
        }
        else
        {
            Log.Error("Could not update project {project} as IDS did not match.", _project.Name);
        }
    }

    private void DetermineTypeFromBuildAnalysis(IProjectAnalyzer project)
    {
        Log.Information("Using advanced project build analysis");
        IAnalyzerResults results = project.Build();
        // I believe the possibility for multiple build results is for when a project targets multiple frameworks,
        // but if its a test project in one, its a test project in all of them. So just checking the first is good.
        IAnalyzerResult? result = results.FirstOrDefault();

        if (result == null)
        {
            Log.Warning("Unable to analyse the project {proj}. Will assume its not a test project");
            return;
        }

        // In almost all cases, this will be sufficient.
        if (result.Properties.TryGetValue("IsTestProject", out var isTest) &&
            bool.TryParse(isTest, out var parsed) &&
            parsed)
        {
            Log.Information("Determined {proj} is a test project.", Name);
            ProjectType = ProjectType.Test;
            return;
        }

        // If it doesn't have the test project property, but uses the test project sdk, assume its a test project.
        // Same goes for the standard test platform. More could be added here such as moq or NSubstitute, but this is suffcient.
        string[] frameworks =
        {
            "Microsoft.NET.Test.Sdk",
            "xunit",
            "nunit",
            "mstest.testframework"
        };

        if (result.PackageReferences.Any(p =>
            frameworks.Any(f =>
                p.Key.Equals(f, StringComparison.OrdinalIgnoreCase))))
        {
            Log.Information("Determined {proj} is a test project.", Name);
            ProjectType = ProjectType.Test;
            return;
        }
    }


    private void DetermineTypeFromCsprojAnalysis()
    {
        Log.Information("Using basic project file analysis");
        string projFile;
        try
        {
            projFile = File.ReadAllText(CsprojFilePath);
        }
        catch (Exception ex)
        {
            Log.Error("Reading csproj file for project {proj} failed. Will attempt to determine project type from build analysis. {ex}", Name, ex);
            return;
        }

        // If any of the following strings are present in the csproj file, its a fairly safe assumption the project is a unit test project.
        // not fool proof. for that use the slower advanced detection
        string[] testFlags =
        {
            "<IsTestProject>true</IsTestProject>",
            "<IsTestProject> true</IsTestProject >",
            "<IsTestProject>true </IsTestProject >",
            "<IsTestProject> true </IsTestProject>",
            "<IsTestProject >true </IsTestProject>",
            "<IsTestProject>true< /IsTestProject>",
            "<IsTestProject > true < /IsTestProject >",
            "Microsoft.NET.Test.Sdk",
            "xunit",
            "nunit",
            "mstest.testframework"
        };

        if (testFlags.Any(projFile.Contains))
        {
            ProjectType = ProjectType.Test;
        }
    }
}
