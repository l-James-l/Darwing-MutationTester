using Buildalyzer;
using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Events;
using NSubstitute;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace CoreTests;

public class SolutionPathProvidedAwaiterTests
{
    private SolutionPathProvidedAwaiter _awaiter; //SUT

    private IEventAggregator _eventAggregator;
    private IAnalyzerManagerFactory _analyzerManagerFactory;
    private ISolutionProfileDeserializer _slnProfileDeserializer;
    private IMutationSettings _mutationSettings;
    private IProjectBuilder _projectBuilder;

    private SolutionPathProvided _solutionPathProvided;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _analyzerManagerFactory = Substitute.For<IAnalyzerManagerFactory>();
        _slnProfileDeserializer = Substitute.For<ISolutionProfileDeserializer>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _projectBuilder = Substitute.For<IProjectBuilder>();

        _solutionPathProvided = new SolutionPathProvided();

        _eventAggregator.GetEvent<SolutionPathProvided>().Returns(_solutionPathProvided);

        _awaiter = new SolutionPathProvidedAwaiter(_eventAggregator, _analyzerManagerFactory, _slnProfileDeserializer, _mutationSettings, _projectBuilder);
    }

    [Test, Explicit("Fails on build sever due to local path. TODO to fix")]
    public void WhenOnSolutionPathProvidedWithValidPath_ThenCreateSolutionContainer()
    {
        // Arrange
        var mockAnalyzerManager = Substitute.For<IAnalyzerManager>();

        _analyzerManagerFactory.CreateAnalyzerManager(Arg.Any<string>()).Returns(mockAnalyzerManager);
        _awaiter.StartUp();

        //TODO this would fail on a build server
        const string SolutionPath = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";

        // Act
        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload(SolutionPath));

        // Assert
        _analyzerManagerFactory.Received(1).CreateAnalyzerManager(Arg.Is<string>(x => x == SolutionPath));
        _projectBuilder.Received(1).InitialBuild(Arg.Is<SolutionContainer>(x => x.AnalyzerManager == mockAnalyzerManager));
        _slnProfileDeserializer.Received(1).LoadSlnProfileIfPresent(SolutionPath);
    }

    [Test]
    public void WhenOnSolutionPathProvidedWithInvalidPath_ThenLogErrorAndDoNotCreateSolutionContainer()
    {
        // Arrange
        _awaiter.StartUp();
        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        string path = @"C:\InvalidPath\NonExistentSolution.sln";

        // Act
        _solutionPathProvided.Publish(new SolutionPathProvidedPayload(path));

        // Assert
        _analyzerManagerFactory.DidNotReceive().CreateAnalyzerManager(Arg.Any<string>());
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(e =>
            e.Level == Serilog.Events.LogEventLevel.Error &&
            e.MessageTemplate.Text.Contains($"Solution file not found at location: {path}")),
            Is.Not.Null);
    }
}
