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
    private SolutionLoader _awaiter; //SUT

    private IEventAggregator _eventAggregator;
    private IAnalyzerManagerFactory _analyzerManagerFactory;
    private ISolutionProfileDeserializer _slnProfileDeserializer;
    private IMutationSettings _mutationSettings;

    private SolutionPathProvidedEvent _solutionPathProvided;
    private SolutionLoadedEvent _requestSolutionBuild;

    private Action<SolutionPathProvidedPayload> _requestSolutionBuildCallback;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _analyzerManagerFactory = Substitute.For<IAnalyzerManagerFactory>();
        _slnProfileDeserializer = Substitute.For<ISolutionProfileDeserializer>();
        _mutationSettings = Substitute.For<IMutationSettings>();

        _solutionPathProvided = Substitute.For<SolutionPathProvidedEvent>();
        _requestSolutionBuild = new SolutionLoadedEvent();

        _solutionPathProvided.When(x => x.Subscribe(Arg.Any<Action<SolutionPathProvidedPayload>>(), Arg.Any<ThreadOption>(), Arg.Any<bool>()))
            .Do(x => _requestSolutionBuildCallback = x.Arg<Action<SolutionPathProvidedPayload>>());

        _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Returns(_solutionPathProvided);
        _eventAggregator.GetEvent<SolutionLoadedEvent>().Returns(_requestSolutionBuild);

        _awaiter = new SolutionLoader(_eventAggregator, _analyzerManagerFactory, _slnProfileDeserializer, _mutationSettings);
    }

    [Test, Explicit("Fails on build sever due to local path. TODO to fix")]
    public void WhenOnSolutionPathProvidedWithValidPath_ThenCreateSolutionContainer()
    {
        // Arrange
        _analyzerManagerFactory.CreateAnalyzerManager(Arg.Any<string>()).Returns(Substitute.For<IAnalyzerManager>());
        _awaiter.StartUp();

        //TODO this would fail on a build server
        const string SolutionPath = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";

        // Act
        _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Publish(new SolutionPathProvidedPayload(SolutionPath));

        // Assert
        _analyzerManagerFactory.Received(1).CreateAnalyzerManager(Arg.Is<string>(x => x == SolutionPath));
        _requestSolutionBuild.Received(1).Publish();
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
        _requestSolutionBuildCallback.Invoke(new SolutionPathProvidedPayload(path));

        // Assert
        _analyzerManagerFactory.DidNotReceive().CreateAnalyzerManager(Arg.Any<string>());
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(e =>
            e.Level == Serilog.Events.LogEventLevel.Error &&
            e.MessageTemplate.Text.Contains($"Solution file not found at location: {path}")),
            Is.Not.Null);
    }
}
