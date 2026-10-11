using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetworkManager;
namespace IPC_Backup_Server
{
    public partial class ServerMain
    {
        private void StartConfiguredServer()
        {
            if(closing||!serverTask.IsCompleted)return;
            serverLifetime?.Dispose();serverLifetime=new CancellationTokenSource();
            server=new Server(presetStore,ref Server_Preset,Server_Preset["ServerPort"][0],Server_Preset["ServerIP"][0],logger:_logger);
            var current=server;var token=serverLifetime.Token;
            serverTask=Task.Run(()=>current.Start(token));BtnServerStart.Text="Stop Server";
        }
        private async Task StopServerAsync()
        {serverLifetime?.Cancel();server?.Stop();await serverTask;server=null;BtnServerStart.Text="Start Server";}
        private void RebuildStationButtons()
        {
            foreach(var item in buttonDict.Values){Controls.Remove(item);item.Dispose();}
            foreach(var item in labelDict.Values){Controls.Remove(item);item.Dispose();}
            buttonDict.Clear();labelDict.Clear();
            var names=Server_Preset["registered_SystemName"];
            for(int i=0;i<names.Count;i++)
            {SetIPCBtn(names[i],i%5,i/5);buttonDict.Add(names[i],(Button)Controls["IPCBtn_"+names[i]]);labelDict.Add(names[i],(Label)Controls["IPCUpdateLabel_"+names[i]]);}
        }
    }
}
