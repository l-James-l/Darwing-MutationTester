using Models;
using Models.Enums;
using Models.Events;

namespace GUI.ViewModels.SettingsElements;

public class GeneralSettingsViewModel : ViewModelBase
{
    private readonly IMutationSettings _settings;

    public GeneralSettingsViewModel(IMutationSettings settings, IEventAggregator eventAggregator)
    {
        _settings = settings;

        RefreshFromNewProfile();
        eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => RefreshFromNewProfile(), ThreadOption.UIThread, true, x => x is DarwingOperation.LoadSolution);
    }

    private void RefreshFromNewProfile()
    {
        DefaultGitComparisonBranch = _settings.DefaultGitComparisonBranch;
        BuildTimeout = _settings.BuildTimeout;
        TestTimeout = _settings.TestRunTimeout;
        SingleMutationPerLine = _settings.SingleMutantPerLine;
        SkipTestingNoActiveMutants = _settings.SkipTestingNoActiveMutants;
        UseAdvancedProjectTypeAnalysis = _settings.UseAdvancedProjectTypeAnalysis;
        MutationTestTimeoutScaler = (int)Math.Round((_settings.MutationTestTimeoutScaler * 100) - 100);
    }

    public string DefaultGitComparisonBranch
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value != _settings.DefaultGitComparisonBranch)
            {
                _settings.DefaultGitComparisonBranch = value;
            }
        }
    }

    public int BuildTimeout
    {
        get;
        set
        {
            if (int.TryParse(value.ToString(), out int result))
            {
                SetProperty(ref field, result);
                if (value != _settings.BuildTimeout)
                {
                    _settings.BuildTimeout = result;
                }
            }
        }
    }

    public int TestTimeout
    {
        get;
        set
        {
            if (int.TryParse(value.ToString(), out int result))
            {
                SetProperty(ref field, result);
                if (value != _settings.TestRunTimeout)
                {
                    _settings.TestRunTimeout = result;
                }
            }
        }
    }

    public bool SingleMutationPerLine
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value != _settings.SingleMutantPerLine)
            {
                _settings.SingleMutantPerLine = value;
            }
        }
    }

    public bool SkipTestingNoActiveMutants
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value != _settings.SkipTestingNoActiveMutants)
            {
                _settings.SkipTestingNoActiveMutants = value;
            }
        }
    }

    public bool UseAdvancedProjectTypeAnalysis
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value != _settings.UseAdvancedProjectTypeAnalysis)
            {
                _settings.UseAdvancedProjectTypeAnalysis = value;
            }
        }
    }

    /// <Note>In the model this property is a double, but is displayed as a percentage, and needs to be converted.</Note>
    public int MutationTestTimeoutScaler
    {
        get;
        set
        {
            if (int.TryParse(value.ToString(), out int result))
            {
                SetProperty(ref field, result);
                double convertedResult = 1 + (result / 100.0);
                if (convertedResult != _settings.MutationTestTimeoutScaler)
                {
                    _settings.MutationTestTimeoutScaler = convertedResult;
                }
            }
        }
    }
}
