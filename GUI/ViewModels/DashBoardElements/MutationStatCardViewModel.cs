using System.Windows.Media;

namespace GUI.ViewModels.DashBoardElements;

public class MutationStatCardViewModel : ViewModelBase
{
    /// <summary>
    /// Title displayed on the card
    /// </summary>
    public string Title
    {
        get; 
        set => SetProperty(ref field, value);
    } = "";

    /// <summary>
    /// Binding property for how the value is displayed
    /// </summary>
    public string ValueString
    {
        get;
        set => SetProperty(ref field, value);
    } = "";


    /// <summary>
    /// The actual value
    /// </summary>
    public int Value
    {
        get;
        set
        {
            SetProperty(ref field, value);
            ValueString = $"{value}{ValueSuffix}";
        }
    }

    /// <summary>
    /// Suffix to be applied to the value to result in the <see cref="ValueString"/>
    /// </summary>
    public string ValueSuffix
    {
        get;
        set
        {
            SetProperty(ref field, value);
            ValueString = $"{Value}{value}";
        }
    } = "";

    /// <summary>
    /// Substile displayed on the card
    /// </summary>
    public string Subtitle
    {
        get; 
        set => SetProperty(ref field, value);
    } = "";

    /// <summary>
    /// Icon displayed on the card
    /// </summary>
    public string Icon
    {
        get; 
        set => SetProperty(ref field, value);
    } = "";

    /// <summary>
    /// Colour of the icon
    /// </summary>
    public Brush IconBrush
    {
        get; 
        set => SetProperty(ref field, value);
    } = Brushes.Blue;
}