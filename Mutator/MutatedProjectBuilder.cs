using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Models;
using Models.Enums;
using Models.Events;
using Serilog;

namespace Mutator;

public class MutatedProjectBuilder : IStartUpProcess
{
    private readonly IMutationDiscoveryManager _mutationDiscovery;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISolutionProvider _solutionProvider;

    public MutatedProjectBuilder(IMutationDiscoveryManager mutationDiscovery, IEventAggregator eventAggregator, ISolutionProvider solutionProvider)
    {
        _mutationDiscovery = mutationDiscovery;
        _eventAggregator = eventAggregator;
        _solutionProvider = solutionProvider;
    }

    public void StartUp()
    {
        _eventAggregator.GetEvent<BuildMutatedSolutionEvent>().Subscribe(EmitAllChanges);
    }

    public void EmitAllChanges()
    {
        const int maxRetrys = 5;
        
        foreach (IProjectContainer project in _solutionProvider.SolutionContiner.SolutionProjects)
        {
            int retryCount = 0;
            bool doFinalRetry = false;
            bool dllCreated = false;
            while (retryCount < maxRetrys && !doFinalRetry)
            {
                retryCount++;
                dllCreated = EmitMutatedDll(project, out List<Diagnostic> failures);
                if (!dllCreated)
                {
                    bool anyFailuresActioned = FindAndRemoveMutationsCausingFailures(project, failures);
                    if (!anyFailuresActioned)
                    {
                        Log.Warning("Build encountered errors that could not be resolved. Will make 1 final attempt.");
                        doFinalRetry = true;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            if (!dllCreated && doFinalRetry)
            {
                if (EmitMutatedDll(project, out List<Diagnostic> failures))
                {
                    Log.Information("Final attempt to create DLL for {proj} succeeded.", project.Name);
                }
                else
                {
                    Log.Error("Final attempt to created mutated DLL for {proj} failed. Will be unable to perform mutation testing. Examin logs for details.", project.Name);
                    return;
                }
            }
            else if (!dllCreated)
            {
                Log.Error("Max number of retrys reached for creating a mutated DLL for {proj}. Will be aunable to perform mutation testing.", project.Name);
                return;
            }
        }

        RestoreDependencies();
        _eventAggregator.GetEvent<TestMutatedSolutionEvent>().Publish();
    }

    private bool EmitMutatedDll(IProjectContainer mutatedProject, out List<Diagnostic> failures)
    {
        Log.Information($"Attempting to emit dll for {mutatedProject.Name}");

        failures = [];

        Compilation? compilation = mutatedProject.GetCompilation();
        if (compilation == null)
        {
            Log.Error($"Failed to created compilation for mutated project {mutatedProject.Name}.");
            return false;
        }

        EmitResult emitResult = compilation.Emit(mutatedProject.DllFilePath);
        if (!emitResult.Success)
        {
            Log.Error("Emitting dll for {Name} failed. {Diagnostics}.", mutatedProject.Name, emitResult.Diagnostics);
            failures = emitResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
            return false;
        }

        Log.Information($"Mutated dll succesfully created for {mutatedProject.Name}.");
        return true;
    }

    private void RestoreDependencies()
    {
        // For every project that has a dependency on another project that has been mutated, we need to replace the dll in that project.
        foreach (IProjectContainer project in _solutionProvider.SolutionContiner.AllProjects)
        {
            // Get the folder containg the projects build artifacts, which will include dll's of any other projects.
            string projectOutputDirectory = Path.GetDirectoryName(project.DllFilePath) ?? "";

            foreach (IProjectContainer mutatedProject in _solutionProvider.SolutionContiner.SolutionProjects.Except([project]))
            {
                // Check if the unmutated dll exists in the output directory, and if it does, replace it
                string wouldBeDependency = Path.Combine(projectOutputDirectory, Path.GetFileName(mutatedProject.DllFilePath));
                if (File.Exists(wouldBeDependency))
                {
                    Log.Debug($"Replacing depdency on {mutatedProject.Name} in {project.Name}.");
                    File.Copy(mutatedProject.DllFilePath, wouldBeDependency, true);
                }
            }
        }
    }

    /// <summary>
    /// For each failure in the build diagnostics, we will remove the mutations which was introduced closest to that location.
    /// </summary>
    /// <returns>True if all failures were actioned, False if any failures were unable to be actioned.</returns>
    private bool FindAndRemoveMutationsCausingFailures(IProjectContainer project, List<Diagnostic> failures)
    {
        bool anyActioned = false;
        foreach (Diagnostic failure in failures)
        {
            if (!failure.Location.IsInSource || failure.Location.SourceSpan.IsEmpty)
            {
                Log.Error("failure not located in source code so cannot determine a mutation that caused it. {failure}", failure);
                continue;
            }

            FileLinePositionSpan failureLocation = failure.Location.GetLineSpan();
            if (project.DocumentsByPath.TryGetValue(failureLocation.Path, out DocumentId? document))
            {
                Log.Information("Attempting to address a build failure in file: {path}", failureLocation.Path);

                IEnumerable<DiscoveredMutation> mutationsInFile = _mutationDiscovery.DiscoveredMutations.Where(x => x.Document == document && x.Status >= MutantStatus.Available);

                //We want to find any mutation where the eror occurs partially or entirly inside it
                //We do this by finding all mutations which 'start' before the error ends,
                //then find all the mutations which 'end' after the error starts
                //Then we take the intersection of these, leaving us with mutations that overlap with the error
                IEnumerable<DiscoveredMutation> mutationStartBeforeErrorEnd = 
                    mutationsInFile.Where(mutant => mutant.LineSpan.StartLinePosition.CompareTo(failureLocation.Span.End) <= 0);
                IEnumerable<DiscoveredMutation> mutationsEndAfterErrorStart = 
                    mutationsInFile.Where(x => x.LineSpan.EndLinePosition.CompareTo(failureLocation.Span.Start) >= 0);

                IEnumerable<DiscoveredMutation> mutantsToRemove = mutationStartBeforeErrorEnd.Intersect(mutationsEndAfterErrorStart);

                RemoveMutants(mutantsToRemove.ToList(), document);
                anyActioned = true;

                //TODO maybe it would be better to not remove all the mutants at once,
                //and instead remove the most relevant one first (smallest span entirly inside?), and then if that fails, remove them all?
            }
            else
            {
                Log.Warning("No document found that matches that path specified in the build faliure. {failure}", failureLocation.Path);
                continue;
            }
        }

        return anyActioned;
    }

    private void RemoveMutants(List<DiscoveredMutation> mutantsToRemove, DocumentId document)
    {
        Solution slnWithMutantsRemoved = _solutionProvider.SolutionContiner.Solution;

        while (mutantsToRemove.Count > 0)
        {
            //Take the first mutation which is not contained within another mutation we need to remove.
            DiscoveredMutation? mutant = mutantsToRemove.FirstOrDefault(x => mutantsToRemove.Except([x]).None(y => y.LineSpan.Contains(x.LineSpan)));

            if (mutant is null)
            {
                Log.Warning("Attempted to remove a mutation that couldnt be found");
                mutant = mutantsToRemove.First();
            }
            mutantsToRemove.Remove(mutant);
            foreach (DiscoveredMutation embededMutation in new List<DiscoveredMutation>(mutantsToRemove.Where(x => mutant.LineSpan.Contains(x.LineSpan))))
            {
                //Removing a mutation that contains this one will by default remove this mutant.
                embededMutation.Status = MutantStatus.CausedBuildError;
                mutantsToRemove.Remove(embededMutation);
            }

            mutant.Status = MutantStatus.CausedBuildError;

            SyntaxNode newRoot = mutant.MutatedNode.SyntaxTree.GetRoot().ReplaceNode(mutant.MutatedNode, mutant.OriginalNode);
            
            slnWithMutantsRemoved = slnWithMutantsRemoved.WithDocumentSyntaxRoot(mutant.Document, newRoot);

            _mutationDiscovery.RediscoverMutationsInTree(newRoot);

            Log.Debug(mutant.OriginalNode.SyntaxTree.FilePath + " after removing mutants causing build errors:");
            Log.Debug(newRoot.ToFullString());
        }


        if (!_solutionProvider.SolutionContiner.Workspace.TryApplyChanges(slnWithMutantsRemoved))
        {
            Log.Error("Failed to remove mutants causing errors.");
        }
        _solutionProvider.SolutionContiner.RestoreProjects();
    }
}
