using Models.Enums;
using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters;

class MutationStateStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MutantStatus status)
        {
            return "Awaiting Test";
        }

        // We return only Survived and Killed as strings, all other states are considered "Awaiting Test"
        // This is to simplify the UI and focus on the most relevant states for the user
        // CausedBuildError mutants aren't displayed at all in the UI, and beyond that, the other states are less relevant for the user
        return status switch
        {
            MutantStatus.Survived => nameof(MutantStatus.Survived),
            MutantStatus.Killed => nameof(MutantStatus.Killed),
            MutantStatus.KilledByTimeOut => "Killed by test timeout",
            MutantStatus.NoCoverage => "Survived, No Coverage",
            MutantStatus.IgnoredMultipleOnLine => "Not Tested, Multiple Mutants on Line",
            _ => "Awaiting Test"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
