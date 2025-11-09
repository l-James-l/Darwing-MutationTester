using Serilog;

namespace CoreTests.Startup;

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
            .WriteTo.Console()
            .WriteTo.File(logFilePath)
            .CreateLogger();

            Log.Information($"Logger established at {logFilePath}.");
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
    }
}
