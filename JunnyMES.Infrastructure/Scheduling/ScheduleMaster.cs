using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleManager
{
    public enum ScheduleType { SpecificTime, Interval, Custom }
    public sealed class ScheduleMaster : IScheduler
    {
        private readonly object gate = new object();
        private readonly Func<DateTime, TimeSpan> nextDelay;
        private readonly Func<DateTime> now;
        private readonly Func<TimeSpan, CancellationToken, Task> delay;
        private Func<CancellationToken, Task> task = token => Task.CompletedTask;
        private CancellationTokenSource lifetime;
        private Task completion = Task.CompletedTask;
        private bool disposed;
        public Exception LastError { get; private set; }
        public event EventHandler<Exception> Failed;
        public bool exitRequested { get { lock (gate) return lifetime == null || lifetime.IsCancellationRequested; } set { if (value) Stop(); } }
        public bool isTimerOn { get { lock (gate) return lifetime != null && !lifetime.IsCancellationRequested; } }
        public ScheduleMaster(int hour = 18, int minute = 30, int second = 0)
            : this(ScheduleType.SpecificTime, hour, minute, second) { }
        public ScheduleMaster(ScheduleType type, int hour = 18, int minute = 30, int second = 0)
            : this(CreateDelay(type, hour, minute, second)) { }
        // Injectable clock/delay lets tests drive time without long wall-clock waits.
        public ScheduleMaster(Func<DateTime, TimeSpan> nextDelay, Func<DateTime> now = null,
            Func<TimeSpan, CancellationToken, Task> delay = null)
        {
            this.nextDelay = nextDelay ?? throw new ArgumentNullException(nameof(nextDelay));
            this.now = now ?? (() => DateTime.Now);
            this.delay = delay ?? ((span, token) => Task.Delay(span, token));
        }
        private static Func<DateTime, TimeSpan> CreateDelay(ScheduleType type, int hour, int minute, int second)
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
                throw new ArgumentOutOfRangeException(nameof(hour));
            if (type == ScheduleType.SpecificTime) return value => UntilNextDaily(value, hour, minute, second);
            if (type != ScheduleType.Interval) throw new ArgumentException("Unsupported schedule type.", nameof(type));
            var interval = new TimeSpan(hour, minute, second);
            if (interval <= TimeSpan.Zero) throw new ArgumentException("Interval must be positive.");
            return value => interval;
        }
        public static TimeSpan UntilNextDaily(DateTime value, int hour, int minute, int second)
        {
            var next = value.Date.Add(new TimeSpan(hour, minute, second));
            if (next <= value) next = next.AddDays(1);
            return next - value;
        }
        public void SetTask(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            SetAsyncTask(token => { token.ThrowIfCancellationRequested(); action(); return Task.CompletedTask; });
        }
        public void SetAsyncTask(Func<CancellationToken, Task> action)
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(ScheduleMaster));
                if (!completion.IsCompleted) throw new InvalidOperationException("Stop and await the scheduler before changing its task.");
                task = action ?? throw new ArgumentNullException(nameof(action));
            }
        }
        public void Start()
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(ScheduleMaster));
                if (!completion.IsCompleted)
                {
                    if (lifetime != null && !lifetime.IsCancellationRequested) return;
                    throw new InvalidOperationException("Previous run is stopping. Await StopAsync before restarting.");
                }
                LastError = null;
                var source = new CancellationTokenSource(); lifetime = source;
                var action = task;
                completion = Task.Run(() => RunAsync(source, action));
            }
        }
        private async Task RunAsync(CancellationTokenSource source, Func<CancellationToken, Task> action)
        {
            try
            {
                while (true)
                {
                    source.Token.ThrowIfCancellationRequested();
                    var span = nextDelay(now());
                    if (span <= TimeSpan.Zero) throw new InvalidOperationException("Schedule delay must be positive.");
                    await delay(span, source.Token).ConfigureAwait(false);
                    source.Token.ThrowIfCancellationRequested();
                    await action(source.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (source.IsCancellationRequested) { }
            catch (Exception ex)
            {
                LastError = ex;
                try { Failed?.Invoke(this, ex); } catch (Exception) { }
            }
            finally { lock (gate) { if (lifetime == source) lifetime = null; source.Dispose(); } }
        }
        public void Stop() { lock (gate) lifetime?.Cancel(); }
        public Task StopAsync() { lock (gate) { lifetime?.Cancel(); return completion; } }
        public void Dispose() { lock (gate) { disposed = true; lifetime?.Cancel(); } }
    }
}
