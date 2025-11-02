using Buildalyzer;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;


public class LoadingSyntaxTreePrototype
{
    private Dictionary<SyntaxNode, SyntaxNode> _mutatedTrees = new();

    public async Task LoadSyntaxTree()
    {
        string targetPath = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";

        //VisualStudioInstance[] visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
        //VisualStudioInstance? instance = visualStudioInstances.FirstOrDefault(vi => vi.Version == visualStudioInstances.Max(v => v.Version));


        MSBuildWorkspace workspace;
        try
        {
            //MSBuildLocator.RegisterDefaults();
            workspace = MSBuildWorkspace.Create();
            Solution sln = await workspace.OpenSolutionAsync(targetPath);
            sln.GetProjectDependencyGraph();

            Console.WriteLine($"Solution loaded: {sln.FilePath}");
            foreach (Project project in sln.Projects)
            {
                Console.WriteLine(project.AssemblyName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating MSBuildWorkspace: {ex.Message}");
            throw;
        }
        finally
        {
            Console.WriteLine("MSBuildWorkspace creation attempted.");
        }
    }

    public void LoadSlnUsingBuildAnalyzer()
    {
        string targetPath = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";

        AnalyzerManager analyzerManager = new AnalyzerManager(targetPath);

        Console.WriteLine($"Solution loaded: {analyzerManager.SolutionFilePath}");
        foreach ((string id, IProjectAnalyzer analyzer) in analyzerManager.Projects.ToList()[..1])
        {
            Console.WriteLine(analyzer.ProjectInSolution.ProjectName);

            string projectDirPath = id.Remove(id.Count() - analyzer.ProjectFile.Name.Count());

            EnumerationOptions enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            enumerationOptions.IgnoreInaccessible = true;
            enumerationOptions.ReturnSpecialDirectories = false;
            enumerationOptions.AttributesToSkip = FileAttributes.System | FileAttributes.Hidden | FileAttributes.Compressed | FileAttributes.Temporary | FileAttributes.ReadOnly;

            List<string> files = Directory.EnumerateFiles(projectDirPath, "*.cs", enumerationOptions).ToList();
            //files.ForEach(GetSyntaxTreeFromCsFilePath);
            (SyntaxTree tree, SyntaxNode root) x = GetSyntaxTreeFromCsFilePath(files[0]); // For testing
            //(SyntaxTree tree, SyntaxNode root) y = GetSyntaxTreeFromCsFilePath(files[^1]); // For testing 
            //y.root.DescendantTrivia();
            TraverseSyntaxNode(x.root);

            foreach ((SyntaxNode oldNode, SyntaxNode newNode) in _mutatedTrees)
            {
                x.root = x.root.ReplaceNode(oldNode, newNode);
            }

            Console.WriteLine(x.root.ToFullString());
        }
    }

    private void TraverseSyntaxNode(SyntaxNode node)
    {
        foreach (var child in node.ChildNodes())
        {
            if (child.ChildNodes().Any())
            {
                if (child is MethodDeclarationSyntax methodNode)
                {
                    Console.WriteLine($"Method: {methodNode.Identifier}");
                }
                else if (child.IsKind(SyntaxKind.SubtractExpression) && 
                    child is BinaryExpressionSyntax binaryExp)
                {
                    Console.WriteLine($"Subtract Node Kind Type: {child.GetType()}");

                    BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
                        binaryExp.Left,
                        binaryExp.Right);

                    
                    _mutatedTrees.Add(binaryExp, newSyntaxNode);
                }
                else
                {
                    Console.WriteLine($"Node: {child.Kind()}");
                }
                TraverseSyntaxNode(child);
            }
            else
            {
                Console.WriteLine($"Leaf Node: {child.Kind()} - {child.ToFullString().Trim()}");
            }
        }
    }

    private (SyntaxTree, SyntaxNode) GetSyntaxTreeFromCsFilePath(string path)
    {
        Console.WriteLine($"Processing file: {path}");  

        string code = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();

        return (tree, root);
    }

    public void LoadStringifiedCode()
    {
        var code = @"
        using System;
        namespace HelloWorld
        {
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(""Hello, World!"");
                }
            }
        }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        Console.WriteLine(root.ToFullString());
    }

    private void SyntaxKindSwitch()
    {
        SyntaxKind x = SyntaxKind.ClassDeclaration;
        switch (x)
        {
            case SyntaxKind.ClassDeclaration:
                // Handle class declaration
                break;
            case SyntaxKind.MethodDeclaration:
                // Handle method declaration
                break;
            case SyntaxKind.SubtractExpression:
            // Add more cases as needed
            default:
                // Handle other kinds
                break;
        }
    }
}
