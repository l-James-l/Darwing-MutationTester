using Models;
using Models.Enums;

namespace ModelsTests;

public class MutationSettingsTests
{
    [Test]
    public void MutationSettings_PropertyGettersAndSetters_SetCorrectValues()
    {
        // Arrange
        var settings = new MutationSettings();

        // Define test values
        var solutionPath = @"C:\Repo\Darwin.sln";
        var testProjects = new List<string> { "TestProj" };
        var ignoreProjects = new List<string> { "IgnoreProj" };
        var sourceProjects = new List<string> { "SourceProj" };
        var disabledMutations = new List<SpecificMutation> { SpecificMutation.AddToSubtract };
        var aiInstructions = "Focus on edge cases.";

        // Act - Set all properties
        settings.SolutionPath = solutionPath;
        settings.TestProjects = testProjects;
        settings.IgnoreProjects = ignoreProjects;
        settings.SourceCodeProjects = sourceProjects;
        settings.SingleMutantPerLine = false;
        settings.TestRunTimeout = 5000;
        settings.BuildTimeout = 60;
        settings.SkipTestingNoActiveMutants = true;
        settings.DisabledMutationTypes = disabledMutations;
        settings.UseAdvancedProjectTypeAnalysis = true;
        settings.DefaultGitComparisonBranch = "develop";
        settings.MutationTestTimeoutScaler = 2.5;
        settings.AiTestGenerationAdditionalInstructions = aiInstructions;

        // Assert - Get and verify all properties
        Assert.Multiple(() =>
        {
            Assert.That(settings.SolutionPath, Is.EqualTo(solutionPath));
            Assert.That(settings.TestProjects, Is.EqualTo(testProjects));
            Assert.That(settings.IgnoreProjects, Is.EqualTo(ignoreProjects));
            Assert.That(settings.SourceCodeProjects, Is.EqualTo(sourceProjects));
            Assert.That(settings.SingleMutantPerLine, Is.False);
            Assert.That(settings.TestRunTimeout, Is.EqualTo(5000));
            Assert.That(settings.BuildTimeout, Is.EqualTo(60));
            Assert.That(settings.SkipTestingNoActiveMutants, Is.True);
            Assert.That(settings.DisabledMutationTypes, Is.EqualTo(disabledMutations));
            Assert.That(settings.UseAdvancedProjectTypeAnalysis, Is.True);
            Assert.That(settings.DefaultGitComparisonBranch, Is.EqualTo("develop"));
            Assert.That(settings.MutationTestTimeoutScaler, Is.EqualTo(2.5));
            Assert.That(settings.AiTestGenerationAdditionalInstructions, Is.EqualTo(aiInstructions));
        });
    }

    [Test]
    public void MutationSettings_Defaults_AreCorrect()
    {
        // Arrange & Act
        var settings = new MutationSettings();

        // Assert - Verifies initial state coverage
        Assert.Multiple(() =>
        {
            Assert.That(settings.SolutionPath, Is.EqualTo(""));
            Assert.That(settings.SingleMutantPerLine, Is.True);
            Assert.That(settings.TestRunTimeout, Is.EqualTo(1200));
            Assert.That(settings.DefaultGitComparisonBranch, Is.EqualTo("master"));
            Assert.That(settings.TestProjects, Is.Not.Null);
            Assert.That(settings.DisabledMutationTypes, Is.Not.Null);
        });
    }
}