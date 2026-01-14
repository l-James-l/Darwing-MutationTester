using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Events;
using NSubstitute;
using System.Diagnostics;

namespace CoreTests;

public class ProjectBuilderTests
{
    private ProjectBuilder _projectBuilder; //SUT

    private IMutationSettings _mutationSettings;
    private IEventAggregator _eventAggregator;
    private ISolutionProvider _solutionProvider;
    private IProcessWrapperFactory _processWrapperFactory;

    private RequestSolutionBuildEvent _buildEvent;
    private Action _buildEventCallBack;

    [SetUp]
    public void SetUp()
    {
        _mutationSettings = Substitute.For<IMutationSettings>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _processWrapperFactory = Substitute.For<IProcessWrapperFactory>();

        _projectBuilder = new ProjectBuilder(_mutationSettings, _eventAggregator, _solutionProvider, _processWrapperFactory);

        _buildEvent = Substitute.For<RequestSolutionBuildEvent>();
        _eventAggregator.GetEvent<RequestSolutionBuildEvent>().Returns(_buildEvent);

        _buildEvent.When(x => x.Subscribe(Arg.Any<Action>(), Arg.Any<ThreadOption>(), Arg.Any<bool>()))
            .Do(x => _buildEventCallBack = x.Arg<Action>());
    }

    [Test]
    public void WhenStrartUp_ThenSubscribesToEvent()
    {
        //Arrange
        //Act
        _projectBuilder.StartUp();

        //Assert
        _buildEvent.Received(1).Subscribe(Arg.Any<Action>(), ThreadOption.BackgroundThread, true);
        Assert.That(_buildEventCallBack, Is.Not.Null);
    }

    [Test]
    public void GivenNoSolutionLoaded_WhenRecievesBuildReuest_ThenNoProcessesStarted()
    {
        //Arrange
        _solutionProvider.IsAvailable.Returns(false);
        _projectBuilder.StartUp();

        //Act
        _buildEventCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }

    [Test]
    public void GivenSolutionLoadedWithNoProjects_WhenRecievesBuildReuest_ThenNoProcessesStartedAndNoErrors()
    {
        //Arrange
        _solutionProvider.IsAvailable.Returns(true);
        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        _solutionProvider.SolutionContiner.Returns(solutionContainer);
        solutionContainer.AllProjects.Returns(new List<IProjectContainer>());
        _projectBuilder.StartUp();

        //Act
        _buildEventCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }

    [Test]
    public void GivenSolutionLoadedWithProjects_WhenRecievesBuildReuest_ThenProcessesStartedForEachProject()
    {
        //Arrange
        _solutionProvider.IsAvailable.Returns(true);
        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        _solutionProvider.SolutionContiner.Returns(solutionContainer);

        IProjectContainer project1 = Substitute.For<IProjectContainer>();
        project1.Name.Returns("Project1");
        project1.CsprojFilePath.Returns("thi/is/a/path/project1.csproj");
        IProjectContainer project2 = Substitute.For<IProjectContainer>();
        project2.Name.Returns("Project2");
        project2.CsprojFilePath.Returns("thi/is/a/path/project2.csproj");

        solutionContainer.AllProjects.Returns(new List<IProjectContainer>
        {
            project1, project2
        });

        IProcessWrapper process1 = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("project1"))).Returns(process1);
        process1.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process1.Success.Returns(true);

        IProcessWrapper process2 = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("project2"))).Returns(process2);
        process2.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process2.Success.Returns(true);

        _projectBuilder.StartUp();

        //Act
        _buildEventCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(2).Create(Arg.Any<ProcessStartInfo>()); 
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x => 
            x.FileName == "dotnet" && 
            x.Arguments == "build project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "build project2.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));
        process1.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        process2.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        Assert.That(_projectBuilder.WasLastBuildSuccessful, Is.True);
    }

    [Test]
    public void GivenSolutionLoadedWithProjects_AndBuildFails_WhenRecievesBuildReuest_ThenCleanAndRestoreProcessStarted()
    {
        //Arrange
        _solutionProvider.IsAvailable.Returns(true);
        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        _solutionProvider.SolutionContiner.Returns(solutionContainer);

        IProjectContainer project1 = Substitute.For<IProjectContainer>();
        project1.Name.Returns("Project1");
        project1.CsprojFilePath.Returns("thi/is/a/path/project1.csproj");

        solutionContainer.AllProjects.Returns(new List<IProjectContainer>
        {
            project1
        });

        IProcessWrapper process1 = Substitute.For<IProcessWrapper>();
        IProcessWrapper process2 = Substitute.For<IProcessWrapper>();
        IProcessWrapper process3 = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("build"))).Returns(process1);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("clean"))).Returns(process2);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("restore"))).Returns(process3);
        process1.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process2.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process3.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process1.Success.Returns(false);
        process2.Success.Returns(true);
        process3.Success.Returns(true);
        process1.Output.Returns([]);
        process2.Output.Returns([]);
        process3.Output.Returns([]);
        process1.Errors.Returns([]);
        process2.Errors.Returns([]);
        process3.Errors.Returns([]);

        _projectBuilder.StartUp();

        //Act
        _buildEventCallBack.Invoke();

        //Assert
        _processWrapperFactory.Received(4).Create(Arg.Any<ProcessStartInfo>());
        _processWrapperFactory.Received(2).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "build project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "clean project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "restore project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        process1.Received(2).StartAndAwait(Arg.Any<TimeSpan>());
        process2.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        process3.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        Assert.That(_projectBuilder.WasLastBuildSuccessful, Is.False);
    }
}
