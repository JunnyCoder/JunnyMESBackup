using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JunnyMES.Core.Interfaces;
namespace JunnyMES.Infrastructure.Configuration
{
    public sealed class JsonPresetStore : IPresetStore
    {
        private readonly string _filePath;
        private readonly object gate = new object();
        public JsonPresetStore(string filePath) { _filePath = Path.GetFullPath(filePath ?? throw new ArgumentNullException(nameof(filePath))); }
        public Dictionary<string, List<string>> Load()
        {
            lock (gate)
            {
                if (new FileInfo(_filePath).Length > 1024 * 1024) throw new InvalidDataException("Preset exceeds 1 MiB.");
                return DemoPaths.Normalize(PresetJsonSerializer.Deserialize(File.ReadAllText(_filePath)));
            }
        }
        public void Save(Dictionary<string, List<string>> preset)
        {
            var value = DemoPaths.Normalize(preset);
            // Persist portable paths, while keeping the caller's resolved runtime paths untouched.
            foreach (var key in new[] { "checkingTargetPath", "savePath" })
                for (int i = 0; i < value[key].Count; i++) value[key][i] = DemoPaths.Relative(value[key][i]);
            string json = PresetJsonSerializer.Serialize(value);
            if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new InvalidDataException("Preset exceeds 1 MiB.");
            lock (gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                string temp = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(temp, json, new UTF8Encoding(false));
                    if (File.Exists(_filePath))
                    {
                        string archive = Path.Combine(Path.GetDirectoryName(_filePath), "Preset_old");
                        Directory.CreateDirectory(archive);
                        File.Replace(temp, _filePath, Path.Combine(archive, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json"));
                    }
                    else File.Move(temp, _filePath);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
        }
    }
}
