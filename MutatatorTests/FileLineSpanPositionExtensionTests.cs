using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Mutator;

namespace MutatorTests;

public class FileLineSpanPositionExtensionTests
{
    [Test]
    public void Contains_GivenIdenticalSpans_ReturnsTrue()
    {
        // Arrange
        var span1 = CreateSpan(1, 5, 3, 10);
        var span2 = CreateSpan(1, 5, 3, 10);

        // Act & Assert
        Assert.That(span1.Contains(span2), Is.True);
    }

    [Test]
    public void Contains_GivenSpan2IsStrictlyInsideSpan1_ReturnsTrue()
    {
        // Arrange
        var span1 = CreateSpan(1, 0, 10, 0);
        var span2 = CreateSpan(2, 0, 5, 0);

        // Act & Assert
        Assert.That(span1.Contains(span2), Is.True);
    }

    [Test]
    public void Contains_GivenSpan2StartsBeforeSpan1_ReturnsFalse()
    {
        // Arrange
        var span1 = CreateSpan(5, 0, 10, 0);
        var span2 = CreateSpan(4, 0, 6, 0); // Starts at line 4, span 1 starts at line 5

        // Act & Assert
        Assert.That(span1.Contains(span2), Is.False);
    }

    [Test]
    public void Contains_GivenSpan2EndsAfterSpan1_ReturnsFalse()
    {
        // Arrange
        var span1 = CreateSpan(1, 0, 5, 0);
        var span2 = CreateSpan(2, 0, 6, 0); // Ends at line 6, span 1 ends at line 5

        // Act & Assert
        Assert.That(span1.Contains(span2), Is.False);
    }

    [Test]
    public void Contains_GivenSpansOnSameLine_CorrectlyEvaluatesCharacterPositions()
    {
        // Arrange
        var span1 = CreateSpan(1, 10, 1, 20);

        var inside = CreateSpan(1, 11, 1, 19);
        var startsEarly = CreateSpan(1, 9, 1, 15);
        var endsLate = CreateSpan(1, 15, 1, 21);

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(span1.Contains(inside), Is.True, "Should contain inner character span");
            Assert.That(span1.Contains(startsEarly), Is.False, "Should fail if start character is outside");
            Assert.That(span1.Contains(endsLate), Is.False, "Should fail if end character is outside");
        });
    }

    [Test]
    public void Contains_GivenCompletelyDisjointSpans_ReturnsFalse()
    {
        // Arrange
        var span1 = CreateSpan(1, 0, 2, 0);
        var span2 = CreateSpan(10, 0, 11, 0);

        // Act & Assert
        Assert.That(span1.Contains(span2), Is.False);
    }

    /// <summary>
    /// Helper to create FileLinePositionSpan quickly
    /// </summary>
    private FileLinePositionSpan CreateSpan(int startLine, int startChar, int endLine, int endChar)
    {
        return new FileLinePositionSpan(
            "test.cs",
            new LinePosition(startLine, startChar),
            new LinePosition(endLine, endChar)
        );
    }
}