using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;

namespace ModelsTests;

public class FileLineCollectionTests
{
    private SyntaxTree _tree;

    [SetUp]
    public void Setup()
    {
        _tree = CSharpSyntaxTree.ParseText(TestFileContents);
    }

    [Test]
    public void TestInitialisedToEntireFileContent()
    {
        FileLineCollection lineCollection = new(_tree);

        Assert.That(lineCollection.Count, Is.EqualTo(1));
        Assert.That(lineCollection.First().Start, Is.EqualTo(0));
        Assert.That(lineCollection.First().End, Is.EqualTo(73));
    }

    [Test]
    public void GivenSequenceOfAddingOverlappingRanges_ThenRangesCombined()
    {
        //Arrange
        FileLineCollection lineCollection = new(_tree);
        lineCollection.Clear();

        //Act
        lineCollection.Add(5, 10);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(1));
        Assert.That(lineCollection.First().Start, Is.EqualTo(5));
        Assert.That(lineCollection.First().End, Is.EqualTo(10));

        //Act
        lineCollection.Add(7, 14);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(1));
        Assert.That(lineCollection.First().Start, Is.EqualTo(5));
        Assert.That(lineCollection.First().End, Is.EqualTo(14));

        //Act
        lineCollection.Add(3, 12);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(1));
        Assert.That(lineCollection.First().Start, Is.EqualTo(3));
        Assert.That(lineCollection.First().End, Is.EqualTo(14));
    }

    [Test]
    public void GivenAddingRangeThatOverlaps2ExistingRanges_ExistingRangesCombined()
    {
        //Arrange
        FileLineCollection lineCollection = new(_tree);
        lineCollection.Clear();
        lineCollection.Add(5, 10);
        lineCollection.Add(20, 25);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(2));
        Assert.That(lineCollection[0].Start, Is.EqualTo(5));
        Assert.That(lineCollection[0].End, Is.EqualTo(10));
        Assert.That(lineCollection[1].Start, Is.EqualTo(20));
        Assert.That(lineCollection[1].End, Is.EqualTo(25));

        //Act
        lineCollection.Add(8, 22);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(1));
        Assert.That(lineCollection.First().Start, Is.EqualTo(5));
        Assert.That(lineCollection.First().End, Is.EqualTo(25));
    }

    [Test]
    public void GivenRemoveLine_ThenSplitsContainingRangeAtLine()
    {
        FileLineCollection lineCollection = new(_tree);
        
        //Act
        lineCollection.Remove(5);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(2));
        Assert.That(lineCollection[0].Start, Is.EqualTo(0));
        Assert.That(lineCollection[0].End, Is.EqualTo(4));
        Assert.That(lineCollection[1].Start, Is.EqualTo(6));
        Assert.That(lineCollection[1].End, Is.EqualTo(73));
    }

    [Test]
    public void WhenRemoveIndexNoInAnyRanges_ThenTakesNoAction()
    {
        //Arrange
        FileLineCollection lineCollection = new(_tree);

        //Act
        lineCollection.Remove(75);

        //Assert
        Assert.That(lineCollection.Count, Is.EqualTo(1));
        Assert.That(lineCollection.First().Start, Is.EqualTo(0));
        Assert.That(lineCollection.First().End, Is.EqualTo(73));
    }

    [Test]
    public void WhenAddWithValuesOutOfRange_ThenThrowsException()
    {
        FileLineCollection lineCollection = new(_tree);

        Assert.Throws<ArgumentOutOfRangeException>(() => lineCollection.Add(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineCollection.Add(5, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineCollection.Add(75, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => lineCollection.Add(5, 75));
    }

    [Test]
    public void GivenStartGreterThanEndIndex_ThenThrowsException()
    {
        FileLineCollection lineCollection = new(_tree);
        
        Assert.Throws<InvalidOperationException>(() => lineCollection.Add(5, 3));
    }

    [Test]
    public void GivenNodeInTree_ThenIsNodeWithinTrue()
    {
        FileLineCollection lineCollection = new(_tree);

        Assert.That(lineCollection.IsNodeWithin(_tree.GetRoot()), Is.True);
    }

    [Test]
    public void GivenNodeNotInTree_ThenIsNodeWithinFalse()
    {
        FileLineCollection lineCollection = new(_tree);

        Assert.That(lineCollection.IsNodeWithin(SyntaxFactory.EmptyStatement()), Is.False);
    }

    [Test]
    public void GivenNodeIsInTree_ButNotOnIncludedLine_ThenIsNodeWithinFalse()
    {
        FileLineCollection lineCollection = new(_tree);

        SyntaxNode node = _tree.GetRoot().ChildNodes().First().ChildNodes().First();
        lineCollection.Remove(node.GetLocation().GetLineSpan().StartLinePosition.Line);
        
        Assert.That(lineCollection.IsNodeWithin(node), Is.False);

    }

    [Test]
    public void GivenAddingSingleLine_ThenAddsLine()
    {
        FileLineCollection lineCollection = new(_tree);
        lineCollection.Clear();

        lineCollection.Add(5);

        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(5));
        Assert.That(lineCollection[0].End, Is.EqualTo(5));
    }

    [Test]
    public void GivenAddingAdjacentRange_ThenRangesCombined()
    {
        FileLineCollection lineCollection = new(_tree);
        lineCollection.Clear();

        lineCollection.Add(15, 20);
        lineCollection.Add(10, 14);

        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(10));
        Assert.That(lineCollection[0].End, Is.EqualTo(20));
    }
    
    [Test]
    public void GivenAddingAdjacentRange_ThenRangesCombined2()
    {
        FileLineCollection lineCollection = new(_tree);
        lineCollection.Clear();

        lineCollection.Add(16, 20);
        lineCollection.Add(10, 14);
        lineCollection.Add(15);

        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(10));
        Assert.That(lineCollection[0].End, Is.EqualTo(20));
    }

    [Test]
    public void WhenRemovingFirstLineInCollection_NoLowerRangeAdded()
    {
        FileLineCollection lineCollection = new(_tree);
        
        lineCollection.Remove(0);
        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(1));
        Assert.That(lineCollection[0].End, Is.EqualTo(73));

        lineCollection.Remove(1);
        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(2));
        Assert.That(lineCollection[0].End, Is.EqualTo(73));
    }

    [Test]
    public void WhenRemovingLastLineInCollection_ThenNoUpperRangeAdded()
    {
        FileLineCollection lineCollection = new(_tree);

        lineCollection.Remove(73);
        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(0));
        Assert.That(lineCollection[0].End, Is.EqualTo(72));

        lineCollection.Remove(72);
        Assert.That(lineCollection, Has.Count.EqualTo(1));
        Assert.That(lineCollection[0].Start, Is.EqualTo(0));
        Assert.That(lineCollection[0].End, Is.EqualTo(71));
    }


    private const string TestFileContents =
        """
        Public class FileLineCollection : List<LineRange>
        {
            private int _finalLineIndex;

            public FileLineCollection(SyntaxTree syntaxTree)
            {
                _finalLineIndex = syntaxTree.GetText().Lines.Count - 1;

                //Start with the entire file  
                Add(new LineRange { Start=0, End=_finalLineIndex });
            }

            /// <summary>
            /// Include a new set of lines from start index to end index
            /// </summary>
            /// <param name="start">index of first line in set</param>
            /// <param name="end">index of last line in set</param>
            /// <Note>0 indexed</Note>
            public void Add(int start, int end)
            {
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
            }

            public void Remove(int line)
            {
                LineRange? containingSpan = this.FirstOrDefault(x => x.Start <= line && x.End <= line);
                if (!containingSpan.HasValue)
                {
                    return;
                }
                LineRange newLower = new() { Start=containingSpan.Value.Start, End=line-1 };
                LineRange newUpper = new() { Start = line + 1, End = containingSpan.Value.End };
                Remove(containingSpan.Value);
                Add(newLower);
                Add(newUpper);
            }

        }
        """;
}
