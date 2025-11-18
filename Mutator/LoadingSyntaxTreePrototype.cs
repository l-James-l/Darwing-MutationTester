using Buildalyzer;
using Buildalyzer.Environment;
using Buildalyzer.Workspaces;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CSharp;
using Microsoft.Extensions.Options;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;


public class LoadingSyntaxTreePrototype
{
    private Dictionary<SyntaxNode, SyntaxNode> _mutatedTrees = new();
    private Dictionary<string, string> _mutatedDlls = new();

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
        AdhocWorkspace slnWorkspace = analyzerManager.GetWorkspace();

        Console.WriteLine($"Solution loaded: {analyzerManager.SolutionFilePath}");

        //foreach (Project project in slnWorkspace.CurrentSolution.Projects)
        //{
        //    Console.WriteLine("Creating initial build for " + project.AssemblyName);
        //    //BuildProject(project);
        //    BuildProjsingInbuildCompiler(project);
        //}

        RunInitialBuild(analyzerManager);

        foreach ((string id, IProjectAnalyzer analyzer) in analyzerManager.Projects.ToList())
        {
            if (analyzer.ProjectInSolution.ProjectName != "Project")
            {
                Console.WriteLine("Skipping: " + analyzer.ProjectInSolution.ProjectName);
                //analyzer.Build();  -- this will delete the dll that we just built in the initial build step
                continue;
            }
            Console.WriteLine("Ananlyzing: " + analyzer.ProjectInSolution.ProjectName);

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

            //TraverseSyntaxNode(x.root);
            //SyntaxNode mutatedRoot = x.root.ReplaceNodes(_mutatedTrees.Keys, (x, _) => _mutatedTrees[x]);
            //foreach ((SyntaxNode oldNode, SyntaxNode newNode) in _mutatedTrees)
            //{
            //    x.root = x.root.ReplaceNode(oldNode, newNode);
            //}
            SyntaxNode mutatedRoot = TraverseV2(x.root);
            Console.Write(mutatedRoot.ToFullString());

            DocumentId documentId = slnWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(files[0]).First();
            Solution solution = slnWorkspace.CurrentSolution.WithDocumentSyntaxRoot(documentId, /*x.root*/ mutatedRoot);
            Project project = solution.Projects.FirstOrDefault(x => x.Name == analyzer.ProjectInSolution.ProjectName) ?? throw new Exception();
            slnWorkspace.TryApplyChanges(solution);

            //CSharpCompilation.Create(AssemblyName,
            //syntaxTrees.ToList(),
            //_input.SourceProjectInfo.AnalyzerResult.LoadReferences(),
            //analyzerResult.GetCompilationOptions());

            //CSharpGeneratorDriver.Create().RunGeneratorsAndUpdateCompilation(
            //    projWorkspace.CurrentSolution.GetProject(documentId.ProjectId)!.GetCompilationAsync().Result as CSharpCompilation ?? throw new Exception(),
            //    ImmutableArray.Create<CSharpSourceGenerator>(new CSharpMutationGenerator()),
            //    out CSharpCompilation updatedCompilation);  
            //analyzer.add(projWorkspace);
            //IAnalyzerResults analyzerResults = analyzer.Build();

            //AnalyzerResult analyzerResult = analyzerResults.First() as AnalyzerResult ?? throw new Exception();
            //analyzerResult.AdditionalFiles.ToList();
            //Console.WriteLine(x.root.ToFullString());

            //analyzer.Build(); //--this will delete the dll that we just built in the initial build step

            BuildProject(project);
        }

        //foreach (ProjectId projectId in slnWorkspace.CurrentSolution.ProjectIds)
        //{
        //    Task.Run(() => BuildProject(slnWorkspace, projectId));
        //}
        //foreach (Project project in slnWorkspace.CurrentSolution.Projects)
        //{
        //    //BuildProject(project);
        //    BuildProjsingInbuildCompiler(project);
        //}

        //Parallel.ForEach(slnWorkspace.CurrentSolution.Projects, async project =>
        //{
        //    BuildProject(project);
        //});

        RestoreDependenciesToMutatedProjects(analyzerManager, slnWorkspace);
    }

    private void RestoreDependenciesToMutatedProjects(AnalyzerManager analyzerManager, AdhocWorkspace slnWorkspace)
    {
        Console.WriteLine("Restoring mutated DLLs to dependent output directories...");

        foreach (Project project in slnWorkspace.CurrentSolution.Projects)
        {
            string outputFile = project.OutputFilePath ?? throw new Exception("Output file path is null.");
            string outputDir = Path.GetDirectoryName(outputFile) ?? throw new Exception("Could not establish the output directory");
            foreach ((string dllName, string dllPath) in _mutatedDlls)
            {
                if (outputFile == dllPath)
                {
                    // Skip copying to self
                    continue;
                }

                if (File.Exists(Path.Combine(outputDir, dllName)))
                {
                    string destinationPath = Path.Combine(outputDir, dllName);
                    File.Copy(dllPath, destinationPath, true);

                    Console.WriteLine($"Copied mutated DLL {dllName} to {destinationPath}");
                }
            }
        }
    }

    private void BuildProject(Project project)
    {
        try
        {
            Console.WriteLine($"Attempting compilation for {project.Name}");

            Compilation? compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            if (compilation == null)
            {
                throw new Exception($"Compilation could not be created for project {project.Name}.");
            }

            Console.WriteLine($"Compilation created for project {project.Name}.");

            string path = project.OutputFilePath ?? throw new Exception("Output file path is null.");
            string folderPath = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
            string fileName = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            EmitResult emitResult = compilation.Emit(path);

            if (!emitResult.Success) 
            {
                throw new Exception($"Compilation failed for project {project.Name}. {emitResult.Diagnostics}");
            }

            _mutatedDlls.Add(fileName, path);

            Console.WriteLine($"Compilation completion status: {(emitResult.Success ? "Success" : "Failure")}");

        }
        catch (Exception ex)
        {
            throw new Exception($"Error during compilation emit: {ex.Message}", ex);
        }
        Console.WriteLine($"Finished compilation attempt for project {project.Name}.");
    }

    private void TraverseSyntaxNode(SyntaxNode node)
    {
        foreach (var child in node.ChildNodes())
        {
            if (child.ChildNodes().Any())
            {
                child.GetTrailingTrivia();
                if (child is MethodDeclarationSyntax methodNode)
                {
                    //Console.WriteLine($"Method: {methodNode.Identifier}");
                }
                else if (child is BinaryExpressionSyntax binaryExp)
                {
                    if (child.IsKind(SyntaxKind.SubtractExpression))
                    {
                        //Console.WriteLine($"Subtract Node Kind Type: {child.GetType()}");

                        BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
                            binaryExp.Left,
                            binaryExp.Right);


                        _mutatedTrees.Add(binaryExp, newSyntaxNode);
                    }
                    else if (child.IsKind(SyntaxKind.AddExpression))
                    {
                        //Console.WriteLine($"Subtract Node Kind Type: {child.GetType()}");

                        BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
                            binaryExp.Left,
                            binaryExp.Right);


                        _mutatedTrees.Add(binaryExp, newSyntaxNode);
                    }
                    else
                    {
                        //Console.WriteLine($"Node: {child.Kind()}");
                    }
                }
                else
                {
                    //Console.WriteLine($"Node: {child.Kind()}");
                }
                TraverseSyntaxNode(child);
            }
            else
            {
                //Console.WriteLine($"Leaf Node: {child.Kind()} - {child.ToFullString().Trim()}");
            }
        }
    }

    private SyntaxNode TraverseV2(SyntaxNode node)
    {
        Dictionary<SyntaxNode, SyntaxNode> mutatedChildren = new Dictionary<SyntaxNode, SyntaxNode>();
        foreach (SyntaxNode child in node.ChildNodes())
        {
            SyntaxNode nodePostTraversal = TraverseV2(child);
            mutatedChildren.Add(child, nodePostTraversal);
        }
        node = node.ReplaceNodes(node.ChildNodes(), (x, _) => mutatedChildren[x]);

        if (node is BinaryExpressionSyntax binaryExp)
        {
            if (binaryExp.IsKind(SyntaxKind.SubtractExpression))
            {
                //Console.WriteLine($"Subtract Node Kind Type: {child.GetType()}");

                BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
                    binaryExp.Left,
                    binaryExp.Right);

                return newSyntaxNode;
                //node = binaryExp.Parent?.ReplaceNode(binaryExp, newSyntaxNode) ?? node;
            }
            else if (binaryExp.IsKind(SyntaxKind.AddExpression))
            {
                //Console.WriteLine($"Subtract Node Kind Type: {child.GetType()}");

                BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
                    binaryExp.Left,
                    binaryExp.Right);

                return newSyntaxNode;
                //node = binaryExp.Parent?.ReplaceNode(binaryExp, newSyntaxNode) ?? node;
            }
        }

        // Unmuated node.
        return node;
    }

    private (SyntaxTree, SyntaxNode)  GetSyntaxTreeFromCsFilePath(string path)
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

    public void RunTestsCmd()
    {
        // dotnet test "C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln" --logger "trx;LogFileName=test_results.trx"
        Console.WriteLine("Running tests via command line...");

        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "test \"C:\\Users\\THINKPAD\\Documents\\git\\SimpleTestProject\\SimpleTestProject.sln\" --no-build -c \"Debug\"",
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,

            }
        };
        proc.Start();
        while (!proc.HasExited)
        {
            if (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
        }
        
        proc.WaitForExit();
        while(proc.StandardError.ReadLine() is { } error)
        {
            Console.WriteLine($"Error encountered: {error}");
        }
        
        Console.WriteLine("Tests completed.");
    }

    public void RunInitialBuild(AnalyzerManager analyzerManager)
    {
        Console.WriteLine("Performing initial build");

        foreach (var proj in analyzerManager.Projects)
        {
            Console.WriteLine("Building project: " + proj.Value.ProjectInSolution.ProjectName);
            string path = proj.Value.ProjectFile.Path;

            var cleaningProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"clean {Path.GetFileName(path)}",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    //RedirectStandardOutput = true,
                    WorkingDirectory = Path.GetDirectoryName(path)
                }
            };
            cleaningProcess.Start();
            cleaningProcess.WaitForExit();

            var buildingProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build {Path.GetFileName(path)}",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Path.GetDirectoryName(path)
                }
            };
            buildingProcess.Start();

            List<string> outputs = new List<string>();
            while (buildingProcess.StandardOutput.ReadLine() is { } output)
            {
                outputs.Add(output);
                Console.WriteLine($"{output}");
            }

            buildingProcess.WaitForExit();
            while (buildingProcess.StandardError.ReadLine() is { } error)
            {
                Console.WriteLine($"Error encountered during initial build: {error}");
            }
            if (buildingProcess.ExitCode == 0)
            {
                Console.WriteLine($"Initial build succeeded for project: {proj.Value.ProjectInSolution.ProjectName}");
            }
            else
            {
                Console.WriteLine($"Initial build failed for project: {proj.Value.ProjectInSolution.ProjectName} with exit code {buildingProcess.ExitCode}");
            }
        }
        
        
        Console.WriteLine("Initial build complete completed.");
    }

    public void RunCli()
    {
        //dotnet CLI/ bin\Debug\net8.0\CLI.dll

        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = @"CLI\bin\Debug\net8.0\CLI.dll",
            }
        };
        proc.Start();

    }

    public void BuildProjsingInbuildCompiler(Project proj)
    {
        CSharpCodeProvider codeProvider = new CSharpCodeProvider();

        CompilerParameters parameters = new CompilerParameters();
        parameters.GenerateExecutable = false;
        parameters.OutputAssembly = proj.OutputFilePath;
        //CompilerResults results = codeProvider.CreateCompiler().(parameters, proj.FilePath);
    }
}
