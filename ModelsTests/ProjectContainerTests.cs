using Buildalyzer;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

namespace ModelsTests;

public class ProjectContainerTests
{
    private AdhocWorkspace _workspace;
    private IProjectAnalyzer _mockAnalyzer;
    private IMutationSettings _mockSettings;

    [SetUp]
    public void SetUp()
    {
        _workspace = new AdhocWorkspace();
        _mockAnalyzer = Substitute.For<IProjectAnalyzer>();
        _mockSettings = Substitute.For<IMutationSettings>();

        // Default settings to avoid null refs in constructor logic
        _mockSettings.SourceCodeProjects.Returns(new List<string>());
        _mockSettings.TestProjects.Returns(new List<string>());
        _mockSettings.IgnoreProjects.Returns(new List<string>());
    }

    [TearDown]
    public void TearDown() => _workspace.Dispose();

    private Project CreateMockProject(string name = "TestProj", string filePath = "C:\\src\\TestProj.csproj", string? outputHandle = "C:\\src\\bin\\TestProj.dll")
    {
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            filePath: filePath,
            outputFilePath: outputHandle);

        return _workspace.AddProject(projectInfo);
    }

    [Test]
    public void Constructor_GivenProjectSettingsMatchName_ThenSetTypeToSource()
    {
        // Arrange
        var project = CreateMockProject(name: "MyProject");
        _mockSettings.SourceCodeProjects.Returns(new List<string> { "MyProject" });

        // Act
        var container = new ProjectContainer(project, _mockAnalyzer, _mockSettings); //SUT

        // Assert
        Assert.That(container.ProjectType, Is.EqualTo(ProjectType.Source));
    }

    [Test]
    public void Constructor_GivenAdvancedAnalysisEnabledAndIsTestProjectPropertySet_ThenSetTypeToTest()
    {
        // Arrange
        var project = CreateMockProject();
        _mockSettings.UseAdvancedProjectTypeAnalysis.Returns(true);

        var mockResults = Substitute.For<IAnalyzerResults>();
        var mockResult = Substitute.For<IAnalyzerResult>();

        mockResult.Properties.Returns(new Dictionary<string, string> { { "IsTestProject", "true" } });
        mockResults.GetEnumerator().Returns(new List<IAnalyzerResult> { mockResult }.GetEnumerator());
        _mockAnalyzer.Build().Returns(mockResults);

        // Act
        var container = new ProjectContainer(project, _mockAnalyzer, _mockSettings); //SUT

        // Assert
        Assert.That(container.ProjectType, Is.EqualTo(ProjectType.Test));
    }

    [Test]
    public void Constructor_GivenAdvancedAnalysisAndTestPackagePresent_ThenSetTypeToTest()
    {
        // Arrange
        var project = CreateMockProject();
        _mockSettings.UseAdvancedProjectTypeAnalysis.Returns(true);

        var mockResults = Substitute.For<IAnalyzerResults>();
        var mockResult = Substitute.For<IAnalyzerResult>();

        // The inner dictionary typically contains metadata like "Version"
        var packageMetadata = new Dictionary<string, string> { { "Version", "2.4.1" } };

        // The outer dictionary maps "PackageName" -> Metadata
        var packageRefs = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            { "xunit", packageMetadata }
        };

        mockResult.Properties.Returns(new Dictionary<string, string>());
        mockResult.PackageReferences.Returns(packageRefs);

        // Setup the enumerator so FirstOrDefault() works
        mockResults.GetEnumerator().Returns(new List<IAnalyzerResult> { mockResult }.GetEnumerator());
        _mockAnalyzer.Build().Returns(mockResults);

        // Act
        var container = new ProjectContainer(project, _mockAnalyzer, _mockSettings); //SUT

        // Assert
        Assert.That(container.ProjectType, Is.EqualTo(ProjectType.Test));
    }

    [Test]
    public void UpdateFromMutatedProject_GivenMatchingId_ThenUpdateInternalProject()
    {
        // Arrange
        var originalProject = CreateMockProject(name: "Name");
        var container = new ProjectContainer(originalProject, _mockAnalyzer, _mockSettings); //SUT

        var newProject = _workspace.CurrentSolution
            .GetProject(originalProject.Id)!
            .WithAssemblyName("NewName");

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        container.UpdateFromMutatedProject(newProject);

        // Assert
        LogEvent? log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text.Contains("Could not update project"));
        Assert.That(log, Is.Null);
    }
    
    [Test]
    public void UpdateFromMutatedProject_GivenNonMatchingId_ThenUpdateInternalProject()
    {
        // Arrange
        var originalProject = CreateMockProject(name: "Name");
        var differentProject = CreateMockProject(name: "Name2");
        var container = new ProjectContainer(originalProject, _mockAnalyzer, _mockSettings); //SUT

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        container.UpdateFromMutatedProject(differentProject);

        // Assert
        LogEvent? log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text.Contains("Could not update project"));
        Assert.That(log, Is.Not.Null);
        Assert.That(log.Level, Is.EqualTo(LogEventLevel.Error));
    }

    [Test]
    public void Constructor_GivenNoOutputFilePath_ThenThrowException()
    {
        // Arrange
        var projectWithoutOutput = CreateMockProject(outputHandle: null);

        // Act & Assert
        var ex = Assert.Throws<Exception>(() =>
            new ProjectContainer(projectWithoutOutput, _mockAnalyzer, _mockSettings)); //SUT

        Assert.That(ex.Message, Does.Contain("Could not establish the output file path"));
    }
}