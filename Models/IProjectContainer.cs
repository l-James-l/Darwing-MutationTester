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

    public SourceCodeFileCollection FileCollection { get; }

    Compilation? GetCompilation();

    void UpdateFromMutatedProject(Project proj);
}
