using NSubstitute;
using Core;
using Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CoreTests;

public class CoverageMapperTests
{
    private ISolutionProvider _solutionProvider;
    private CoverageMapper _coverageMapper;
    private string _tempXmlPath;

    [SetUp]
    public void SetUp()
    {
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _coverageMapper = new CoverageMapper(_solutionProvider);
        _tempXmlPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempXmlPath))
        {
            File.Delete(_tempXmlPath);
        }
    }

    [Test]
    public void GivenValidAltCoverReport_WhenMapFullCoverageIsCalled_ThenLinesAreMappedToTests()
    {
        // Arrange
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        testProject.Name.Returns("ProjectTests");

        IProjectContainer sourceProject = Substitute.For<IProjectContainer>();
        sourceProject.Name.Returns("ProjectCore");

        SourceCodeFileCollection fileCollection = new();
        fileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("").WithFilePath("C:\\Code\\Logic.cs"));
        sourceProject.FileCollection.Returns(fileCollection);
        var sourceFile = fileCollection.First();

        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        solutionContainer.TestProjects.Returns([testProject]);
        solutionContainer.SolutionProjects.Returns([sourceProject]);
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        // entry/exit use Ticks (10,000,000 per second)
        string xml = @"
<CoverageSession>
  <Modules>
    <Module>
      <ModuleName>ProjectCore</ModuleName>
      <Files>
        <File uid=""1"" fullPath=""C:\Code\Logic.cs"" />
      </Files>
      <Classes>
        <Class>
          <Methods>
            <Method>
              <FileRef uid=""1"" />
              <SequencePoints>
                <SequencePoint vc=""1"" sl=""10"">
                  <TrackedMethodRefs>
                    <TrackedMethodRef uid=""42"" vc=""1"" />
                  </TrackedMethodRefs>
                </SequencePoint>
              </SequencePoints>
            </Method>
          </Methods>
        </Class>
      </Classes>
    </Module>
    <Module>
      <ModuleName>ProjectTests</ModuleName>
      <TrackedMethods>
        <TrackedMethod uid=""42"" 
                       name=""System.Void Project.Tests.Unit::MyTest()"" 
                       entry=""10000000"" 
                       exit=""30000000"" />
      </TrackedMethods>
    </Module>
  </Modules>
</CoverageSession>";
        File.WriteAllText(_tempXmlPath, xml);

        // Act
        bool result = _coverageMapper.MapFullCoverage(_tempXmlPath);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(sourceFile.LineToTestMapping.ContainsKey(10), Is.True);

            TestInfo mappedTest = sourceFile.LineToTestMapping[10].First();
            Assert.That(mappedTest.RelativePath, Is.EqualTo("Project.Tests.Unit.MyTest"));
            Assert.That(mappedTest.Duration.TotalSeconds, Is.EqualTo(2));
        });
    }

    [Test]
    public void GivenMalformedXml_WhenMapFullCoverageIsCalled_ThenReturnsFalse()
    {
        // Arrange
        File.WriteAllText(_tempXmlPath, "<InvalidTag>MissingClosingTag");

        // Act
        var result = _coverageMapper.MapFullCoverage(_tempXmlPath);

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase("System.Void My.Tests::Test1()", "My.Tests.Test1")]
    [TestCase("int My.Tests::Calculate(int)", "My.Tests.Calculate")]
    [TestCase("My.Tests::ShortName", "My.Tests.ShortName")]
    [TestCase("", "")]
    public void GivenVariousTestSignatures_WhenCleanTestNameIsCalled_ThenReturnsNormalizedName(string input, string expected)
    {
        // Arrange
        var method = typeof(CoverageMapper).GetMethod("CleanTestName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = method.Invoke(_coverageMapper, new object[] { input });

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GivenTrackedMethodWithMissingProject_WhenMapFullCoverageIsCalled_ThenLogsErrorAndSkips()
    {
        // Arrange
        var container = Substitute.For<ISolutionContainer>();
        container.TestProjects.Returns([]);
        container.SolutionProjects.Returns([]);
        _solutionProvider.SolutionContainer.Returns(container);

        string xml = @"
<CoverageSession>
  <Modules>
    <Module>
      <ModuleName>Unknown.Tests</ModuleName>
      <TrackedMethods>
        <TrackedMethod uid=""1"" name=""Test()"" entry=""0"" exit=""0"" />
      </TrackedMethods>
    </Module>
  </Modules>
</CoverageSession>";
        File.WriteAllText(_tempXmlPath, xml);

        // Act
        var result = _coverageMapper.MapFullCoverage(_tempXmlPath);

        // Assert
        Assert.That(result, Is.True); // It shouldn't crash, just log and continue
    }
}