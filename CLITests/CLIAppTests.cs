using Core;
using Models.Events;
using NSubstitute;

namespace CLITests;

public class CLIAppTests
{
    private CLIApp _app; //SUT

    private TextReader _originalIn;
    private IEventAggregator _eventAggregator;
    private SolutionPathProvided _solutionPathProvided;

    [SetUp]
    public void SetUp()
    {
        _originalIn = Console.In;
        _eventAggregator = Substitute.For<IEventAggregator>();

        _solutionPathProvided = Substitute.For<SolutionPathProvided>();

        _eventAggregator.GetEvent<SolutionPathProvided>().Returns(_solutionPathProvided);

        _app = new CLIApp(_eventAggregator);
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
        Console.SetIn(new StringReader(providedPath + Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _solutionPathProvided.Received(1).Publish(Arg.Is<SolutionPathProvidedPayload>(x => x.SolutionPath == providedPath));
    }

    [Test]
    public void GivenNoInputAndDevArg_WhenRun_ThenPublishesDevPath()
    {
        // Arrange
        // Simulate user pressing enter (empty input)
        Console.SetIn(new StringReader(Environment.NewLine));

        var args = new[] { "--dev" };

        // Act
        _app.Run(args);

        // Assert
        _solutionPathProvided.Received(1)
            .Publish(Arg.Is<SolutionPathProvidedPayload>(x => x.SolutionPath == @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln"));
    }

    [Test]
    public void GivenNoInputAndNoDevArg_WhenRun_ThenPublishesEmptyPath()
    {
        // Arrange
        Console.SetIn(new StringReader(Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

        // Assert
        _solutionPathProvided.Received(1).Publish(Arg.Is<SolutionPathProvidedPayload>(x => x.SolutionPath == string.Empty));
    }
}
