using CLI;
using Core.IndustrialEstate;
using Models;
using Models.Events;
using NSubstitute;

namespace CLITests;

public class CLIAppTests
{
    private CLIApp _app; //SUT

    private IEventAggregator _eventAggregator;
    private IMutationSettings _mutationSettings;
    private ISolutionProvider _solutionProvider;
    private ICancelationTokenFactory _cancelationTokenFactory;
    private IWasBuildSuccessfull _buildSuccess;

    private TextReader _originalIn;
    private ICancellationTokenWrapper _cancelationtoken;
    private SolutionPathProvidedEvent _solutionPathProvided;
    private SolutionLoadedEvent _requestSolutionBuildEvent;
    private InitiateTestRunEvent _initiateTestRunEvent;

    [SetUp]
    public void SetUp()
    {
        _originalIn = Console.In;
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _cancelationTokenFactory = Substitute.For<ICancelationTokenFactory>();
        _buildSuccess = Substitute.For<IWasBuildSuccessfull>();

        _solutionPathProvided = Substitute.For<SolutionPathProvidedEvent>();
        _requestSolutionBuildEvent = Substitute.For<SolutionLoadedEvent>();
        _initiateTestRunEvent = Substitute.For<InitiateTestRunEvent>();

        _cancelationtoken = Substitute.For<ICancellationTokenWrapper>();

        _solutionProvider.IsAvailable.Returns(false);
        _cancelationTokenFactory.Generate().Returns(_cancelationtoken);

        _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Returns(_solutionPathProvided);
        _eventAggregator.GetEvent<SolutionLoadedEvent>().Returns(_requestSolutionBuildEvent);
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Returns(_initiateTestRunEvent);

        _app = new CLIApp(_eventAggregator, _mutationSettings, _solutionProvider, _cancelationTokenFactory, _buildSuccess);

        //Limit testing to a single run through.
        Queue<bool> ensureSingleRunQueue = new();
        ensureSingleRunQueue.Enqueue(false);
        ensureSingleRunQueue.Enqueue(true);

        _cancelationtoken.IsCancelled().Returns(_ => ensureSingleRunQueue.Dequeue());
    }

    [TearDown]
    public void TearDown()
    {
        Console.SetIn(_originalIn);
    }

    [Test]
    public void GivenUserProvidesSolutionPath_WhenRun_ThenPublishesThatPath()
    {
        // Arrange
        const string providedPath = "C:\\temp\\MySolution.sln";
        Console.SetIn(new StringReader("--load " + providedPath + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _solutionPathProvided.Received(1).Publish(Arg.Is<SolutionPathProvidedPayload>(x => x.SolutionPath == providedPath));
    }

    [Test]
    public void GivenLaunchSettingPathAndNouserInput_WhenRun_ThenDoesNotPublish()
    {
        // Arrange
        Console.SetIn(new StringReader(Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _solutionPathProvided.Received(0).Publish(Arg.Any<SolutionPathProvidedPayload>());
    }

    [Test]
    public void GivenSlnPathInArgs_WhenRun_ThenPublishesThatPath()
    {
        // Arrange
        const string argPath = "C:\\temp\\ArgSolution.sln";
        var args = new[] { "--sln", argPath };
        Console.SetIn(new StringReader(Environment.NewLine));

        // Act
        _app.Run(args);
        
        // Assert
        _solutionPathProvided.Received(1).Publish(Arg.Is<SolutionPathProvidedPayload>(x => x.SolutionPath == argPath));
    }

    [Test]
    public void GivenRunningMainLoop_AndNoSolutionLoaded_WhenGiveBuildCommand_ThenDoesntPublish()
    {
        // Arrange
        _solutionProvider.IsAvailable.Returns(false);
        
        Console.SetIn(new StringReader("--build" + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _requestSolutionBuildEvent.Received(0).Publish();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionLoaded_WhenGiveBuildCommand_ThenPublishCommand()
    {
        // Arrange
        _solutionProvider.IsAvailable.Returns(true);

        Console.SetIn(new StringReader("--build" + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _requestSolutionBuildEvent.Received(1).Publish();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionLoaded_AndBuildSuccess_WhenGiveTestCommand_ThenPublishCommand()
    {
        // Arrange
        _solutionProvider.IsAvailable.Returns(true);
        _buildSuccess.WasLastBuildSuccessful.Returns(true);

        Console.SetIn(new StringReader("--test" + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _initiateTestRunEvent.Received(1).Publish();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionNotLoaded_WhenGiveTestCommand_ThenDontPublishCommand()
    {
        // Arrange
        _solutionProvider.IsAvailable.Returns(false);
        _buildSuccess.WasLastBuildSuccessful.Returns(true);

        Console.SetIn(new StringReader("--test" + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _initiateTestRunEvent.Received(0).Publish();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionLoaded_AndBuildFailed_WhenGiveTestCommand_ThenDontPublishCommand()
    {
        // Arrange
        _solutionProvider.IsAvailable.Returns(true);
        _buildSuccess.WasLastBuildSuccessful.Returns(false);

        Console.SetIn(new StringReader("--test" + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _initiateTestRunEvent.Received(0).Publish();
    }

}
