using Core.Interfaces;
using Models;
using Mutator;
using Serilog;
using System.Xml;

namespace Core;

/// <summary>
/// Class responsible for reading a altcover coverage report and translating it into a useable line to test coverage mapping
/// </summary>
public class CoverageMapper : ICoverageMapper
{
    private readonly ISolutionProvider _solutionProvider;

    public CoverageMapper(ISolutionProvider solutionProvider)
    {
        _solutionProvider = solutionProvider;
    }

    /// <inheritdoc/>
    public bool MapFullCoverage(string xmlPath)
    {
        // Due to the size of the file potentially being pretty large (5MB for a small project) thanks to the line by line coverage, we use a reader rather than
        // loading the whole document.
        // Due to the structure of the xml (schema snippet below) we do 2 passes over the report, first to get the ID mappings for files and tests,
        // Then another pass to get the tests that map to each line.

        try
        {
            MapCoverageInternal(xmlPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Exception occurred while reading coverage report {xmlPath}.");
            return false;
        }
        return true;
    }

    private void MapCoverageInternal(string xmlPath)
    {
        XmlReaderSettings settings = new()
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        var testIdNameMap = new Dictionary<int, TestInfo>();
        var fileIdNameMap = new Dictionary<string, string>();

        // pass 1, build maps
        using (XmlReader reader = XmlReader.Create(xmlPath, settings))
        {
            IProjectContainer? currentProject = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "ModuleName":
                            string readString = reader.ReadString();
                            currentProject = _solutionProvider.SolutionContainer.TestProjects.Find(x => x.Name == readString);
                            break;
                       
                        case "File":
                            fileIdNameMap[reader.GetAttribute("uid")!] = reader.GetAttribute("fullPath")!;
                            break;
                       
                        case "TrackedMethod" when currentProject is not null: 
                            string name = CleanTestName(reader.GetAttribute("name")!);

                            long entry = long.Parse(reader.GetAttribute("entry") ?? "0");
                            long exit = long.Parse(reader.GetAttribute("exit") ?? "0");

                            TimeSpan duration = TimeSpan.FromTicks(exit - entry);

                            testIdNameMap[int.Parse(reader.GetAttribute("uid")!)] = new TestInfo(currentProject, name, duration);
                            Log.Debug($"Tracked Test found. {name}, in {currentProject.Name}. Has Duration: {duration.TotalSeconds} seconds");
                            break;

                        case "TrackedMethod":
                            Log.Error("Tracked method encountered with null current project.");
                            break;
                    }
                }
            }
        }

        // pass 2, find lines to test mappings
        using (XmlReader reader = XmlReader.Create(xmlPath, settings))
        {
            IProjectContainer? currentProject = null;
            SourceCodeFileContainer? currentFile = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "ModuleName": // The Project Name
                            string readString = reader.ReadString();
                            currentProject = _solutionProvider.SolutionContainer.SolutionProjects.Find(x => x.Name == readString);
                            currentFile = null;
                            break;

                        case "FileRef" when currentProject is not null: // Tells us which file the following methods belong to
                            string? currentFileId = reader.GetAttribute("uid");
                            if (currentFileId is not null && fileIdNameMap.TryGetValue(currentFileId, out string? fileName) &&
                                currentProject.FileCollection.TryGetValue(fileName, out SourceCodeFileContainer? file))
                            {
                                currentFile = file;
                            }
                            else
                            {
                                currentFile = null;
                            }
                            break;

                        case "SequencePoint" when currentFile is not null:
                            ProcessSequencePoint(reader, currentFile, testIdNameMap);
                            break;
                    }
                }
            }
        }
    }

    private void ProcessSequencePoint(XmlReader reader, SourceCodeFileContainer file, Dictionary<int, TestInfo> testIdNameMap)
    {
        int lineNo = int.Parse(reader.GetAttribute("sl")!);

        if (!reader.IsEmptyElement)
        {
            using XmlReader subReader = reader.ReadSubtree();
            while (subReader.Read())
            {
                if (subReader.Name == "TrackedMethodRef")
                {
                    int uid = int.Parse(subReader.GetAttribute("uid")!);
                    if (testIdNameMap.TryGetValue(uid, out TestInfo? test))
                    {
                        file.LineToTestMapping.AddOrCreate(lineNo, test);
                        Log.Debug($"Associated {test.RelativePath} with line {lineNo} in {file.Path}.");
                    }
                }
            }
        }
    }

    private string CleanTestName(string nameFromXml)
    {
        // Converts: "System.Void Project.Tests::MyTest(int x)" to: "Project.Tests.MyTest"
        if (string.IsNullOrWhiteSpace(nameFromXml))
        {
            return string.Empty;
        }

        int lastSpace = nameFromXml.LastIndexOf(' ');
        string nameWithoutReturnType = lastSpace != -1 ? nameFromXml[(lastSpace + 1)..] : nameFromXml;

        string dotNormalized = nameWithoutReturnType.Replace("::", ".");

        int parenIndex = dotNormalized.IndexOf('(');
        return parenIndex != -1 ? dotNormalized[..parenIndex] : dotNormalized;
    }


    // Example report Schema:
    //
    //<CoverageSession>
    //  <Modules>
    //    <Module>
    //      <ModuleName>ProjectName</ModuleName>
    //      <Files>
    //        <File uid = "1" fullPath="C:\...\FileName.cs" />
    //      </Files>

    //      <Classes>
    //        <Class>
    //          <Methods>
    //            <Method name = "MethodName" >
    //              < FileRef uid="1" /> 

    //              <SequencePoints>
    //                <SequencePoint vc = "2" sl="21">
    //                  <TrackedMethodRefs>
    //                    <TrackedMethodRef uid = "4" vc="1" />
    //                  </TrackedMethodRefs>
    //                </SequencePoint>
    //              </SequencePoints>
    //            </Method>
    //          </Methods>
    //        </Class>
    //      </Classes>
    //    </Module>
    //  </Modules>

    //  <TrackedMethods>
    //    <TrackedMethod uid = "4" name="ReturnType ProjectTests.TestFile::TestName(TestCase Params)" />
    //  </TrackedMethods>
    //</CoverageSession>
}