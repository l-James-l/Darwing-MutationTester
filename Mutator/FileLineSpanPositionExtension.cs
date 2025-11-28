using Microsoft.CodeAnalysis;

namespace Mutator;

public static class FileLineSpanPositionExtension
{
    public static bool Contains(this FileLinePositionSpan span1, FileLinePositionSpan span2)
    {
        ArgumentNullException.ThrowIfNull(span1);
        ArgumentNullException.ThrowIfNull(span2);

        if (span1.StartLinePosition.CompareTo(span2.StartLinePosition) > 0)
        {
            return false;
        }
        if (span1.EndLinePosition.CompareTo(span2.EndLinePosition) < 0)
        {
            return false;
        }
        return true;
    }
}
