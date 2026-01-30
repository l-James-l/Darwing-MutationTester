using GUI.Services;
using Models;
using Models.Enums;
using Models.Events;
using System.Collections.ObjectModel;
using System.IO;

namespace GUI.ViewModels.SolutionExplorerElements;

/// <summary>
/// View model for the solution explorer tree, showing all the projects and their contained files.
/// </summary>
public class FileExplorerViewModel : ViewModelBase
{
    private readonly ISolutionProvider _solutionProvider;
    private readonly IEventAggregator _eventAggregator;

    public FileExplorerViewModel(ISolutionProvider solutionProvider, IEventAggregator eventAggregator)
    {
        _solutionProvider = solutionProvider;
        _eventAggregator = eventAggregator;

        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => OnSolutionLoaded(),
            ThreadOption.UIThread, true, x => x == DarwingOperation.LoadSolution);
    }

    /// <summary>
    /// The full path of the currently selected file.
    /// Null if no file has been selected yet
    /// </summary>
    public string? SelectFilePath 
    { 
        get => _selectedFilePath; 
        set 
        {
            _selectedFilePath = value;
            OnPropertyChanged();
        } 
    }
    private string? _selectedFilePath = null;

    /// <summary>
    /// The tree that contains all the different folders, projects, and .cs files
    /// Excludes test projects
    /// 
    /// TODO make a separate tree to allow showing the test projects separately.
    /// </summary>
    public ObservableCollection<SolutionTreeNode> SolutionTree { get; private set; } = [];

    private void OnSolutionLoaded()
    {
        SolutionTree.Clear();
        if (!_solutionProvider.IsAvailable || _solutionProvider.SolutionContainer.Solution.FilePath is null)
        {
            return;
        }

        string? solutionFolder = Path.GetDirectoryName(_solutionProvider.SolutionContainer.Solution.FilePath);
        FolderNode dummyRoot = new (solutionFolder ?? "");
        BuildSolutionTree(dummyRoot);
        SolutionTree.AddRange(dummyRoot.Children);
    }

    private void BuildSolutionTree(FolderNode parentFolder)
    {
        foreach (string filePath in Directory.GetFiles(parentFolder.FullPath, "*.cs", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".xaml.cs") || fileName.EndsWith(".g.cs") || fileName.EndsWith(".g.i.cs"))
            {
                continue;
            }
            parentFolder.Children.Add(new FileNode(filePath, this));

            //Because we've added this code file, we now know the parent has at least 1 code file
            parentFolder.ContainsCodeFiles = true;
        }

        foreach (string dir in Directory.GetDirectories(parentFolder.FullPath, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(dir);
            if (name.EndsWith(".git") || name.EndsWith("bin") || name.EndsWith("obj") || name.StartsWith('.'))
            {
                continue;
            }

            string wouldBeProjFile = Path.Combine(dir, $"{name}.csproj");
            if (File.Exists(wouldBeProjFile))
            {
                if (_solutionProvider.SolutionContainer.TestProjects.Any(x => x.CsprojFilePath == wouldBeProjFile))
                {
                    // Dont want to show test projects in the main tree.
                    // Can show these separately later.
                    continue;
                }
                ProjectNode projectNode = new (dir);
                parentFolder.Children.Add(projectNode);
                BuildSolutionTree(projectNode);
            }
            else
            {
                FolderNode folderNode = new (dir);
                parentFolder.Children.Add(folderNode);
                BuildSolutionTree(folderNode);
            }
        }

        // Check for nested folders containing code files
        if (parentFolder.Children.Any(x => x is FolderNode { ContainsCodeFiles: true }))
        {
            parentFolder.ContainsCodeFiles = true;
        }

        // Remove any pointless folder from the tree. 
        parentFolder.Children.RemoveWhere(x => x is FolderNode { ContainsCodeFiles: false });
    }
}
