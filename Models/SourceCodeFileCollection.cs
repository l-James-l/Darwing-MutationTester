using Microsoft.CodeAnalysis;
using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Models;

/// <summary>
/// Class to contain all the source code files in a project.
/// Enumerable class that allows looking up files by either path or id
/// </summary>
public class SourceCodeFileCollection : IEnumerable<SourceCodeFileContainer>
{
    private Dictionary<string, SourceCodeFileContainer> _documentsByPath = new();

    private Dictionary<DocumentId, SourceCodeFileContainer> _documentsById = new();

    public void AddDocument(DocumentId documentId,  SyntaxTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree.FilePath);
        if (_documentsByPath.ContainsKey(tree.FilePath) || _documentsById.ContainsKey(documentId))
        {
            throw new DuplicateNameException("Tried to add duplicate file to collection.");
        }

        SourceCodeFileContainer file = new (documentId, tree);
        
        _documentsByPath.Add(tree.FilePath, file);
        _documentsById.Add(documentId, file);
    }

    public bool TryGetValue(string path, [NotNullWhen(true)] out SourceCodeFileContainer? file)
    {
        if (_documentsByPath.TryGetValue(path, out file))
        {
            return true;
        }
        return false;
    }

    public bool TryGetValue(DocumentId document, [NotNullWhen(true)] out SourceCodeFileContainer? file)
    {
        if (_documentsById.TryGetValue(document, out file))
        {
            return true;
        }
        return false;
    }

    public IEnumerator<SourceCodeFileContainer> GetEnumerator() => _documentsById.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
