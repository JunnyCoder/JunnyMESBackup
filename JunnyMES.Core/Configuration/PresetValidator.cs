using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace JunnyMES.Core.Configuration
{
    public static class PresetValidator
    {
        public static void Validate(Dictionary<string, List<string>> preset)
        {
            if (preset == null) throw new InvalidDataException("Preset is null.");
            List<string> names;
            if (!preset.TryGetValue("registered_SystemName", out names) || names == null || names.Count < 1 || names.Count > 32) throw new InvalidDataException("Invalid system count.");
            foreach (var field in PresetSchema.Fields)
            {
                List<string> values;
                if (!preset.TryGetValue(field.Key, out values) || values == null || values.Any(x => x == null)) throw new InvalidDataException("Missing/null preset: " + field.Key);
                int count = field.Kind == PresetValueKind.Single ? 1 : names.Count;
                if (field.Kind != PresetValueKind.List && (values.Count != count || values.Any(string.IsNullOrWhiteSpace))) throw new InvalidDataException("Invalid preset count/value: " + field.Key);
            }
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count || preset["registered_IP"].Distinct().Count() != names.Count) throw new InvalidDataException("Duplicate station.");
            foreach (string name in names) if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..") throw new InvalidDataException("Invalid station name.");
            for (int i = 0; i < names.Count; i++)
            {
                Number(preset, "folderDepth", i, 1, 32);
                int type = Number(preset, "check_interval_type", i, 0, 1);
                int hour = Number(preset, "check_interval_hour", i, 0, 23);
                int minute = Number(preset, "check_interval_minute", i, 0, 59);
                int second = Number(preset, "check_interval_second", i, 0, 59);
                if (type == 1 && hour + minute + second == 0) throw new InvalidDataException("Interval must be positive.");
                bool flag;if (!bool.TryParse(preset["update_only"][i], out flag)) throw new InvalidDataException("Invalid update_only.");
            }
            List<string> selected;
            if (preset.TryGetValue("FilesToCheck", out selected) && (selected == null || selected.Any(x => string.IsNullOrWhiteSpace(x) || x.Contains("..") || x.IndexOfAny(new[] {'/', '\\', ':'}) >= 0))) throw new InvalidDataException("Invalid program selection.");
        }
        private static int Number(Dictionary<string,List<string>> p,string key,int i,int min,int max)
        {int value;if(!int.TryParse(p[key][i],out value)||value<min||value>max)throw new InvalidDataException("Invalid " + key);return value;}
    }
}
