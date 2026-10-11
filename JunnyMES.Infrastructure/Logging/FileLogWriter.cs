
using System;
using System.IO;
using JunnyMES.Core.Logging;

namespace JunnyMES.Infrastructure.Logging
{
    public sealed class FileLogWriter
    {
        private static readonly object WriteLock = new object();

        private readonly string _baseDirectory;

        public FileLogWriter(string baseDirectory = null)
        {
            _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppContext.BaseDirectory
                : baseDirectory;
        }

        public void Write(LogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            bool isErrorLog =
                entry.Level == LogLevel.Error ||
                entry.Level == LogLevel.Warning ||
                entry.Level == LogLevel.System;

            string filePrefix = isErrorLog ? "Log" : "Data";

            DateTime timestamp = entry.Timestamp;

            string directory = Path.Combine(
                _baseDirectory,
                filePrefix + "_" + timestamp.ToString("yyyyMM")
            );

            string filePath = Path.Combine(
                directory,
                filePrefix + "_" +
                timestamp.ToString("yyyyMMdd") + ".txt"
            );

            string line = FormatEntry(entry);

            lock (WriteLock)
            {
                Directory.CreateDirectory(directory);

                using (StreamWriter writer = File.AppendText(filePath))
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static string FormatEntry(LogEntry entry)
        {
            string levelPrefix;

            switch (entry.Level)
            {
                case LogLevel.Error:
                    levelPrefix = "[Error]";
                    break;

                case LogLevel.Warning:
                    levelPrefix = "[Warning]";
                    break;

                case LogLevel.System:
                    levelPrefix = "[System]";
                    break;

                case LogLevel.Information:
                    levelPrefix = "[Information]";
                    break;

                case LogLevel.Operation:
                    levelPrefix = "";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(entry.Level));
            }

            string sourcePrefix =
                string.IsNullOrWhiteSpace(entry.Source)
                    ? ""
                    : "[" + entry.Source + "]";

            return string.Join(" ",
                new[]
                {
                    entry.Timestamp.ToString("yyyy/MM/dd HH:mm:ss"),
                    levelPrefix,
                    sourcePrefix,
                    entry.Message ?? ""
                }.WhereNotEmpty());
        }
    }

    internal static class LogStringExtensions
    {
        public static System.Collections.Generic.IEnumerable<string>
            WhereNotEmpty(this string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value))
                    yield return value;
            }
        }
    }
}
