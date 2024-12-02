using Hikaria.QC;

namespace Hikaria.AdminSystem.Utilities
{
    internal static class ConsoleLogs
    {
        public static void LogToConsole(string logText, LogLevel logLevel = LogLevel.Message) => QuantumConsole.Instance.LogToConsole(logText, logLevel);

        public static void LogToConsoleAsync(string logText, LogLevel logLevel = LogLevel.Message) => QuantumConsole.Instance.LogToConsoleAsync(logText, logLevel);
    }
}
