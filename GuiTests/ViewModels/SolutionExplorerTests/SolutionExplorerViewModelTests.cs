using GUI.ViewModels;
using GUI.ViewModels.SolutionExplorerElements;
using Models;
using Models.Events;
using Mutator;
using NSubstitute;

namespace GuiTests.ViewModels.SolutionExplorerTests;

public class SolutionExplorerViewModelTests
{
    private SolutionExplorerViewModel _solutionExplorer;
    private FileExplorerViewModel _fileExplorerViewModel;

    private ISolutionProvider _solutionProvider;
    private IEventAggregator _eventAggregator;
    private IMutationDiscoveryManager _mutationDiscoveryManager;

    private const string TestFilePath = "ViewModels\\SolutionExplorerTests\\TestData\\TestContentCodeFile.txt";

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();

        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(Substitute.For<DarwingOperationStatesChangedEvent>());
        _eventAggregator.GetEvent<MutationUpdated>().Returns(Substitute.For<MutationUpdated>());

        _fileExplorerViewModel = new FileExplorerViewModel(_solutionProvider, _eventAggregator, _mutationDiscoveryManager);
        
        _solutionExplorer = new SolutionExplorerViewModel(_fileExplorerViewModel);
    }

    [Test]
    public void WhenCreated_ThenFileExplorerCallBackSetBySolutionExplorer()
    {
        Assert.That(_fileExplorerViewModel.SelectedFileChangedCallBack, Is.Not.Null);
    }

    [Test]
    public void GivenFileSelected_WhenFileExplorerCallBackInvoked_ThenFileDetailsLoaded()
    {
        //Act
        _fileExplorerViewModel.SelectFile = new FileNode(TestFilePath, _fileExplorerViewModel);

        //Assert
        Assert.That(_solutionExplorer.FileDetails, Is.Not.Null.Or.Empty);
        Assert.That(_solutionExplorer.FileDetails.Count, Is.EqualTo(62)); //File has 63 lines
        int prevLineNumber = 0;
        foreach (LineDetails line in _solutionExplorer.FileDetails)
        {
            Assert.That(line.LineNumber, Is.EqualTo(++prevLineNumber));
        }
        Assert.That(_solutionExplorer.FileDetails[0].SourceCode, Is.EqualTo("namespace GuiTests.ViewModels.SolutionExplorerTests.TestData;"));
        Assert.That(_solutionExplorer.FileDetails[5].SourceCode, Is.EqualTo("public class TestContentCodeFile"));
        Assert.That(_solutionExplorer.FileDetails[61].SourceCode, Is.EqualTo("}"));
    }
}
