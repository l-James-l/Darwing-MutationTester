using GUI.ViewModels.SettingsElements;

namespace GUI.ViewModels;

public interface ISettingsViewModel
{

}

public class SettingsViewModel : ViewModelBase, ISettingsViewModel
{
    public SettingsViewModel(ProjectTypeCollectionSettings projectTypeSettings)
    {
        ProjectTypeSettings = projectTypeSettings;
    }

    public ProjectTypeCollectionSettings ProjectTypeSettings { get; }
}

