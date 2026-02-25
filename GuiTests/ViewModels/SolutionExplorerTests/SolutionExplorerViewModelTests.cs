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
    private IGeminiApiHandler _geminiApiHandler;

    private const string TestFilePath = "ViewModels\\SolutionExplorerTests\\TestData\\TestContentCodeFile.txt";

    private Action? _gitCallback;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _gitDiffManager = Substitute.For<IGitDiffManager>();
        _geminiApiHandler = Substitute.For<IGeminiApiHandler>();

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
        
        _solutionExplorer = new SolutionExplorerViewModel(_fileExplorerViewModel, _eventAggregator, _solutionProvider, _gitDiffManager, _geminiApiHandler);
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

    [Test]
    public void WhenGenerateUnitTestCommand_ThenGeminiApiInvoked()
    {
        //Arrange
        DiscoveredMutation m = new(new SyntaxAnnotation(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator, 0, 0);
        MutationViewModel mutationVm = new MutationViewModel(m);
        TaskCompletionSource asyncTaskSource = new();
        _geminiApiHandler.GenerateUnitTest(Arg.Any<DiscoveredMutation>(), Arg.Any<Action<string, string>>()).Returns(asyncTaskSource.Task);

        //Act
        _solutionExplorer.TryGetUnitTestCommand.Execute(mutationVm);

        //Assert
        _geminiApiHandler.Received(1).GenerateUnitTest(Arg.Is<DiscoveredMutation>(x => x == m), Arg.Any<Action<string, string>>());
        Assert.That(mutationVm.TestGenerationOngoingVisibility, Is.EqualTo(Visibility.Visible));

        //Act - complete task
        asyncTaskSource.SetResult();

        //Assert
        Assert.That(mutationVm.TestGenerationOngoingVisibility, Is.EqualTo(Visibility.Collapsed));
    }

    [Test]
    public void GivenFileWithMutatedTreeSelected_WhenShowMutatedTreeTrue_ShowsMutatedFile()
    {
        //Arrange
        SourceCodeFileContainer file = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("public void UnmutatedTree() \n {}"));
        file.MutatedTree = CSharpSyntaxTree.ParseText("public void MutatedTree() {}");
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<string>(), ProjectType.Source).Returns(file);

        _fileExplorerViewModel.SelectedFile = new FileNode(file, _fileExplorerViewModel);

        //Act
        _solutionExplorer.ShowFullMutatedFile = true;

        //Assert
        Assert.That(_solutionExplorer.FileDetails.Count, Is.EqualTo(1)); 
        Assert.That(_solutionExplorer.FileDetails[0].SourceCode, Is.EqualTo("public void MutatedTree() {}"));
    }

    [Test]
    public void GivenFileWithNoMutatedTreeSelected_WhenShowMutatedTreeTrue_ShowsNormalFile()
    {
        //Arrange
        SourceCodeFileContainer file = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("public void UnmutatedTree() \n {}"));
        file.MutatedTree = null;
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<string>(), ProjectType.Source).Returns(file);

        _fileExplorerViewModel.SelectedFile = new FileNode(file, _fileExplorerViewModel);

        //Act
        _solutionExplorer.ShowFullMutatedFile = true;

        //Assert
        Assert.That(_solutionExplorer.FileDetails.Count, Is.EqualTo(2)); 
        Assert.That(_solutionExplorer.FileDetails[0].SourceCode, Is.EqualTo("public void UnmutatedTree() "));
        Assert.That(_solutionExplorer.FileDetails[1].SourceCode, Is.EqualTo(" {}"));
    }

    [Test]
    public void GivenNoFileSelected_WhenShowMutatedTreeSelected_ShowsNothing()
    {
        //Act
        _solutionExplorer.ShowFullMutatedFile = true;

        //Assert
        Assert.That(_solutionExplorer.FileDetails.Count, Is.Zero);
        Assert.That(_solutionExplorer.SelectedLine, Is.Null);
        Assert.That(_solutionExplorer.SelectedFileHeader, Is.EqualTo("No File Selected"));

    }
}
