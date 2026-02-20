using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Mutator;
using Mutator.MutationImplementations;
using NSubstitute;
using System.Diagnostics;

namespace CoreTests;

public class MutatedSolutionTesterTests
{
    private IEventAggregator _eventAggregator;
    private IMutationDiscoveryManager _discoveryManager;
    private IProcessWrapperFactory _processFactory;
    private IMutationSettings _settings;
    private IStatusTracker _statusTracker;
    private IProcessWrapper _mockProcess;
    private MutatedSolutionTester _mutatedSolutionTester;

    [SetUp]
    public void SetUp()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _discoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _processFactory = Substitute.For<IProcessWrapperFactory>();
        _settings = Substitute.For<IMutationSettings>();
        _statusTracker = Substitute.For<IStatusTracker>();
        _mockProcess = Substitute.For<IProcessWrapper>();

        // Default setup: Status tracker allows operation
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SolutionPath.Returns(@"C:\Solution\App.sln");
        _settings.TestRunTimeout.Returns(1000);
        _settings.MutationTestTimeoutScaler.Returns(1.5);
        _processFactory.Create(Arg.Any<ProcessStartInfo>()).Returns(_mockProcess);

        _mutatedSolutionTester = new MutatedSolutionTester(_eventAggregator, _discoveryManager, _processFactory, _settings, _statusTracker);
    }

    [Test]
    public void RunTests_WhenProcessTimesOut_MutantIsKilled()
    {
        // Arrange
        var mutant = CreateAvailableMutant("123");
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });
        _settings.SkipTestingNoActiveMutants.Returns(true); // Skip preliminary run for simplicity

        // StartAndAwait returning false simulates a timeout
        _mockProcess.StartAndAwait(Arg.Any<double>()).Returns(false);
        _mockProcess.Output.Returns([]);

        // Act
        _mutatedSolutionTester.RunTestsOnMutatedSolution();

        // Assert
        Assert.That(mutant.Status, Is.EqualTo(MutantStatus.Killed));
        _statusTracker.Received().FinishOperation(DarwingOperation.TestMutants, true);
    }

    [Test]
    public void RunTests_PassesCorrectMutantIdInEnvironment()
    {
        // Arrange
        var mutant = CreateAvailableMutant("ID_999");
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });
        _settings.SkipTestingNoActiveMutants.Returns(true);
        _mockProcess.StartAndAwait(Arg.Any<double>()).Returns(true);
        _mockProcess.Success.Returns(true); // Process ran but mutant survived

        // Act
        _mutatedSolutionTester.RunTestsOnMutatedSolution();

        // Assert
        _processFactory.Received().Create(Arg.Is<ProcessStartInfo>(info =>
            info.EnvironmentVariables[Annotations.ActiveMutationIndex] == "ID_999"
        ));
    }

    [Test]
    public void RunTests_ScalesTimeoutBasedOnInitialRun()
    {
        // Arrange
        // Simulate receipt of InitialTestRunCompleteEvent
        var initialInfo = new InitialTestRunInfo { InitialRunDuration = TimeSpan.FromSeconds(100) };
        var testInfoEvent = Substitute.For<InitialTestRunCompleteEvent>();
        _eventAggregator.GetEvent<InitialTestRunCompleteEvent>().Returns(testInfoEvent);

        // Capture the subscription and invoke it
        testInfoEvent.When(x => x.Subscribe(Arg.Any<Action<InitialTestRunInfo>>(),
            Arg.Any<ThreadOption>())).Do(x => x.Arg<Action<InitialTestRunInfo>>().Invoke(initialInfo));

        _mutatedSolutionTester.StartUp();
        var mutant = CreateAvailableMutant("1");
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });
        _settings.SkipTestingNoActiveMutants.Returns(true);
        _settings.MutationTestTimeoutScaler.Returns(2.0);

        // Act
        _mutatedSolutionTester.RunTestsOnMutatedSolution();

        // Assert: 100ms * 2.0 scaler = 200ms
        _mockProcess.Received().StartAndAwait(Arg.Is<TimeSpan>(x => x.TotalSeconds == 200));
    }

    private DiscoveredMutation CreateAvailableMutant(string id)
    {
        // Mocking the SyntaxNode parts might be needed depending on your DiscoveredMutation constructor
        return new DiscoveredMutation(new SyntaxAnnotation("key", id), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator, 0, 0)
        {
            Status = MutantStatus.Available,
        };
    }
}