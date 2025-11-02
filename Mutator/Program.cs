using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

static class Program
{
    public static void Main(string[] args)
    {
        //This is the entry point for the backend applicaiton, and should be responsible for:
        //  Registering all DI
        //  Taking in command line args relating to what projects/solutions the tool will run for
        //  Calling the class that will handle initating the process of mutation testing

        Console.WriteLine("Hello world");


        LoadingSyntaxTreePrototype prototype = new LoadingSyntaxTreePrototype();
        try
        {
            prototype.LoadSlnUsingBuildAnalyzer();
            //prototype.LoadSyntaxTree();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

    }
}
