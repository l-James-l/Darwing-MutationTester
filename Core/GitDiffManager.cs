using Core.IndustrialEstate;
using Core.Interfaces;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using Serilog;
using System.IO.Abstractions;

namespace Core;

/// <summary>
/// Used to establish files/lines that have changed, so that we can mutate only those sections.
/// 
/// We always do this from the working directory, and we can compare against either the head, or a specific branch.
/// </summary>
public sealed class GitDiffManager : IGitDiffManager, IDisposable
{
    private readonly ISolutionProvider _solutionProvider;
    private readonly IMutationSettings _settings;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IFileSystem _fileSystem;
    private IRepository? _repo = null;

    public GitDiffManager(ISolutionProvider solutionProvider, IMutationSettings settings, IEventAggregator eventAggregator,
        IRepositoryFactory repositoryFactory, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(settings);

        _solutionProvider = solutionProvider;
        _settings = settings;
        _eventAggregator = eventAggregator;
        _repositoryFactory = repositoryFactory;
        _fileSystem = fileSystem;
    }

    public List<string> Branches => [.._repo?.Branches.Select(x => x.FriendlyName) ?? []];

    public string? LastSelectedBranch { get; private set; } = null;

    public void InitialGitDiff()
    {
        string wouldBeGitPath = Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git");
        if (!_fileSystem.Directory.Exists(wouldBeGitPath) && !_fileSystem.File.Exists(wouldBeGitPath))
        {
            Log.Information("No git repository detected at solution path, skipping diff.");
            LastSelectedBranch = null;
            _eventAggregator.GetEvent<GitUpdateEvent>().Publish();
            return;
        }
        Log.Information("Git folder found, attempting to load repository.");

        try
        {
            _repo = _repositoryFactory.Get(wouldBeGitPath);
            Log.Information("Git repository loaded successfully. Branches found: {branches}", Branches);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load git repository at path: {wouldBeGitPath}. {ex}");
            _repo = null;
            LastSelectedBranch = null;
            _eventAggregator.GetEvent<GitUpdateEvent>().Publish();
            return;
        }
        EstablishDiff(_settings.DefaultGitComparisonBranch);
    }

    public void EstablishDiff(string compareBranch) 
    {
        if (_repo is null)
        {
            Log.Warning("Command to establish a git diff received but no repo has been loaded.");
            LastSelectedBranch = null;
            _eventAggregator.GetEvent<GitUpdateEvent>().Publish();
            return;
        }

        Log.Information($"Loading diff to branch {compareBranch}");
        Patch? patch = TryGetPatch(FindBranch(compareBranch));
        if (patch is null)
        {
            Log.Warning("Couldn't establish a git diff to {branch}.", compareBranch);
            LastSelectedBranch = null;
            _eventAggregator.GetEvent<GitUpdateEvent>().Publish();
            return;
        }

        LastSelectedBranch = compareBranch;
        SetLinesToMutateFromPatch(patch);
    }

    private Branch? FindBranch(string name)
    {
        Branch? branch = _repo?.Branches.FirstOrDefault(x => x.FriendlyName == name);
        if (branch is null)
        {
            Log.Warning("Couldn't find branch {branch}", name);
        }
        return branch;
    }

    private Patch? TryGetPatch(Branch? branch)
    {
        if (branch is null)
        {
            return null;
        }
        try
        {
            Patch? patch = _repo?.Diff.Compare<Patch>(branch.Tip.Tree, DiffTargets.WorkingDirectory);
            return patch;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get git diff.");
            return null;
        }
    }

    private void SetLinesToMutateFromPatch(Patch patch)
    {
        // We now know that we have a valid diff. So clear the default selection (everything will be selected)
        _solutionProvider.SolutionContainer.SolutionProjects.ForEach(x => x.FileCollection.ForEach(y => y.LinesToMutate.Clear()));
        
        foreach (PatchEntryChanges change in patch)
        {
            string path = Path.GetFullPath(Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, change.Path));
            SourceCodeFileContainer? file = _solutionProvider.SolutionContainer.FindFile(path, ProjectType.Source);
            if (file is null)
            {
                if (path.EndsWith(".cs"))
                {
                    Log.Warning($"File in diff not found in solution. {path}");
                }
                continue;
            }

            Log.Information("Diff in {file}", file.Path);
            foreach (Line addition in change.AddedLines)
            {
                file.LinesToMutate.Add(addition.LineNumber-1);
                Log.Debug("{n}: {c}", addition.LineNumber, addition.Content.ToString().ReplaceLineEndings(""));
            }
        }

        _eventAggregator.GetEvent<GitUpdateEvent>().Publish();
    }

    public void Dispose()
    {
        _repo?.Dispose();
    }
}
