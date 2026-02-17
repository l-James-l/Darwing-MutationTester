using System.Collections.ObjectModel;

namespace GUI.ViewModels.SolutionExplorerElements;

/// <summary>
/// Represents a folder in the solution explorer tree
/// </summary>
public class FolderNode : SolutionTreeNode
{
    public ObservableCollection<SolutionTreeNode> Children { get; }

    /// <summary>
    /// By tracking if the folder contains any code files, we can ignore folders that do not contain any code files.
    /// Such as folders that only contain resources, documentation, or binaries.
    /// </summary>
    public bool ContainsCodeFiles { get; set; } = false;

    public override bool IsChecked 
    { 
        get;
        set 
        {
            SetProperty(ref field, value);
            if (!_supressChildNotifications)
            {
                foreach (SolutionTreeNode child in Children)
                {
                    child.NotifyCheckedUpdateFromParent(value);
                }
            }
            if (!_supressParentNotifications)
            {
                Parent?.NotifyCheckedUpdateFromChild();
            }
        }
    }
    private bool _supressChildNotifications = false;
    private bool _supressParentNotifications = false;

    /// <summary>
    /// When the checked state of any files/folders in the folder are checked/unchecked.
    /// Will suppress downwards propagation.
    /// </summary>
    public void NotifyCheckedUpdateFromChild()
    {
        _supressChildNotifications = true;
        IsChecked = Children.Any(x => x.IsChecked);
        _supressChildNotifications = false;
    }

    /// <summary>
    /// When the parent folder is checked/unchecked.
    /// Suppresses upwards propagation
    /// </summary>
    public override void NotifyCheckedUpdateFromParent(bool check)
    {
        _supressParentNotifications = true;
        IsChecked = check;
        _supressParentNotifications = false;
    }

    public FolderNode(string fullPath) : base(fullPath)
    {
        Children = [];
    }
}

/// <summary>
/// Represents a project folder node within a hierarchical structure, such as a solution or workspace.
/// Adds no new functionality beyond FolderNode, but by being a distinct type, it allows for specific handling in the tree view.
/// </summary>
public sealed class ProjectNode : FolderNode
{
    public ProjectNode(string fullPath) : base(fullPath)
    {

    }
}
