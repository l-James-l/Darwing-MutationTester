using Microsoft.CodeAnalysis;

namespace Models;

public interface IProjectContainer
{
    ProjectId ID { get; }

    string Name { get; }

    string CsprojFilePath { get; }

    string DirectoryPath { get; }

    string AssemblyName { get; }

    string DllFilePath { get; }

    bool IsTestProject { get; }

    Dictionary<DocumentId, SyntaxTree> UnMutatedSyntaxTrees { get; }

    Dictionary<string, DocumentId> DocumentsByPath { get; }

    Compilation? GetCompilation();

    void UpdateFromMutatedProject(Project proj);
}
