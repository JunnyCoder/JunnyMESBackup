using System;
using JunnyMES.Core.Logging;

namespace JunnyMES.Core.Interfaces
{
    public interface IAppLogger
    {
        event EventHandler<LogEntry> LogWritten;

        void Log(
            LogLevel level,
            string source,
            string message);
    }
}