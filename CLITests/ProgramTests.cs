using Castle.Core.Resource;
using Serilog;
using System.Text;
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

    [Test, Explicit("This test is basically just for the sake of it, and it cloggs up the test output because it creates a real logger.")]
    public void WhenMainCalled_ThenDoesNotThrow()
    {
        //TODO, this doesnt actually test anything. exeptions dont cause this test to fail. find fix.

        // Arrange
        string[] args = [];

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken cancellationToken = cts.Token;
            Task.Run(() => Program.Main(args), cancellationToken); // Will loop infinitly so just check we dont throw initially.
            Thread.Sleep(2000);
            cts.Cancel();
            return;
        });
    }
}