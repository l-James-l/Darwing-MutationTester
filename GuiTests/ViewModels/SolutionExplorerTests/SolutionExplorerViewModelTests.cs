using Core.Interfaces;
using GUI.ViewModels;
using GUI.ViewModels.SolutionExplorerElements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.Events;
using Mutator;
using NSubstitute;
using System.Windows;

namespace GuiTests.ViewModels.SolutionExplorerTests;

public class SolutionExplorerViewModelTests
{
    private SolutionExplorerViewModel _solutionExplorer;
    private FileExplorerViewModel _fileExplorerViewModel;

    private ISolutionProvider _solutionProvider;
    private IEventAggregator _eventAggregator;
    private IMutationDiscoveryManager _mutationDiscoveryManager;
    private IGitDiffManager _gitDiffManager;

    private const string TestFilePath = "ViewModels\\SolutionExplorerTests\\TestData\\TestContentCodeFile.txt";

    private Action? _gitCallback;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _gitDiffManager = Substitute.For<IGitDiffManager>();

        GitUpdateEvent gitEvent = Substitute.For<GitUpdateEvent>();
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(Substitute.For<DarwingOperationStatesChangedEvent>());
        _eventAggregator.GetEvent<MutationUpdated>().Returns(Substitute.For<MutationUpdated>());
        _eventAggregator.GetEvent<SettingChanged>().Returns(Substitute.For<SettingChanged>());
        _eventAggregator.GetEvent<GitUpdateEvent>().Returns(gitEvent);

        gitEvent.When(e => e.Subscribe(Arg.Any<Action>(), ThreadOption.UIThread)).Do(callInfo =>
        {
            _gitCallback = callInfo.Arg<Action>();
        });

        _fileExplorerViewModel = new FileExplorerViewModel(_solutionProvider, _eventAggregator, _mutationDiscoveryManager);
        
        _solutionExplorer = new SolutionExplorerViewModel(_fileExplorerViewModel, _eventAggregator, _solutionProvider, _gitDiffManager);
    }

    [Test]
    public void WhenCreated_ThenFileExplorerCallBackSetBySolutionExplorer()
    {
        Assert.That(_fileExplorerViewModel.SelectedFileChangedCallBack, Is.Not.Null);
        Assert.That(_gitCallback, Is.Not.Null);
    }

    [Test]
    public void GivenFileSelected_WhenFileExplorerCallBackInvoked_ThenFileDetailsLoaded()
    {
        //Arrange
        SourceCodeFileContainer file = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(File.ReadAllText(TestFilePath)));
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<string>(), ProjectType.Source).Returns(file);
        //Act
        
        _fileExplorerViewModel.SelectedFile = new FileNode(file, _fileExplorerViewModel);

        //Assert
        Assert.That(_solutionExplorer.FileDetails, Is.Not.Null.Or.Empty);
        Assert.That(_solutionExplorer.FileDetails.Count, Is.EqualTo(63)); //File has 63 lines
        int prevLineNumber = 0;
        foreach (LineDetails line in _solutionExplorer.FileDetails)
        {
            Assert.That(line.LineNumber, Is.EqualTo(++prevLineNumber));
        }
        Assert.That(_solutionExplorer.FileDetails[0].SourceCode, Is.EqualTo("namespace GuiTests.ViewModels.SolutionExplorerTests.TestData;"));
        Assert.That(_solutionExplorer.FileDetails[5].SourceCode, Is.EqualTo("public class TestContentCodeFile"));
        Assert.That(_solutionExplorer.FileDetails[61].SourceCode, Is.EqualTo("}"));
    }

    [Test]
    public void OnGitUpdateEvent_WhenBranchesExist_SetsVisibilityToVisible()
    {
        // Arrange
        _gitDiffManager.Branches.Returns(new List<string> { "main", "feature/test" });

        // Act: Capture the action passed to Subscribe and invoke it manually
        _gitCallback?.Invoke();

        // Assert
        Assert.That(_solutionExplorer.GitVisibility, Is.EqualTo(Visibility.Visible));
        Assert.That(_solutionExplorer.AvailableGitBranches, Does.Contain("Test full solution"));
    }

    [Test]
    public void OnGitUpdateEvent_WhenNoBranches_SetsVisibilityToHidden()
    {
        _gitDiffManager.Branches.Returns(new List<string>());

        _gitCallback?.Invoke();

        Assert.That(_solutionExplorer.GitVisibility, Is.EqualTo(Visibility.Hidden));
    }

    [Test]
    public void SelectedBranch_WhenSetToRealBranch_CallsEstablishDiff()
    {
        // Act
        _solutionExplorer.SelectedBranch = "feature/mutation-fix";

        // Assert
        _gitDiffManager.Received(1).EstablishDiff("feature/mutation-fix");
    }
}
