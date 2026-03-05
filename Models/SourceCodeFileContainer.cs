using Microsoft.CodeAnalysis;

namespace Models;

/// <summary>
/// Represents a single source code file.
/// </summary>
public class SourceCodeFileContainer
{
    /// <summary>
    /// Full path to file
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// ID of the document
    /// </summary>
    public DocumentId DocumentId { get; } 

    /// <summary>
    /// Unmutated syntax tree
    /// </summary>
    public SyntaxTree SyntaxTree { get; }

    /// <summary>
    /// Syntax tree after mutation
    /// </summary>
    public SyntaxTree? MutatedTree { get; set; }

    /// <summary>
    /// All the lines in the file that will be mutated
    /// </summary>
    public FileLineCollection LinesToMutate { get; }

    /// <summary>
    /// A mapping of line number to test names that cover each line.
    /// 1-indexed
    /// </summary>
    public Dictionary<int, List<TestInfo>> LineToTestMapping { get; }

    public SourceCodeFileContainer(DocumentId documentId, SyntaxTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree.FilePath);

        SyntaxTree = tree;
        DocumentId = documentId;
        Path = tree.FilePath;
        LinesToMutate = new FileLineCollection(tree);
        LineToTestMapping = new Dictionary<int, List<TestInfo>>();
    }
}
