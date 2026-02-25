using Models;
using Models.Events;
using Models.Enums;
using System.Windows;

namespace GUI.ViewModels.SolutionExplorerElements;

/// <summary>
/// View model for a single mutation in the solution explorer side panel 
/// </summary>
public class MutationViewModel: ViewModelBase
{
    private readonly bool _geminiApiConfigured;

    public MutationViewModel(DiscoveredMutation mutation, bool geminiApiConfigured=false)
    {
        Mutation = mutation;
        _geminiApiConfigured = geminiApiConfigured;
        CopyTestCommand = new DelegateCommand(CopyTest);
    }

    /// <summary>
    /// The mutation this VM represents
    /// </summary>
    public DiscoveredMutation Mutation { get; }

    /// <summary>
    /// Binding property for if the panel containing the suggested test be visible.
    /// Shown when a test has been generated.
    /// </summary>
    public Visibility SuggestedTestVisibility 
    { 
        get;
        set => SetProperty(ref field, value);
    } = Visibility.Collapsed;

    /// <summary>
    /// Binding property for if the generate test button should be shown.
    /// Shown when the mutation survived.
    /// Updating this is handled by the subscription to <see cref="MutationUpdated"/> in <see cref="SolutionExplorerViewModel"/>
    /// </summary>
    public Visibility SuggestTestButtonVisibility => _geminiApiConfigured && Mutation.Status.IncludeInSurvivedCount() ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Binding property for if the test generation ongoing progress bar should be visible
    /// </summary>
    public Visibility TestGenerationOngoingVisibility
    {
        get;
        set => SetProperty(ref field, value);
    } = Visibility.Collapsed;

    /// <summary>
    /// Source code for the new suggested test
    /// </summary>
    public string? AiGeneratedUnitTest 
    { 
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Description for the new test
    /// </summary>
    public string? GeneratedUnitTestDescription 
    { 
        get;
        set => SetProperty(ref field, value); 
    }

    /// <summary>
    /// Callback method invoked after generating a test 
    /// </summary>
    public void MutationTestCreatedCallBack(string testBody, string testDesc)
    {
        AiGeneratedUnitTest = testBody;
        GeneratedUnitTestDescription = testDesc;
        SuggestedTestVisibility = Visibility.Visible;
        CopyButtonText = "Copy";
    }

    public DelegateCommand CopyTestCommand { get; }
    private void CopyTest()
    {
        if (AiGeneratedUnitTest is not null)
        {
            Clipboard.SetText(AiGeneratedUnitTest);
            CopyButtonText = "Copied ✔";
        }
    }

    public string CopyButtonText
    {
        get;
        set => SetProperty(ref field, value);
    } = "Copy";
}