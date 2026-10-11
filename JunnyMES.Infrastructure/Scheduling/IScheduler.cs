using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using JunnyMES.Infrastructure.Configuration;

namespace ScheduleManager
{
    public interface IScheduler : IDisposable
    {
        bool exitRequested { get; set; }
        bool isTimerOn { get; }
        void SetTask(Action task);
        void SetAsyncTask(Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> task);
        event EventHandler<Exception> Failed;
        void Start();
        void Stop();
        System.Threading.Tasks.Task StopAsync();
    }
}
