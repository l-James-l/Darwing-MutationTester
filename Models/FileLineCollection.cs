using Microsoft.CodeAnalysis;

namespace Models;

/// <summary>
/// Collection to represent a collection of lines in a file.
/// </summary>
public class FileLineCollection : List<LineRange>
{
    private readonly int _finalLineIndex;
    private readonly string _filePath;

    public FileLineCollection(SyntaxTree syntaxTree)
    {
        _finalLineIndex = syntaxTree.GetText().Lines.Count - 1;
        _filePath = syntaxTree.FilePath;

        //Start with the entire file  
        Add(new LineRange { Start=0, End=_finalLineIndex });
    }

    /// <summary>
    /// Is the line number included in the ranges
    /// </summary>
    public bool ContainsLine(int line)
    {
        return FindContainingRange(line) is not null;
    }

    /// <summary>
    /// Adds a single line to the collection
    /// </summary>
    /// <param name="line"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Add(int line)
    {
        if (line < 0 || line > _finalLineIndex)
        {
            throw new ArgumentOutOfRangeException($"start or end outside range of file {_filePath}. Accepted Range: (0, {_finalLineIndex}), Received line: {line}.");
        }

        // If already included, do nothing
        LineRange? containingRange = FindContainingRange(line);
        if (containingRange.HasValue)
        {
            return;
        }
        Add(new LineRange { Start = line, End = line });
        MaintainAdjacentRanges();
    }

    /// <summary>
    /// This method maintains the included ranges, such that rnages are ordered, and adjacent ranges are combined
    /// </summary>
    private void MaintainAdjacentRanges()
    {
        List<LineRange> sorted = [.. this.OrderBy(x => x.Start)];

        Clear();
        LineRange current = sorted[0];
        for (int i = 1; i < sorted.Count; i++)
        {
            LineRange next = sorted[i];
            // Adjacent ranges should be combined
            if (next.Start <= current.End + 1)
            {
                current = new LineRange
                {
                    Start = current.Start,
                    End = next.End
                };
            }
            else
            {
                Add(current);
                current = next;
            }
        }
        Add(current);
    }

    /// <summary>
    /// Include a new set of lines from start index to end index
    /// </summary>
    /// <param name="start">index of first line in set</param>
    /// <param name="end">index of last line in set</param>
    /// <Note>0 indexed</Note>
    public void Add(int start, int end)
    {
        if (start < 0 || end < 0 || start > _finalLineIndex || end > _finalLineIndex)
        {
            throw new ArgumentOutOfRangeException($"start or end outside range of file {_filePath}. Accepted Range: (0, {_finalLineIndex}), Received Range: ({start}, {end}).");
        }
        if (start > end)
        {
            throw new InvalidOperationException("start greater than end index");
        }

        LineRange? startOverlap = null;
        LineRange? endOverlap = null;

        foreach (LineRange lineRange in this)
        {
            if (lineRange.Start <= start && start <= lineRange.End)
            {
                startOverlap = lineRange;
            }
            if (lineRange.Start <= end && end <= lineRange.End)
            {
                endOverlap = lineRange;
            }
        }

        LineRange newRange;
        if (startOverlap.HasValue && endOverlap.HasValue)
        {
            newRange = new() { Start=startOverlap.Value.Start, End=endOverlap.Value.End };
            Remove(startOverlap.Value);
            Remove(endOverlap.Value);
        }
        else if (startOverlap.HasValue)
        {
            newRange = new() { Start=startOverlap.Value.Start, End=Math.Max(end, startOverlap.Value.End) };
            Remove(startOverlap.Value);
        }
        else if (endOverlap.HasValue)
        {
            newRange = new() { Start =Math.Min(start, endOverlap.Value.Start), End = endOverlap.Value.End };
            Remove(endOverlap.Value);
        }
        else
        {
            newRange = new() { Start=start, End=end };
        }
        Add(newRange);
        MaintainAdjacentRanges();
    }

    public void Remove(int line)
    {

        LineRange? containingSpan = FindContainingRange(line);
        if (!containingSpan.HasValue)
        {
            return;
        }
        Remove(containingSpan.Value);
        if (line - 1 >= 0 && containingSpan.Value.Start < line)
        {
            LineRange newLower = new() { Start = containingSpan.Value.Start, End = line - 1 };
            Add(newLower);
        }
        if (line + 1 <= _finalLineIndex && containingSpan.Value.End > line)
        {
            LineRange newUpper = new() { Start = line + 1, End = containingSpan.Value.End };
            Add(newUpper);
        }
    }

    private LineRange? FindContainingRange(int line)
    {
        //Set to null and do an any check before the first or default assignment so that value is left null when
        //no range exists rather than a default LineRange
        if (!this.Any(x => x.Start <= line && line <= x.End))
        {
            return null;
        }
        LineRange? containingSpan = this.FirstOrDefault(x => x.Start <= line && line <= x.End);
        return containingSpan;
    }

    public void Fill()
    {
        Clear();
        Add(new LineRange
        {
            Start = 0,
            End = _finalLineIndex
        });
    }
}

public struct LineRange
{
    public int Start;
    public int End;
}