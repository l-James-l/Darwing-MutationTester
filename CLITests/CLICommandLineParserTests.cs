using CLI;
using Models;
using NSubstitute;

namespace CLITests;

public class CLICommandLineParserTests
{
    private IMutationSettings _mutationSettings;

    [SetUp]
    public void SetUp()
    {
        _mutationSettings = Substitute.For<IMutationSettings>();

        // Ensure lists are initialized so .Add() doesn't throw NullReferenceException
        _mutationSettings.TestProjects.Returns(new List<string>());
        _mutationSettings.SourceCodeProjects.Returns(new List<string>());
        _mutationSettings.IgnoreProjects.Returns(new List<string>());
    }

    [Test]
    public void ParseCliArgs_GivenSlnFlag_SetsSolutionPath()
    {
        // Arrange
        string[] args = { "--sln", @"C:\Code\MySln.sln" };

        // Act
        _mutationSettings.ParseCliArgs(args);

        // Assert
        _mutationSettings.Received().SolutionPath = @"C:\Code\MySln.sln";
    }

    [Test]
    public void ParseCliArgs_GivenMultiValueProjectFlags_PopulatesListsCorrectly()
    {
        // Arrange
        string[] args =
        {
            "--test-projects", "TestProj1", "TestProj2",
            "--source-projects", "SourceProj1",
            "--ignore-projects", "Ignore1", "Ignore2", "Ignore3"
        };

        // Act
        _mutationSettings.ParseCliArgs(args);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_mutationSettings.TestProjects, Has.Count.EqualTo(2));
            Assert.That(_mutationSettings.TestProjects, Contains.Item("TestProj1"));
            Assert.That(_mutationSettings.TestProjects, Contains.Item("TestProj2"));

            Assert.That(_mutationSettings.SourceCodeProjects, Has.Count.EqualTo(1));
            Assert.That(_mutationSettings.SourceCodeProjects, Contains.Item("SourceProj1"));

            Assert.That(_mutationSettings.IgnoreProjects, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void ParseCliArgs_WhenFlagIsLastArgument_DoesNotThrow()
    {
        // Arrange
        string[] args = { "--sln" }; // No path provided after flag

        // Act & Assert
        Assert.DoesNotThrow(() => _mutationSettings.ParseCliArgs(args));
        _mutationSettings.DidNotReceive().SolutionPath = Arg.Any<string>();
    }

    [Test]
    public void ParseCliArgs_WhenArgsAreEmpty_DoesNotModifySettings()
    {
        // Arrange
        string[] args = Array.Empty<string>();

        // Act
        _mutationSettings.ParseCliArgs(args);

        // Assert
        _mutationSettings.DidNotReceive().SolutionPath = Arg.Any<string>();
        Assert.That(_mutationSettings.TestProjects, Is.Empty);
    }

    [Test]
    public void ParseCliArgs_GivenMixedOrderWithOtherFlags_StopsParsingAtNextFlag()
    {
        // Arrange
        // Verify that parsing for --test-projects stops when it hits --source-projects
        string[] args = { "--test-projects", "T1", "T2", "--source-projects", "S1" };

        // Act
        _mutationSettings.ParseCliArgs(args);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_mutationSettings.TestProjects, Has.Count.EqualTo(2));
            Assert.That(_mutationSettings.TestProjects, Does.Not.Contain("--source-projects"));
            Assert.That(_mutationSettings.SourceCodeProjects, Has.Count.EqualTo(1));
        });
    }
}