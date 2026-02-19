using System.IO;

namespace GUI.ViewModels.SolutionExplorerElements;


/// <summary>
/// Base class for all the nodes used in the solution explorer tree view.
/// All classes in the same file because they are so closely related, separating them would only be detrimental.
/// </summary>
public abstract class SolutionTreeNode : ViewModelBase
{
    /// <summary>
    /// Name of the file or folder represented by this node
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The absolute path to the file or directory represented by this instance.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Whether this node is currently selected in the tree view.
    /// Only FileNodes can be selected.
    /// Virtual to allow FileNode to override the setter to notify the owning vm.
    /// </summary>
    public virtual bool IsSelected { get; set; } = false;

    /// <summary>
    /// Is the file/folder going to be mutated in any capacity
    /// </summary>
    public abstract bool IsChecked { get; set; }

    /// <summary>
    /// all nodes need to be able be notified from their parent 
    /// </summary>
    public abstract void NotifyCheckedUpdateFromParent(bool check);


    /// <summary>
    /// Gets or sets the number of mutations that have been applied.
    /// For folder and project nodes, this is the total number of mutations in all child files.
    /// </summary>
    public int MutationCount
    { 
        get;
        set
        {
            field = value;
            Parent?.MutationCount = Parent.Children.Select(x => x.MutationCount).Sum();
            OnPropertyChanged();
        }
    } = 0;

    /// <summary>
    /// Gets or sets the number of mutations that have been killed during testing.  
    /// </summary>
    public int KilledMutationCount
    { 
        get;
        set
        {
            field = value;
            Parent?.KilledMutationCount = Parent.Children.Select(x => x.KilledMutationCount).Sum();
            OnPropertyChanged();
        }
    } = 0;

    /// <summary>
    /// The Folder (or project) that directly owns this file.
    /// Null is top level node
    /// </summary>
    public FolderNode? Parent 
    { 
        get;
        set
        {
            field = value;
            //Checked value are initialized bottom up, before parents are assigned.
            //So when we assign it, notify the parent it needs to reevaluate its state
            value?.NotifyCheckedUpdateFromChild();
        } 
    }

    protected SolutionTreeNode(string fullPath)
    {
        ArgumentNullException.ThrowIfNull(fullPath);

        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
    }
}
