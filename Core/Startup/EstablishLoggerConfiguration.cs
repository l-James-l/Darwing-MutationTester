using Serilog;
using Serilog.Events;

namespace CoreTests.Startup;

/// <summary>
/// Class to establish the logger.
/// Log messages are written to both console and a file in Logs directory.
/// Log dirrectories are located in the current working directory, e.g., the CLI or GUI bin folder.
/// </summary>
public class EstablishLoggerConfiguration
{
    public EstablishLoggerConfiguration()
    {
        string dateTime = DateTime.UtcNow.ToString().Normalize()
            .Replace(".", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(" ", "-")
            .Replace(":", "-");

        string workingDir = Directory.GetCurrentDirectory();
        string logFilePath = $"{workingDir}\\Logs\\Log-{dateTime}.txt";

        if (!Directory.Exists($"{workingDir}\\Log"))
        {
            Directory.CreateDirectory($"{workingDir}\\Logs");
        }

        try
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File(logFilePath).MinimumLevel.Debug()
            .CreateLogger();

            Log.Debug($"Logger established at {logFilePath}.");
        }
        catch
        {
            throw new Exception("Failed to establish logger configuration.");
        }
        finally
        {
            if (!File.Exists(logFilePath))
            {
                throw new Exception("Logger file was not created successfully.");
            }
        }

        RemoveOldLogFiles(workingDir);
    }

    private void RemoveOldLogFiles(string workingDir)
    {
        // Keep only the 5 most recent log files to avoid excessive disk usage.
        string[] files = Directory.GetFiles(workingDir, "Logs\\Log-*.txt");
        if (files.Length > 5)
        {
            foreach (string file in files.OrderBy(File.GetCreationTime).Take(files.Length - 5))
            {
                File.Delete(file);
            }
        }
    }
}
