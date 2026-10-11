using System;
using JunnyMES.Core.Interfaces;
using JunnyMES.Core.Logging;

namespace JunnyMES.Infrastructure.Logging
{
    // Client.exe / Server.exe each owns its own logger instance.
    // Fallback for infrastructure messages before the app logger is configured.
    public static class AppLogHub
    {
        private static readonly IAppLogger Fallback =
            new AppLogger(new FileLogWriter());
        private static IAppLogger _current;

        public static void Configure(IAppLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            System.Threading.Interlocked.Exchange(ref _current, logger);
        }

        public static void Log(LogLevel level, string source, string message)
        {
            var logger = System.Threading.Volatile.Read(ref _current) ?? Fallback;
            logger.Log(level, source, message);
        }
    }
}
