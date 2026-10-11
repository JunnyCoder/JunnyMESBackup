
using System;
using System.Diagnostics;
using JunnyMES.Core.Interfaces;
using JunnyMES.Core.Logging;

namespace JunnyMES.Infrastructure.Logging
{
    public sealed class AppLogger : IAppLogger
    {
        private readonly FileLogWriter _fileWriter;

        public event EventHandler<LogEntry> LogWritten;

        public AppLogger(FileLogWriter fileWriter)
        {
            _fileWriter = fileWriter ??
                throw new ArgumentNullException(nameof(fileWriter));
        }

        public void Log(
            LogLevel level,
            string source,
            string message)
        {
            var entry = new LogEntry(
                level,
                source,
                message
            );

            Exception writeError = null;

            try
            {
                _fileWriter.Write(entry);
            }
            catch (Exception ex)
            {
                writeError = ex;

                // 파일 저장이 실패해도 업무 처리는 중단하지 않음
                Trace.WriteLine(
                    "Log file write failed: " + ex);
            }

            // 원본 로그를 Form에 전달
            Publish(entry);

            if (writeError != null)
            {
                // 동일 로거의 Log()를 다시 호출하면
                // 파일 저장 실패가 재귀적으로 발생할 수 있음
                var errorEntry = new LogEntry(
                    LogLevel.Error,
                    "AppLogger",
                    "File log write failed: " +
                    writeError.Message
                );

                // 파일 재저장 없이 UI에만 알림
                Publish(errorEntry);
            }
        }

        private void Publish(LogEntry entry)
        {
            var handlers = LogWritten;

            if (handlers == null)
                return;

            foreach (EventHandler<LogEntry> handler
                in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, entry);
                }
                catch (Exception ex)
                {
                    // UI 구독자 오류가 백업 처리를 중단하지 않도록
                    // 별도 진단 경로에만 남김
                    Trace.WriteLine(
                        "Log event handler failed: " + ex);
                }
            }
        }
    }
}
