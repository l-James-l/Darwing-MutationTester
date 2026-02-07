using GUI.ViewModels.SettingsElements;

namespace GUI.ViewModels;

public interface ISettingsViewModel
{

}

public class SettingsViewModel : ViewModelBase, ISettingsViewModel
{
    public SettingsViewModel(ProjectTypeCollectionSettings projectTypeSettings, GeneralSettingsViewModel generalSettingsViewModel)
    {
        ProjectTypeSettings = projectTypeSettings;
        GeneralSettingsViewModel = generalSettingsViewModel;
    }

    public ProjectTypeCollectionSettings ProjectTypeSettings { get; }
    public GeneralSettingsViewModel GeneralSettingsViewModel { get; }
}

