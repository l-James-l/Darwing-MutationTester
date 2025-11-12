using Models;

namespace CLITests;

public class CLICommandLineParserTests
{
    private IMutationSettings _mutationSettings;

    [SetUp]
    public void Setup()
    {
        _mutationSettings = new MutationSettings();
    }

    [Test]
    public void GivenEmptyArgs_WhenParseCliArgs_ThenNoSettingsSet()
    {
        // Arrange
        string[] args = Array.Empty<string>();

        // Act
        _mutationSettings.ParseCliArgs(args);
        
        // Assert
        Assert.That(_mutationSettings.SolutionPath, Is.Empty);
        Assert.That(_mutationSettings.TestProjectNames, Is.Empty);
    }

    [Test]
    public void GivenSolutionPathFlag_WhenParseCliArgs_ThenSolutionPathSet()
    {
        // Arrange
        string expectedPath = @"C:\Projects\MySolution.sln";
        string[] args = new[] { "--sln", expectedPath };
        // Act
        _mutationSettings.ParseCliArgs(args);
        
        // Assert
        Assert.That(_mutationSettings.SolutionPath, Is.EqualTo(expectedPath));
    }

    [Test]
    public void GivenTestProjectNamesFlag_WhenParseCliArgs_ThenTestProjectNamesSet()
    {
        // Arrange
        string[] expectedNames = new[] { "TestProject1", "TestProject2" };
        string[] args = new[] { "--test-projects", "TestProject1", "TestProject2" };
        
        // Act
        _mutationSettings.ParseCliArgs(args);
        
        // Assert
        Assert.That(_mutationSettings.TestProjectNames, Is.EquivalentTo(expectedNames));
    }

    [Test]
    public void GivenSlnFlagWithoutPath_WhenParseCliArgs_ThenSolutionPathNotSet()
    {
        // Arrange
        string[] args = new[] { "--sln" };
        // Act
        _mutationSettings.ParseCliArgs(args);
        
        // Assert
        Assert.That(_mutationSettings.SolutionPath, Is.Empty);
    }

    [Test]
    public void GivenTestProjectNamesFlagAndNoNamesGiven_WhenParseCliArgs_ThenTestProjectNamesNotSet()
    {
        // Arrange
        string[] args = new[] { "--test-projects" };
        
        // Act
        _mutationSettings.ParseCliArgs(args);
        
        // Assert
        Assert.That(_mutationSettings.TestProjectNames, Is.Empty);
    }
}
