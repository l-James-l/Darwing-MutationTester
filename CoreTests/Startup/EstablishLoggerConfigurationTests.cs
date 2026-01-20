using Core.Startup;

namespace CoreTests.Startup;

public class EstablishLoggerConfigurationTests
{
    private string _originalWorkingDir;
    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _originalWorkingDir = Directory.GetCurrentDirectory();
        _tempDir = Path.Combine(Path.GetTempPath(), "EstLoggerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Directory.SetCurrentDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.SetCurrentDirectory(_originalWorkingDir);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // ignore clean up errors
        }
    }

    [Test]
    public void GivenLogFileAndFolderDontExists_WhenLoggerConstructed_LogFileCreated()
    {
        // Arrange
        // Ensure no Log or Logs directories exist
        Assert.IsFalse(Directory.Exists(Path.Combine(_tempDir, "Log")));

        // Act & Assert - constructor should not throw
        Assert.DoesNotThrow(() => new EstablishLoggerConfiguration());

        var logsDir = Path.Combine(_tempDir, "Logs");
        Assert.IsTrue(Directory.Exists(logsDir));

        var files = Directory.GetFiles(logsDir, "Log-*.txt");
        Assert.IsNotEmpty(files);

        var filePath = files[0];
        Assert.IsTrue(File.Exists(filePath));
        var length = new FileInfo(filePath).Length;
        Assert.Greater(length, 0, "Log file should not be empty");
    }

    [Test]
    public void GivenLogFolderExists_whenLoggerConstructed_ThenNoErrorThrown()
    {
        // Arrange
        // Create 'Log' directory (singular) to prevent creation of 'Logs' directory
        Directory.CreateDirectory(Path.Combine(_tempDir, "Log"));

        // Act & Assert - constructor should not throw
        Assert.DoesNotThrow(() => new EstablishLoggerConfiguration());
    }

    [Test]
    public void GivenMoreThanFiveLogFiles_WhenLoggerConstructed_OldFilesAreDeletedLeavingFive()
    {
        // Arrange
        var logsDir = Path.Combine(_tempDir, "Logs");
        Directory.CreateDirectory(logsDir);

        // Create 6 existing log files with different creation times (older to newer)
        var existingFiles = Enumerable.Range(1, 6).Select(i =>
        {
            var path = Path.Combine(logsDir, $"Log-old-{i}.txt");
            File.WriteAllText(path, $"old file {i}");
            // set creation time so that lower i => older
            File.SetCreationTime(path, DateTime.UtcNow.AddDays(-10).AddMinutes(i));
            return path;
        }).ToArray();

        // Sanity: there are 6 files before construction
        Assert.That(Directory.GetFiles(logsDir, "Log-*.txt").Length, Is.EqualTo(6));

        // Act - constructing will create another log file and then prune old files to 5
        Assert.DoesNotThrow(() => new EstablishLoggerConfiguration());

        // Assert - only 5 most recent log files remain
        var finalFiles = Directory.GetFiles(logsDir, "Log-*.txt").OrderBy(File.GetCreationTime).ToArray();
        Assert.That(finalFiles.Length, Is.EqualTo(5), "There should only be 5 log files after pruning");

        // Ensure the remaining files are the newest ones (i.e., not the oldest existing files)
        // The oldest existing file (Log-old-1) should have been deleted
        bool oldestExists = finalFiles.Any(f => Path.GetFileName(f).Contains("Log-old-1"));
        Assert.IsFalse(oldestExists, "The oldest log file should have been deleted");
    }
}
