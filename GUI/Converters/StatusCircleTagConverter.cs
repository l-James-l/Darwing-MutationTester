using Models.Enums;
using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters;

public class StatusCircleTagConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[1] is not OperationStates status || values[0] is not string indexStr || !int.TryParse(indexStr, out int index))
        {
            return "";
        }

        if (status is OperationStates.Succeeded)
        {
            return "✔";
        }

        if (status is OperationStates.Failed)
        {
            return "X";
        }

        if (status is OperationStates.Ongoing)
        {
            return "⌛";
        }

        return indexStr;

    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}