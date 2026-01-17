using Buildalyzer;
using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Serilog;

namespace Core;

/// <summary>
/// This class will handle performing actions required as soon as a solution path has been provided.
/// This includes:
///     - Locating the solution, and initializing the solution container
///     - Checking for a solution profile
///     - Performing an initial build
/// </summary>
public class SolutionLoader : ISolutionLoader, ISolutionProvider
{
    private readonly IAnalyzerManagerFactory _analyzerManagerFactory;
    private readonly ISolutionProfileDeserializer _slnProfileDeserializer;
    private readonly IMutationSettings _mutationSettings;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly IStatusTracker _statusTracker;

    //By making the public property the interface, we can mock the solution in testing.
    public ISolutionContainer SolutionContainer => _solutionContainer ?? throw new InvalidOperationException("Attempted to retrieve a solution before one has been loaded.");
    private SolutionContainer? _solutionContainer;

    public bool IsAvailable => _solutionContainer != null;

    public SolutionLoader(IAnalyzerManagerFactory analyzerManagerFactory, ISolutionProfileDeserializer slnProfileDeserializer,
        IMutationSettings mutationSettings, ISolutionBuilder solutionBuilder, IStatusTracker statusTracker)
    {
        ArgumentNullException.ThrowIfNull(analyzerManagerFactory);
        ArgumentNullException.ThrowIfNull(slnProfileDeserializer);
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(solutionBuilder);
        ArgumentNullException.ThrowIfNull(statusTracker);

        _analyzerManagerFactory = analyzerManagerFactory;
        _slnProfileDeserializer = slnProfileDeserializer;
        _mutationSettings = mutationSettings;
        _solutionBuilder = solutionBuilder;
        _statusTracker = statusTracker;
    }

    public void Load(string solutionPath)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        Log.Information("Received solution path: {path}", solutionPath);

        if (!_statusTracker.TryStartOperation(DarwingOperation.LoadSolution))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(solutionPath) || !solutionPath.EndsWith(".sln") || !File.Exists(solutionPath))
        {
            Log.Error($"Solution file not found at location: {solutionPath}");
            _mutationSettings.SolutionPath = "";
            _solutionContainer = null;
        }
        else
        {
            _mutationSettings.SolutionPath = solutionPath;

            TryCreateManager(solutionPath);
        }

        if (_solutionContainer is not null)
        {
            //Do this outside the try catch so that errors caught are only for loading the solution.
            //Deserializer shall handle its own exceptions.
            _slnProfileDeserializer.LoadSlnProfileIfPresent(solutionPath);
            _solutionContainer.FindTestProjects(_mutationSettings);
            DiscoverSourceCodeFiles();
            _statusTracker.FinishOperation(DarwingOperation.LoadSolution, IsAvailable);
            _solutionBuilder.InitialBuild();
        }
        else
        {
            _statusTracker.FinishOperation(DarwingOperation.LoadSolution, false);
        }
    }

    private void TryCreateManager(string path)
    {
        try
        {
            Log.Information("Creating analyzer for solution.");
            IAnalyzerManager analyzerManager = _analyzerManagerFactory.CreateAnalyzerManager(path);
            _solutionContainer = new SolutionContainer(analyzerManager);
        }
        catch (Exception ex)
        {
            _solutionContainer = null;
            _mutationSettings.SolutionPath = "";
            Log.Error("Failed to load solution.");
            Log.Debug($"Failed to create AnalyzerManager for solution at location: {path}. {ex}");
        }
    }

    private void DiscoverSourceCodeFiles()
    {
        ArgumentNullException.ThrowIfNull(_solutionContainer);

        EnumerationOptions enumerationOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden | FileAttributes.Compressed | FileAttributes.Temporary | FileAttributes.ReadOnly
        };

        foreach (IProjectContainer project in _solutionContainer.SolutionProjects)
        {
            Log.Information($"Loading source code files for {project.Name}");

            List<string> files = Directory.EnumerateFiles(project.DirectoryPath, "*.cs", enumerationOptions).ToList();
            files.RemoveAll(path => 
                path.StartsWith(Path.Combine(project.DirectoryPath, "obj")) ||
                path.StartsWith(Path.Combine(project.DirectoryPath, "bin")));

            foreach (string file in files)
            {
                Log.Information($"Discovered: {file}");
                SyntaxTree syntaxTree = GetSyntaxTree(file);

                _solutionContainer.Solution.GetDocumentId(syntaxTree);
                if (_solutionContainer.Solution.GetDocumentId(syntaxTree) is { } documentId)
                {
                    project.UnMutatedSyntaxTrees.Add(documentId, syntaxTree);
                    project.DocumentsByPath.Add(file, documentId);
                }
                else if (_solutionContainer.Solution.GetDocumentIdsWithFilePath(syntaxTree.FilePath) is { Length: 1 } documentIds)
                {
                    project.UnMutatedSyntaxTrees.Add(documentIds.First(), syntaxTree);
                    project.DocumentsByPath.Add(file, documentIds.First());
                }
                else
                {
                    Log.Error($"Unable to find document ID for {syntaxTree.FilePath}. This file will not be mutated.");
                }
            }
        }
    }

    private SyntaxTree GetSyntaxTree(string path)
    {
        string code = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        return tree.WithFilePath(path);
    }
}
