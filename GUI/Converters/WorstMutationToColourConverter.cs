using GUI.ViewModels.SolutionExplorerElements;
using Models;
using Models.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters;

/// <summary>
/// Converter used to get a colour representing the worst mutation status for a collection of mutations.
/// </summary>
class WorstMutationToColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<MutationViewModel> mutations || !mutations.Any())
        {
            return Brushes.Transparent;
        }

        if (mutations.Any(x => x.Mutation.Status.IncludeInSurvivedCount()))
        {
            return Brushes.Red;
        }
        else if (mutations.Any(x => !x.Mutation.Status.IncludeInKilledCount() && x.Mutation.Status != MutantStatus.IgnoredMultipleOnLine))
        {
            return Brushes.Orange;
        }

        return Brushes.Green;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
