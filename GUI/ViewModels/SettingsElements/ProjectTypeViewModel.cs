using Models;
using Models.Enums;

namespace GUI.ViewModels.SettingsElements;

/// <summary>
/// View model used as the backing data context for each entry in the <see cref="ProjectTypeCollectionSettings.Projects"/>
/// Allows each entry to set its own type
/// </summary>
public class ProjectTypeViewModel : ViewModelBase
{
    private readonly Action<IProjectContainer, ProjectType> _callBack;

    public IProjectContainer Project { get; }

    public ProjectType Type
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _callBack.Invoke(Project, value);
        }
    }

    public ProjectTypeViewModel(IProjectContainer project, Action<IProjectContainer, ProjectType> callBack)
    {
        Project = project;
        _callBack = callBack;
        Type = project.ProjectType;
    }
}