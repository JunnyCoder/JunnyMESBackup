using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JunnyMES.Core.Configuration;
using JunnyMES.Core.Interfaces;
using JunnyMES.Infrastructure.Configuration;
namespace FileManager
{
    internal sealed class PresetConnectionException : IOException
    { public PresetConnectionException(string message, Exception inner) : base(message,inner) {} }
    class PresetManager : PresetManagerBase
    {
        private readonly IAppLogger logger;
        private readonly CancellationToken token;
        public string LastConnectedLocalIP { get; private set; }
        public PresetManager(IPresetStore store,IAppLogger logger=null,CancellationToken token=default(CancellationToken)) : base(store)
        {
            this.logger=logger;this.token=token;
            Dictionary<string,List<string>> local;
            try { local=Store.Load(); }
            catch(FileNotFoundException) { local=DemoPaths.Defaults(); }
            catch(DirectoryNotFoundException) { local=DemoPaths.Defaults(); }
            Preset=DemoPaths.Normalize(local);
            var remote=getPresetFromServer();
            var next=PresetEditMerger.Clone(remote);
            foreach(string key in new[]{"checkingTargetPath","folderDepth","skipKeywordsFolder","skipKeywordsFile","FilesToCheck"})
                if(local.ContainsKey(key) && local["registered_SystemName"].SequenceSame(remote["registered_SystemName"])) next[key]=new List<string>(local[key]);
            Preset=DemoPaths.Normalize(next); token.ThrowIfCancellationRequested();SavePreset();
        }
        private PresetManager(IPresetStore store,bool offline) : base(store) { Preset=DemoPaths.Normalize(Store.Load()); }
        public static PresetManager LoadOffline(IPresetStore store) { return new PresetManager(store,true); }
        public Dictionary<string,List<string>> getPresetFromDefaultServer() { return Fetch("127.0.0.1","18080"); }
        public Dictionary<string,List<string>> getPresetFromServer() { return Fetch(Preset["ServerIP"][0],Preset["ServerPort"][0]); }
        private Dictionary<string,List<string>> Fetch(string address,string port)
        {
            string json;
            try
            {
                using(var client=new NetworkManager.Client(address,port,3,1,logger,cancellationToken:token))
                {json=client.GetPresetString();LastConnectedLocalIP=client.ConnectedLocalIPAddress;}
            }
            catch(OperationCanceledException) { throw; }
            catch(IOException ex) { token.ThrowIfCancellationRequested();throw new PresetConnectionException("Server connection or response failed.",ex); }
            // Invalid JSON/config is not an offline connection fallback.
            return DemoPaths.Normalize(PresetJsonSerializer.Deserialize(json));
        }
        public static Dictionary<string,List<string>> DeserializePresetJson(string json) {return PresetJsonSerializer.Deserialize(json);}
    }
    internal static class PresetListComparison
    {internal static bool SequenceSame(this List<string> a,List<string> b){return System.Linq.Enumerable.SequenceEqual(a,b);}}
}
