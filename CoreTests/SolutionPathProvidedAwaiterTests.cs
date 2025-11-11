using Buildalyzer;
using Core;
using Core.IndustrialEstate;
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

    private SolutionPathProvided _solutionPathProvided;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _analyzerManagerFactory = Substitute.For<IAnalyzerManagerFactory>();

        _solutionPathProvided = new SolutionPathProvided();

        _eventAggregator.GetEvent<SolutionPathProvided>().Returns(_solutionPathProvided);

        _awaiter = new SolutionPathProvidedAwaiter(_eventAggregator, _analyzerManagerFactory);
    }

    [Test]
    public void WhenOnSolutionPathProvidedWithValidPath_ThenCreateSolutionContainer()
    {
        // Arrange
        var mockAnalyzerManager = Substitute.For<IAnalyzerManager>();

        _analyzerManagerFactory.CreateAnalyzerManager(Arg.Any<string>()).Returns(mockAnalyzerManager);
        _awaiter.StartUp();

        // Act
        _eventAggregator.GetEvent<SolutionPathProvided>().Publish(new SolutionPathProvidedPayload(@"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln"));

        // Assert
        //_eventAggregator.Received(2).GetEvent<SolutionPathProvided>();
        _analyzerManagerFactory.Received(1).CreateAnalyzerManager(Arg.Any<string>()/*@"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln"*/);
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
