using System;

namespace JunnyMES.Core.Logging
{
    public class LogEntry : EventArgs
    {
        public DateTime Timestamp { get; }

        public LogLevel Level { get; }

        public string Source { get; }

        public string Message { get; }

        public LogEntry(
            LogLevel level,
            string source,
            string message)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Source = source;
            Message = message;
        }
    }
}