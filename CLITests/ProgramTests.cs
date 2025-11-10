namespace CLITests;

public class ProgramTests
{
    private string _originalWorkingDir;
    private string _tempDir;
    private TextReader _originalIn;

    [SetUp]
    public void SetUp()
    {
        _originalWorkingDir = Directory.GetCurrentDirectory();
        _tempDir = Path.Combine(Path.GetTempPath(), "ProgramTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);

        _originalIn = Console.In;
        // Provide a blank line so CLIApp.ReadLine will return and not block
        Console.SetIn(new StringReader(Environment.NewLine));
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Console.SetIn(_originalIn);
            Directory.SetCurrentDirectory(_originalWorkingDir);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup errors
        }
    }

    [Test]
    public void GivenNoArgs_WhenMainCalled_ThenDoesNotThrow()
    {
        // Arrange
        string[] args = Array.Empty<string>();

        // Act & Assert
        Assert.DoesNotThrow(() => Program.Main(args));
    }

    [Test]
    public void GivenDevArg_WhenMainCalled_ThenDoesNotThrow()
    {
        // Arrange
        string[] args = new[] { "--dev" };

        // Act & Assert
        Assert.DoesNotThrow(() => Program.Main(args));
    }
}
