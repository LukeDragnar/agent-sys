namespace AgentInfrastructure.Clients;

internal static class AppFileLogger
{
    private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly object _lock = new();

    public static void Error(string source, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var file = Path.Combine(LogDir, $"{DateTime.Now:yyyy-MM-dd}.log");
            var line = $"{DateTime.Now:HH:mm:ss}:[{source}] {message}{Environment.NewLine}";
            lock (_lock) File.AppendAllText(file, line);
        }
        catch { }
    }

    public static void Error(string source, Exception ex)
        => Error(source, $"{ex.GetType().Name}: {ex.Message}");
}
