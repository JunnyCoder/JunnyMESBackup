using System.Collections.Generic;
using System.IO;
using JunnyMES.Core.Interfaces;
using JunnyMES.Infrastructure.Configuration;
namespace FileManager
{
    // Keep the user's server-specific manager and filename.
    class PresetManager : PresetManagerBase
    {
        public PresetManager(IPresetStore store) : base(store)
        {
            try { Preset = DemoPaths.Normalize(Store.Load()); }
            catch (FileNotFoundException) { Preset = getServerDefaultPreset(); Store.Save(Preset); }
            catch (DirectoryNotFoundException) { Preset = getServerDefaultPreset(); Store.Save(Preset); }
        }
        public static Dictionary<string,List<string>> getServerDefaultPreset() { return DemoPaths.Defaults(); }
        public static Dictionary<string,List<string>> DeserializePresetJson(string json) { return PresetJsonSerializer.Deserialize(json); }
        public void SavePreset(bool save) { if (save) SavePreset(); }
    }
}
