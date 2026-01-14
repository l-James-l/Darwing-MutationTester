using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Events;
using Mutator;
using NSubstitute;
using System.Diagnostics;

namespace CoreTests;

public class InitialTestRunnerTests
{
    private InitialTestRunnner _runner; // SUT

    private IEventAggregator _eventAggregator;
    private IMutationSettings _mutationSettings;
    private IWasBuildSuccessfull _buildSuccessfull;
    private IProcessWrapperFactory _processWrapperFactory;
    private IMutationRunInitiator _mutationRunManager;

    private InitiateTestRunEvent _initiateTestRunEvent;
    private InitialTestRunCompleteEvent _initiateTestRunCompleteEvent;

    private Action _initTestRunSubCallBack;

    [SetUp]
    public void SetUp()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _buildSuccessfull = Substitute.For<IWasBuildSuccessfull>();
        _processWrapperFactory = Substitute.For<IProcessWrapperFactory>();
        _mutationRunManager = Substitute.For<IMutationRunInitiator>();

        _runner = new InitialTestRunnner(_eventAggregator, _mutationSettings, _buildSuccessfull, _processWrapperFactory, _mutationRunManager);

        _initiateTestRunEvent = Substitute.For<InitiateTestRunEvent>();
        _initiateTestRunCompleteEvent = Substitute.For<InitialTestRunCompleteEvent>();
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Returns(_initiateTestRunEvent);
        _eventAggregator.GetEvent<InitialTestRunCompleteEvent>().Returns(_initiateTestRunCompleteEvent);

        _initiateTestRunEvent.When(x => x.Subscribe(Arg.Any<Action>(), Arg.Any<ThreadOption>(), Arg.Any<bool>()))
            .Do(x => _initTestRunSubCallBack = x.Arg<Action>());
    }

    private void InitiateRunner()
    {
        _runner.StartUp();
        if (_initTestRunSubCallBack == null)
        {
            Assert.Fail($"{nameof(InitiateTestRunEvent)} subscription not made");
        }
    }

    [Test]
    public void WhenStartUp_ThenTestrunEventSubscribed()
    {
        //Arrange
        //Act
        _runner.StartUp();

        //Assert
        _initiateTestRunEvent.Received(1).Subscribe(Arg.Any<Action>(), ThreadOption.BackgroundThread, true);
        Assert.That(_initTestRunSubCallBack, Is.Not.Null);
    }

    [Test]
    public void GivenLastBuildNotSuccessful_WhenPublishEvent_ThenNoProcessStarted()
    {
        //Arrange
        _buildSuccessfull.WasLastBuildSuccessful.Returns(false);
        InitiateRunner();

        //Act
        _initTestRunSubCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }


    [Test]
    public void GivenSuccessfulBuild_WhenPublishEvent_ThenTestRunStarted()
    {
        //Arrange
        _buildSuccessfull.WasLastBuildSuccessful.Returns(true);
        _mutationSettings.SolutionPath.Returns("this/is/the/path/to/solution.sln");
        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments == "test solution.sln --no-build")).Returns(testProcess);
        testProcess.StartAndAwait(null).Returns(true);
        testProcess.Success.Returns(true);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);
        testProcess.Duration.Returns(TimeSpan.FromSeconds(30));

        InitiateRunner();

        //Act
        _initTestRunSubCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "test solution.sln --no-build" &&
            x.WorkingDirectory == "this\\is\\the\\path\\to" &&
            x.RedirectStandardError && x.RedirectStandardOutput));
        testProcess.Received(1).StartAndAwait(null);
        _mutationRunManager.Received(1).Run(Arg.Is<InitialTestRunInfo>(x => x.WasSuccesful));
        _initiateTestRunCompleteEvent.Received(1).Publish(Arg.Is<InitialTestRunInfo>(x => x.WasSuccesful && x.InitialRunDuration.Seconds == 30));
    }

    [Test]
    public void GivenSuccessfulBuild_WhenPublishEvent_ThenTestRunStarted_AndTestRunFails_ThenResultAvailable()
    {
        //Arrange
        _buildSuccessfull.WasLastBuildSuccessful.Returns(true);
        _mutationSettings.SolutionPath.Returns("this/is/the/path/to/solution.sln");
        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments == "test solution.sln --no-build")).Returns(testProcess);
        testProcess.StartAndAwait(null).Returns(true);
        testProcess.Success.Returns(false);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);

        InitiateRunner();

        //Act
        _initTestRunSubCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "test solution.sln --no-build" &&
            x.WorkingDirectory == "this\\is\\the\\path\\to" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));
        testProcess.Received(1).StartAndAwait(null);
        _mutationRunManager.DidNotReceiveWithAnyArgs().Run(default!);
    }

    //TODO: see if I can add tests for the file/ syntax tree discovery section
}
