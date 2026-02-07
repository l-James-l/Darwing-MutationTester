using Core;
using Models;
using Models.Enums;
using Models.Events;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

namespace CoreTests;

public class SolutionProfileDeserializerTests
{
    private SolutionProfileDeserializer _solutionProfileDeserializer; // SUT

    private IMutationSettings _mutationSettings;

    [SetUp]
    public void SetUP()
    {
        _mutationSettings = Substitute.For<IMutationSettings>();


        _solutionProfileDeserializer = new SolutionProfileDeserializer(_mutationSettings);

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();
    }

    [Test]
    public void GivenNoProfileFile_WhenTryFind_ThenNoActionTaken()
    {
        //Arrange
        //Act
        _solutionProfileDeserializer.LoadSlnProfileIfPresent("./");

        //Assert
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x =>
            x.MessageTemplate.Text == "No solution profile file found in solution directory." && x.Level == LogEventLevel.Information),
            Is.Not.Null);
    }

    [Test]
    public void GivenPathNotValidPathSoCannotExtractDirectory_WhenTryFind_ThenNoActionTaken()
    {
        //Arrange
        //Act
        _solutionProfileDeserializer.LoadSlnProfileIfPresent("");

        //Assert
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x =>
            x.MessageTemplate.Text == "Failed to determine solution directory from solution file path." && x.Level == LogEventLevel.Error),
            Is.Not.Null);
    }

    [Test]
    public void GivenProfileFileIsPresent_WhenTryFind_ThenDeserialzedAndPropertiesAssigned()
    {
        //Arrange
        //Act
        _solutionProfileDeserializer.LoadSlnProfileIfPresent("./TestData/example.sln");

        //Assert
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x =>
            x.MessageTemplate.Text == "Successfully loaded solution profile data." && x.Level == LogEventLevel.Information),
            Is.Not.Null);

        Assert.That(_mutationSettings.TestProjects, Does.Contain("Project1").And.Contain("Project2"));
        
        Assert.That(_mutationSettings.IgnoreProjects, Does.Contain("MutateMe"));
        
        Assert.That(_mutationSettings.DisabledMutationTypes, Does.Contain(SpecifcMutation.SubtractToAdd));

        Assert.That(_mutationSettings.SingleMutantPerLine, Is.False);
    }
}
