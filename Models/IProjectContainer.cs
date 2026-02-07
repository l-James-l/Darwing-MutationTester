using Microsoft.CodeAnalysis;
using Models.Enums;

namespace Models;

public interface IProjectContainer
{
    ProjectId ID { get; }

    string Name { get; }

    string CsprojFilePath { get; }

    string DirectoryPath { get; }

    string AssemblyName { get; }

    string DllFilePath { get; }

    ProjectType ProjectType { get; set; }

    Dictionary<DocumentId, SyntaxTree> UnMutatedSyntaxTrees { get; }

    Dictionary<string, DocumentId> DocumentsByPath { get; }

    Compilation? GetCompilation();

    void UpdateFromMutatedProject(Project proj);
}
