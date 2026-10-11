using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JunnyMES.Core.Configuration;
using JunnyMES.Infrastructure.Configuration;
using JunnyMES.Infrastructure.Backup;
namespace IPC_Backup
{
    public partial class ClientMain
    {
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private CancellationTokenSource _workCancellation;
        private Task _activeWork=Task.CompletedTask;
        private Task _connectWork=Task.CompletedTask;
        private bool _busy,_closing,_allowClose;
        private BackupWorkflow _workflow;
        private ChangeSet _plan;
        private BackupPolicy CapturePolicy()
        {
            var preset=DemoPaths.Normalize(Preset);List<string> selected;
            if(!preset.TryGetValue("FilesToCheck",out selected))selected=listView1.Items.Cast<ListViewItem>().Select(x=>x.SubItems[1].Text).ToList();
            return new BackupPolicy(preset["checkingTargetPath"][sysIndex],preset["ServerIP"][0]+":"+preset["ServerPort"][0]+"|"+preset["savePath"][sysIndex],int.Parse(preset["folderDepth"][sysIndex]),selected,preset["skipKeywordsFolder"],preset["skipKeywordsFile"]);
        }
        private static int CountProgram(IEnumerable<string> files,string name) {return files.Count(x=>BackupPolicy.ProgramName(x).Equals(name,StringComparison.OrdinalIgnoreCase));}
        private void ShowSuccessTime()
        {
            try {var success=new JsonSuccessSnapshotStore(DemoPaths.State("client")).Load();LastCheckTime.Text=success==null?"No successful backup yet":"Last backup success (UTC): "+success.CompletedUtc;}
            catch(Exception ex){LastCheckTime.Text="Success state error: "+ex.Message;}
        }
        private Task<bool> RunWorkAsync(Func<CancellationToken,Task<bool>> action)
        {
            if(_busy||_closing||_connecting||_workflow==null)return Task.FromResult(false);
            _busy=true;_workCancellation=CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            var task=RunCoreAsync(action,_workCancellation);_activeWork=task;return task;
        }
        private async Task<bool> RunCoreAsync(Func<CancellationToken,Task<bool>> action,CancellationTokenSource cancellation)
        {
            await Task.Yield();
            try {ManualPanel.Enabled=false;toolStrip1.Enabled=false;AutoModePanel.Enabled=true;BtnRun.Enabled=false;BtnStop.Enabled=true;RBtnAutoUpdate.Enabled=false;RBtnCheckOnly.Enabled=false;return await action(cancellation.Token);}
            catch(OperationCanceledException){_plan=null;if(!_closing)windowActionLog("Operation cancelled. Check again.");return false;}
            catch(Exception ex){_plan=null;if(!_closing){AlertChange("Operation incomplete",System.Drawing.Color.LightYellow,System.Drawing.Color.Black);windowActionLog("Operation failed: "+ex.Message);}return false;}
            finally {_busy=false;cancellation.Dispose();if(ReferenceEquals(_workCancellation,cancellation))_workCancellation=null;if(!_closing&&!IsDisposed)RenderReadyControls();}
        }
        private void RenderReadyControls()
        {if(appSchedule.isTimerOn)DisableExceptStop();else{SetWinodwToDefalut();ManualUpdate.Enabled=_plan!=null&&_plan.HasChanges;}}
        private async Task StopWorkAsync()
        { _workCancellation?.Cancel();await appSchedule.StopAsync();await _activeWork;_plan=null; }
        private Task OnUiAsync(Func<Task> action,CancellationToken token)
        {
            var done=new TaskCompletionSource<bool>();
            if(_closing||IsDisposed||token.IsCancellationRequested){done.SetCanceled();return done.Task;}
            try{BeginInvoke(new Action(async()=>{try{token.ThrowIfCancellationRequested();await action();done.TrySetResult(true);}catch(OperationCanceledException){done.TrySetCanceled();}catch(Exception ex){done.TrySetException(ex);}}));}
            catch(InvalidOperationException){done.TrySetCanceled();}
            return done.Task;
        }
        private async void OnClientClosing(object sender,FormClosingEventArgs e)
        {
            if(_allowClose)return;e.Cancel=true;if(_closing)return;_closing=true;_lifetime.Cancel();
            try{await StopWorkAsync();await _connectWork;}catch(OperationCanceledException){}
            finally{appSchedule.Dispose();_allowClose=true;Close();}
        }
        public void SaveConnectionSettings(string address,string port)
        {
            DemoPaths.ValidateEndpoint(address,port);if(_busy||_connecting||appSchedule.isTimerOn)throw new InvalidOperationException("Stop active work before editing connection.");
            var next=PresetEditMerger.Clone(Preset);next["ServerIP"][0]=address;next["ServerPort"][0]=port;
            presetManager.SaveEditedPreset(next);presetManager.Preset=next;Preset=next;_plan=null;SetServerName();
        }
    }
}
