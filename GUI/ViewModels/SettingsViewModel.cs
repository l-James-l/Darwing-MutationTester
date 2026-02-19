using GUI.Services;
using GUI.ViewModels.SettingsElements;
using Models;
using Serilog;
using System.IO;
using YamlDotNet.Serialization;

namespace GUI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IMutationSettings _settings;
    private readonly IDarwingDialogService _dialogService;

    public SettingsViewModel(ProjectTypeCollectionSettings projectTypeSettings, GeneralSettingsViewModel generalSettingsViewModel,
        DisabledMutationTypesViewModel disabledMutationTypesViewModel, IMutationSettings settings, IDarwingDialogService dialogService)
    {
        _settings = settings;
        _dialogService = dialogService;
        ProjectTypeSettings = projectTypeSettings;
        GeneralSettingsViewModel = generalSettingsViewModel;
        DisabledMutationTypesViewModel = disabledMutationTypesViewModel;

        SaveSettingsCommand = new DelegateCommand(SaveProfile);
    }

    public ProjectTypeCollectionSettings ProjectTypeSettings { get; }
    public GeneralSettingsViewModel GeneralSettingsViewModel { get; }
    public DisabledMutationTypesViewModel DisabledMutationTypesViewModel { get; }

    public DelegateCommand SaveSettingsCommand { get; }

    public void SaveProfile()
    {
        if (string.IsNullOrEmpty(_settings.SolutionPath))
        {
            Log.Error("No solution file path specified. Solution profile could not be saved.");
            _dialogService.ErrorDialog("Save Failed",
                "No solution file path specified. Solution profile could not be saved.");
            return;
        }

        string? directory = Path.GetDirectoryName(_settings.SolutionPath);
        if (directory == null)
        {
            Log.Error("Failed to determine solution directory from solution file path. Solution profile could not be saved.");
            _dialogService.ErrorDialog("Save Failed",
                 "Failed to determine solution directory from solution file path. Solution profile could not be saved.");
            return;
        }

        SolutionProfileData profileData = BuildNewProfileObject();
        var serializer = new SerializerBuilder().Build();
        string ymlContent = serializer.Serialize(profileData);

        string path = Path.Combine(directory, ".darwingSolutionProfile.yml");
        File.WriteAllText(path, ymlContent);

        _dialogService.InfoDialog("Save Confirmation", $"Profile saved to file location: '{path}'");
    }

    private SolutionProfileData BuildNewProfileObject()
    {
        SolutionProfileData profileData = new SolutionProfileData
        {
            TestProjects = _settings.TestProjects,
            IgnoreProjects = _settings.IgnoreProjects,
            SourceCodeProjects = _settings.SourceCodeProjects,
            DisabledMutationTypes = _settings.DisabledMutationTypes,
            SingleMutantPerLine = _settings.SingleMutantPerLine,
            BuildTimeout = _settings.BuildTimeout,
            TestRunTimeout = _settings.TestRunTimeout,
            SkipTestingNoActiveMutants = _settings.SkipTestingNoActiveMutants,
            DefaultGitComparisonBranch = _settings.DefaultGitComparisonBranch,
            MutationTestTimeoutScaler = _settings.MutationTestTimeoutScaler,
            UseAdvancedProjectTypeAnalysis = _settings.UseAdvancedProjectTypeAnalysis
        };

        return profileData;
    }
}

