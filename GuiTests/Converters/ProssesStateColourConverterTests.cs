using GUI.Converters;
using Models.Enums;
using System.Windows.Media;

namespace GuiTests.Converters;

public class ProssesStateColourConverterTests
{
    private ProssesStateColourConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new ProssesStateColourConverter();
    }

    [Test]
    public void GivenInvalidInput_ThenReturnsGrey()
    {
        object value = _converter.Convert("invalid", null, null, null);
        Assert.That(value is SolidColorBrush);
        SolidColorBrush colour  = (SolidColorBrush)value;
        Assert.That(colour.Color, Is.EqualTo(ColorConverter.ConvertFromString("#9CA3AF")));
    }

    [Test]
    public void GivenNotStarted_ThenReturnsGrey()
    {
        object value = _converter.Convert(OperationStates.NotStarted, null, null, null);
        Assert.That(value is SolidColorBrush);
        SolidColorBrush colour = (SolidColorBrush)value;
        Assert.That(colour.Color, Is.EqualTo(ColorConverter.ConvertFromString("#9CA3AF")));
    }

    [Test]
    public void GivenOnoign_ThenReturnsOrange()
    {
        object value = _converter.Convert(OperationStates.Ongoing, null, null, null);
        Assert.That(value is SolidColorBrush);
        SolidColorBrush colour = (SolidColorBrush)value;
        Assert.That(colour.Color, Is.EqualTo(ColorConverter.ConvertFromString("#F59E0B")));
    }

    [Test]
    public void GivenSucceeded_ThenReturnsGreen()
    {
        object value = _converter.Convert(OperationStates.Succeeded, null, null, null);
        Assert.That(value is SolidColorBrush);
        SolidColorBrush colour = (SolidColorBrush)value;
        Assert.That(colour.Color, Is.EqualTo(ColorConverter.ConvertFromString("#22C55E")));
    }

    [Test]
    public void GivenFailed_ThenReturnsRed()
    {
        object value = _converter.Convert(OperationStates.Failed, null, null, null);
        Assert.That(value is SolidColorBrush);
        SolidColorBrush colour = (SolidColorBrush)value;
        Assert.That(colour.Color, Is.EqualTo(ColorConverter.ConvertFromString("#EF4444")));
    }
}
