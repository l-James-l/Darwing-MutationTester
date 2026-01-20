using Buildalyzer;
using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using NSubstitute;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace CoreTests;

public class SolutionLoaderTests
{
    private SolutionLoader _slnLoader; //SUT

    private IAnalyzerManagerFactory _analyzerManagerFactory;
    private ISolutionProfileDeserializer _slnProfileDeserializer;
    private IMutationSettings _mutationSettings;
    private IStatusTracker _statusTracker;
    private ISolutionProvider _slnProvider;
    private ISolutionBuilder _solutionBuilder;

    [SetUp]
    public void Setup()
    {
        _analyzerManagerFactory = Substitute.For<IAnalyzerManagerFactory>();
        _slnProfileDeserializer = Substitute.For<ISolutionProfileDeserializer>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _statusTracker = Substitute.For<IStatusTracker>();
        _slnProvider = Substitute.For<ISolutionProvider>();
        _solutionBuilder = Substitute.For<ISolutionBuilder>();

        _slnLoader = new SolutionLoader(_analyzerManagerFactory, _slnProfileDeserializer, _mutationSettings, _solutionBuilder, _statusTracker, _slnProvider);
    }

    [Test]
    public void GivenStatusTrackerSaysNo_WhenLoad_ThenDoNotLoadSolution()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.LoadSolution).Returns(false);
        
        // Act
        _slnLoader.Load("AnyPath.sln");
        
        // Assert
        _analyzerManagerFactory.DidNotReceive().CreateAnalyzerManager(Arg.Any<string>());
        _slnProfileDeserializer.DidNotReceive().LoadSlnProfileIfPresent(Arg.Any<string>());
        _solutionBuilder.DidNotReceive().InitialBuild();
        _slnProvider.DidNotReceive().NewSolution(Arg.Any<SolutionContainer>());
        _statusTracker.Received(0).FinishOperation(DarwingOperation.LoadSolution, Arg.Any<bool>());
    }

    /// <summary>
    /// Find a way to make this test not depend on a local path.
    /// </summary>
    [Test, Explicit("Fails on build sever due to local path.")]
    public void WhenOnSolutionPathProvidedWithValidPath_ThenCreateSolutionContainer()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.LoadSolution).Returns(true);
        _analyzerManagerFactory.CreateAnalyzerManager(Arg.Any<string>()).Returns(Substitute.For<IAnalyzerManager>());

        //TODO this would fail on a build server
        const string SolutionPath = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";

        // Act
        _slnLoader.Load(SolutionPath);

        // Assert
        _analyzerManagerFactory.Received(1).CreateAnalyzerManager(Arg.Is<string>(x => x == SolutionPath));
        _slnProfileDeserializer.Received(1).LoadSlnProfileIfPresent(SolutionPath);
        _solutionBuilder.Received(1).InitialBuild();
        _slnProvider.Received(1).NewSolution(Arg.Any<SolutionContainer>());
        _statusTracker.Received(1).FinishOperation(DarwingOperation.LoadSolution, true);
    }

    [Test]
    public void WhenOnSolutionPathProvidedWithInvalidPath_ThenLogErrorAndDoNotCreateSolutionContainer()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.LoadSolution).Returns(true);
        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        string path = @"C:\InvalidPath\NonExistentSolution.sln";

        // Act
        _slnLoader.Load(path);

        // Assert
        _analyzerManagerFactory.DidNotReceive().CreateAnalyzerManager(Arg.Any<string>());
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(e =>
            e.Level == Serilog.Events.LogEventLevel.Error &&
            e.MessageTemplate.Text.Contains($"Solution file not found at location: {path}")),
            Is.Not.Null);
        _slnProvider.DidNotReceive().NewSolution(Arg.Any<SolutionContainer>());
        _statusTracker.Received(1).FinishOperation(DarwingOperation.LoadSolution, false);
    }

    //TODO: see if I can add tests for the file/ syntax tree discovery section
}
