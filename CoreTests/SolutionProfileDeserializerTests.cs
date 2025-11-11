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
    private IEventAggregator _eventAggregator;

    [SetUp]
    public void SetUP()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationSettings = Substitute.For<IMutationSettings>();

        _eventAggregator.GetEvent<SolutionPathProvided>().Returns(new SolutionPathProvided());

        _solutionProfileDeserializer = new SolutionProfileDeserializer(_eventAggregator, _mutationSettings);

        _solutionProfileDeserializer.StartUp();

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();
    }

    [Test]
    public void GivenNoProfileFile_WhenTryFind_ThenNoActionTaken()
    {
        //Arrange
        //Act
        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload("./"));

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
        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload(""));

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
        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload("./TestData/example.sln"));

        //Assert
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x =>
            x.MessageTemplate.Text == "Successfully loaded solution profile data." && x.Level == LogEventLevel.Information),
            Is.Not.Null);

        Assert.That(_mutationSettings.TestProjectNames, Does.Contain("Project1").And.Contain("Project2"));
        Assert.That(_mutationSettings.SolutionProfileData, Is.Not.Null);
        
        Assert.That(_mutationSettings.SolutionProfileData.ProjectsToMutate, Does.Contain("MutateMe"));
        
        Assert.That(_mutationSettings.SolutionProfileData.SpecificMutations, Does.ContainKey(SpecifcMutation.SubtractToAdd));
        Assert.That(_mutationSettings.SolutionProfileData.SpecificMutations[SpecifcMutation.SubtractToAdd], Is.False);
        Assert.That(_mutationSettings.SolutionProfileData.SpecificMutations, Does.ContainKey(SpecifcMutation.AddToSubtract));
        Assert.That(_mutationSettings.SolutionProfileData.SpecificMutations[SpecifcMutation.AddToSubtract], Is.True);

        Assert.That(_mutationSettings.SolutionProfileData.MutationCategories, Does.ContainKey(MutationCategory.Arithmetic));
        Assert.That(_mutationSettings.SolutionProfileData.MutationCategories[MutationCategory.Arithmetic], Is.True);
        Assert.That(_mutationSettings.SolutionProfileData.MutationCategories, Does.ContainKey(MutationCategory.Logical));
        Assert.That(_mutationSettings.SolutionProfileData.MutationCategories[MutationCategory.Logical], Is.True);

        Assert.That(_mutationSettings.SolutionProfileData.GeneralSettings.SingleMutantPerLine, Is.False);
    }
}
