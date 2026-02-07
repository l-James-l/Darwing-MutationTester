using Models;
using Models.Enums;
using Models.Events;

namespace GUI.ViewModels.SettingsElements;

public class GeneralSettingsViewModel : ViewModelBase
{
    private readonly IMutationSettings _settings;
    private readonly IEventAggregator _eventAggregator;

    public GeneralSettingsViewModel(IMutationSettings settings, IEventAggregator eventAggregator)
    {
        _settings = settings;
        _eventAggregator = eventAggregator;

        RefreshFromNewProfile();
        eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => RefreshFromNewProfile(), ThreadOption.UIThread, true, x => x is DarwingOperation.LoadSolution);
    }

    private void RefreshFromNewProfile()
    {
        BuildTimeout = _settings.BuildTimeout;
        TestTimeout = _settings.TestRunTimeout;
        SingleMutationPerLine = _settings.SingleMutantPerLine;
        SkipTestingNoActiveMutants = _settings.SkipTestingNoActiveMutants;
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
}
