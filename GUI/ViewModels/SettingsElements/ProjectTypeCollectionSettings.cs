using Models;
using Models.Enums;
using Models.Events;
using System.Collections.ObjectModel;

namespace GUI.ViewModels.SettingsElements;

/// <summary>
/// View model for the settings section that allows the user to set the project type for all the loaded projects.
/// </summary>
public class ProjectTypeCollectionSettings
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IMutationSettings _settings;

    public ProjectTypeCollectionSettings(IEventAggregator eventAggregator, ISolutionProvider solutionProvider, IMutationSettings settings)
    {
        _eventAggregator = eventAggregator;
        _solutionProvider = solutionProvider;
        _settings = settings;

        eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => RefreshProjects(), ThreadOption.UIThread, true, x => x == DarwingOperation.LoadSolution);

        ProjectTypes = [.. Enum.GetValues<ProjectType>()];
    }

    /// <summary>
    /// Binding property for the project type item control
    /// </summary>
    public ObservableCollection<ProjectTypeViewModel> Projects { get; } = [];

    /// <summary>
    /// Binding property for the available project types dropdown
    /// </summary>
    public ObservableCollection<ProjectType> ProjectTypes { get; }

    private void RefreshProjects()
    {
        if (!_solutionProvider.IsAvailable || _solutionProvider.SolutionContainer is null)
        {
            return;
        }

        Projects.Clear();
        foreach (IProjectContainer proj in _solutionProvider.SolutionContainer.AllProjects)
        {
            Projects.Add(new ProjectTypeViewModel(proj, UpdateSettingsAndNotify));
        }
    }

    private void UpdateSettingsAndNotify(IProjectContainer project, ProjectType newType)
    {
        //method invoked when users change the type of a project.
        //we maintain the settings lists, so that they can be saved, and reloaded later
        //setting the project type is what actually affects behaviour

        project.ProjectType = newType;

        SyncProjectSetting(_settings.SourceCodeProjects, project.Name, nameof(IMutationSettings.SourceCodeProjects), newType == ProjectType.Source);
        SyncProjectSetting(_settings.TestProjects, project.Name, nameof(IMutationSettings.TestProjects), newType == ProjectType.Test);
        SyncProjectSetting(_settings.IgnoreProjects, project.Name, nameof(IMutationSettings.IgnoreProjects), newType == ProjectType.Ignore);
    }

    private void SyncProjectSetting(List<string> collection, string projectName, string settingName, bool shouldExist)
    {
        bool exists = collection.Contains(projectName);

        if (shouldExist && !exists)
        {
            collection.Add(projectName);
            _eventAggregator.GetEvent<SettingChanged>().Publish(settingName);
        }
        else if (!shouldExist && exists)
        {
            collection.Remove(projectName);
            _eventAggregator.GetEvent<SettingChanged>().Publish(settingName);
        }
    }
}
