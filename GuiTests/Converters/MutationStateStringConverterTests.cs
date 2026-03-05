using GUI.Converters;
using Models.Enums;

namespace GuiTests.Converters;

public class MutationStateStringConverterTests
{
    [Test]
    public void GivenInvalidValue_WhenConvertIsCalled_ThenAwaitingTestIsReturned()
    {
        // Arrange
        var converter = new MutationStateStringConverter();
        var invalidValue = "InvalidStatus";

        // Act
        var result = converter.Convert(invalidValue, null, null, null);
        
        // Assert
        Assert.That(result, Is.EqualTo("Awaiting Test"));
    }

    [Test]
    public void GivenKilledStatus_WhenConvertIsCalled_ThenKilledIsReturned()
    {
        // Arrange
        var converter = new MutationStateStringConverter();
        var status = MutantStatus.Killed;

        // Act
        var result = converter.Convert(status, null, null, null);
        
        // Assert
        Assert.That(result, Is.EqualTo("Killed"));
    }

    [Test]
    public void GivenKilledByTimeoutStatus_WhenConvertIsCalled_ThenKilledByTimeoutIsReturned()
    {
        // Arrange
        var converter = new MutationStateStringConverter();
        var status = MutantStatus.KilledByTimeOut;

        // Act
        var result = converter.Convert(status, null, null, null);
        
        // Assert
        Assert.That(result, Is.EqualTo("Killed by test timeout"));
    }

    [Test]
    public void GivenSurvivedStatus_WhenConvertIsCalled_ThenSurvivedIsReturned()
    {
        // Arrange
        var converter = new MutationStateStringConverter();
        var status = MutantStatus.Survived;

        // Act
        var result = converter.Convert(status, null, null, null);
        
        // Assert
        Assert.That(result, Is.EqualTo("Survived"));
    }

    [Test]
    public void GivenNoCoverageStatus_WhenConvertIsCalled_ThenSurvivedNoCoverageIsReturned()
    {
        // Arrange
        var converter = new MutationStateStringConverter();
        var status = MutantStatus.NoCoverage;

        // Act
        var result = converter.Convert(status, null, null, null);
        
        // Assert
        Assert.That(result, Is.EqualTo("Survived, No Coverage"));
    }

    [Test]
    public void GivenIgnoredStatus_WhenConvertIsCalled_ThenIgnoredIsReturned()
    {
        // Arrange
        var converter = new MutationStateStringConverter();
        var status = MutantStatus.IgnoredMultipleOnLine;

        // Act
        var result = converter.Convert(status, null, null, null);
        
        // Assert
        Assert.That(result, Is.EqualTo("Not Tested, Multiple Mutants on Line"));
    }

    [Test]
    public void GivenOtherStatuses_WhenConvertIsCalled_ThenAwaitingTestIsReturned()
    {
        foreach (var status in Enum.GetValues<MutantStatus>().Except([MutantStatus.Killed, MutantStatus.Survived, MutantStatus.NoCoverage, MutantStatus.IgnoredMultipleOnLine, MutantStatus.KilledByTimeOut]))
        {
            // Arrange
            var converter = new MutationStateStringConverter();

            // Act
            var result = converter.Convert(status, null, null, null);

            // Assert
            Assert.That(result, Is.EqualTo("Awaiting Test"));
        }
    }
}
