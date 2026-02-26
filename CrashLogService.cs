using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MeetingNotesApp
{
    /// <summary>
    /// Global crash logging service. Catches unhandled exceptions from 3 sources
    /// (AppDomain, WPF Dispatcher, TaskScheduler) and writes detailed crash reports
    /// to %AppData%/MeetingNotesApp/crashlog.txt.
    /// </summary>
    public class CrashLogService
    {
        private const string AppFolderName = "MeetingNotesApp";
        private const string CrashLogFileName = "crashlog.txt";
        private readonly object _lockObject = new();

        public string GetCrashLogPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, AppFolderName, CrashLogFileName);
        }

        /// <summary>
        /// Registers global exception handlers. Call this first in App.OnStartup,
        /// before any other initialization that could fail.
        /// </summary>
        public void RegisterGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            }

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public void LogCrash(Exception exception, string source)
        {
            try
            {
                var crashLogPath = GetCrashLogPath();
                var directory = Path.GetDirectoryName(crashLogPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var logEntry = BuildLogEntry(exception, source);

                lock (_lockObject)
                {
                    File.AppendAllText(crashLogPath, logEntry);
                }
            }
            catch
            {
                // If we can't write the crash log, there's nothing we can do.
                // Never throw from the crash handler.
            }
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception
                ?? new Exception($"Unknown exception: {e.ExceptionObject}");
            LogCrash(exception, $"AppDomain (IsTerminating: {e.IsTerminating})");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception, "WPF Dispatcher (UI Thread)");

            e.Handled = true;

            MessageBox.Show(
                $"Meeting Notes encountered an unexpected error and needs to close.\n\n" +
                $"Error details have been saved to:\n{GetCrashLogPath()}\n\n" +
                $"Error: {e.Exception.Message}",
                "Meeting Notes - Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Application.Current?.Shutdown(1);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash(e.Exception, "TaskScheduler (Unobserved Task)");
            e.SetObserved();
        }

        private string BuildLogEntry(Exception exception, string source)
        {
            var sb = new StringBuilder();

            sb.AppendLine("================================================================================");
            sb.AppendLine($"CRASH REPORT - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            sb.AppendLine("--- Application Info ---");
            sb.AppendLine($"App Version: {GetAppVersion()}");
            sb.AppendLine($".NET Version: {Environment.Version}");
            sb.AppendLine($"OS Version: {Environment.OSVersion}");
            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
            sb.AppendLine();

            sb.AppendLine("--- Exception Source ---");
            sb.AppendLine($"Source: {source}");
            sb.AppendLine();

            sb.AppendLine("--- Exception Details ---");
            AppendExceptionDetails(sb, exception, 0);

            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        private void AppendExceptionDetails(StringBuilder sb, Exception exception, int depth)
        {
            var indent = new string(' ', depth * 2);

            if (depth > 0)
            {
                sb.AppendLine($"{indent}--- Inner Exception (Level {depth}) ---");
            }

            sb.AppendLine($"{indent}Type: {exception.GetType().FullName}");
            sb.AppendLine($"{indent}Message: {exception.Message}");

            if (!string.IsNullOrEmpty(exception.Source))
            {
                sb.AppendLine($"{indent}Source: {exception.Source}");
            }

            if (exception.TargetSite != null)
            {
                sb.AppendLine($"{indent}Target Site: {exception.TargetSite.DeclaringType?.FullName}.{exception.TargetSite.Name}");
            }

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                sb.AppendLine($"{indent}Stack Trace:");
                foreach (var line in exception.StackTrace.Split('\n'))
                {
                    sb.AppendLine($"{indent}  {line.Trim()}");
                }
            }

            if (exception is AggregateException aggregateException)
            {
                sb.AppendLine($"{indent}--- Aggregated Exceptions ({aggregateException.InnerExceptions.Count}) ---");
                for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                {
                    sb.AppendLine($"{indent}[{i + 1}]");
                    AppendExceptionDetails(sb, aggregateException.InnerExceptions[i], depth + 1);
                }
            }
            else if (exception.InnerException != null)
            {
                AppendExceptionDetails(sb, exception.InnerException, depth + 1);
            }
        }

        private static string GetAppVersion()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
