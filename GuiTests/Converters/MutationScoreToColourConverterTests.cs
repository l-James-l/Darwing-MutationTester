using GUI.Converters;
using System.Windows.Media;

namespace GuiTests.Converters;

public class MutationScoreToColourConverterTests
{
    private MutationScoreToColourConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new();
    }

    [Test]
    public void GivenNonIntValue_ThenReturnsRed()
    {
        // Act
        object result = _converter.Convert("not an int", default!, default!, default!);

        //Assert
        Assert.That(result is SolidColorBrush);
        SolidColorBrush brush = (SolidColorBrush)result;
        Assert.That(brush.Color, Is.EqualTo(Brushes.Red.Color));
    }

    [TestCase(0)]
    [TestCase(25)]
    [TestCase(49)]
    public void GivenLessThen50_ThenReturnsRed(int value)
    {
        // Act
        object result = _converter.Convert(value, default!, default!, default!);

        //Assert
        Assert.That(result is SolidColorBrush);
        SolidColorBrush brush = (SolidColorBrush)result;
        Assert.That(brush.Color, Is.EqualTo(Brushes.Red.Color));
    }


    [TestCase(50)]
    [TestCase(75)]
    [TestCase(84)]
    public void GivenBetween50And85_ThenReturnsOrange(int value)
    {
        // Act
        object result = _converter.Convert(value, default!, default!, default!);

        //Assert
        Assert.That(result is SolidColorBrush);
        SolidColorBrush brush = (SolidColorBrush)result;
        Assert.That(brush.Color, Is.EqualTo(Brushes.Orange.Color));
    }

    [TestCase(85)]
    [TestCase(90)]
    [TestCase(100)]
    public void GivenBetween85And100_ThenReturnsGreen(int value)
    {
        // Act
        object result = _converter.Convert(value, default!, default!, default!);

        //Assert
        Assert.That(result is SolidColorBrush);
        SolidColorBrush brush = (SolidColorBrush)result;
        Assert.That(brush.Color, Is.EqualTo(Brushes.Green.Color));
    }

    [Test]
    public void GivenLessThan0_ThenReturnsRed()
    {
        // Act
        object result = _converter.Convert(-5, default!, default!, default!);

        //Assert
        Assert.That(result is SolidColorBrush);
        SolidColorBrush brush = (SolidColorBrush)result;
        Assert.That(brush.Color, Is.EqualTo(Brushes.Red.Color));
    }

    [Test]
    public void GivenGreaterThan100_ThenReturnsGreen()
    {
        // Act
        object result = _converter.Convert(110, default!, default!, default!);

        //Assert
        Assert.That(result is SolidColorBrush);
        SolidColorBrush brush = (SolidColorBrush)result;
        Assert.That(brush.Color, Is.EqualTo(Brushes.Green.Color));
    }
}
