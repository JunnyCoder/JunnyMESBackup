using System;
using System.Collections.Generic;
using System.Linq;

namespace JunnyMES.Core.Configuration
{
    public static class PresetSchema
    {
        public static readonly IReadOnlyList<PresetFieldDefinition> Fields =
            new List<PresetFieldDefinition>
            {
                new PresetFieldDefinition(
                    "registered_IP", "Registered IP",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "registered_SystemName", "System Name",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "savePath", "Backup Save Path",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "checkingTargetPath", "Target Path",
                    PresetValueKind.PerSystem, true, true),

                new PresetFieldDefinition(
                    "folderDepth", "Folder Depth",
                    PresetValueKind.PerSystem, true, true),

                new PresetFieldDefinition(
                    "skipKeywordsFolder", "Excluded Folders",
                    PresetValueKind.List, true, true),

                new PresetFieldDefinition(
                    "skipKeywordsFile", "Excluded Files",
                    PresetValueKind.List, true, true),

                new PresetFieldDefinition(
                    "ServerIP", "Server IP",
                    PresetValueKind.Single, false, true),

                new PresetFieldDefinition(
                    "ServerPort", "Server Port",
                    PresetValueKind.Single, false, true),

                new PresetFieldDefinition(
                    "check_interval_type", "Schedule Type",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "check_interval_hour", "Schedule Hour",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "check_interval_minute", "Schedule Minute",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "check_interval_second", "Schedule Second",
                    PresetValueKind.PerSystem, false, true),

                new PresetFieldDefinition(
                    "last_Check", "Last Check",
                    PresetValueKind.PerSystem, false, false),

                new PresetFieldDefinition(
                    "update_only", "Update Only",
                    PresetValueKind.PerSystem, false, false)
            }.AsReadOnly();

        public static PresetFieldDefinition Get(string key)
        {
            PresetFieldDefinition field =
                Fields.FirstOrDefault(f => f.Key == key);

            if (field == null)
                throw new ArgumentException(
                    "Unknown preset key: " + key);

            return field;
        }
    }
}
