using System;
using System.Windows.Forms;
using JunnyMES.Core.Interfaces;
using JunnyMES.Core.Logging;

namespace JunnyMES.WinForms.Logging
{
    // Only presents a LogEntry. The logger writes the file exactly once.
    public sealed class LogListViewBinder : IDisposable
    {
        private readonly ListView _view;
        private readonly IAppLogger _logger;
        private bool _disposed;
        public event EventHandler<LogEntry> EntryDisplayed;

        public LogListViewBinder(ListView view, IAppLogger logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Needed because the server starts before the Form is displayed.
            var handle = _view.Handle;
            _logger.LogWritten += OnLogWritten;
        }

        private void OnLogWritten(object sender, LogEntry entry)
        {
            if (_disposed || _view.IsDisposed || _view.Disposing) return;
            if (_view.InvokeRequired)
            {
                if (!_view.IsHandleCreated) return;
                try
                {
                    _view.BeginInvoke(new Action(() => Append(entry)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
            else Append(entry);
        }

        private void Append(LogEntry entry)
        {
            if (_disposed || _view.IsDisposed || _view.Disposing) return;
            string level = entry.Level == LogLevel.Operation ? "" : "[" + entry.Level + "] ";
            string source = string.IsNullOrWhiteSpace(entry.Source) ? "" : "[" + entry.Source + "] ";
            var item = new ListViewItem(new[] {
                entry.Timestamp.ToString("yyyy/MM/dd HH:mm:ss"),
                level + source + entry.Message
            });
            _view.Items.Add(item);
            _view.EnsureVisible(_view.Items.Count - 1);
            EntryDisplayed?.Invoke(this, entry);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _logger.LogWritten -= OnLogWritten;
            EntryDisplayed = null;
        }
    }
}
