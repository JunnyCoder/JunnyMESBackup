using System;
using System.Collections.Generic;
using System.Linq;

namespace JunnyMES.Core.Configuration
{
    public class PresetEditSession
    {
        private readonly PresetEditorRole _role;
        private readonly int _systemIndex;

        private readonly Dictionary<string, List<string>> _workingCopy;

        public PresetEditSession(
            Dictionary<string, List<string>> source,
            PresetEditorRole role,
            int systemIndex)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (systemIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(systemIndex));

            _role = role;
            _systemIndex = systemIndex;

            _workingCopy = Clone(source);
        }

        public string GetValue(string key)
        {
            PresetFieldDefinition field = PresetSchema.Get(key);

            if (field.Kind == PresetValueKind.List)
                throw new InvalidOperationException(
                    "Use GetValues() for list fields.");

            List<string> values = GetExistingValues(key);

            int index = field.Kind == PresetValueKind.Single
                ? 0
                : _systemIndex;

            if (index >= values.Count)
                throw new InvalidOperationException(
                    "Preset index out of range: " + key);

            return values[index];
        }

        public List<string> GetValues(string key)
        {
            PresetFieldDefinition field = PresetSchema.Get(key);

            if (field.Kind != PresetValueKind.List)
                throw new InvalidOperationException(
                    "This field is not a list.");

            return new List<string>(GetExistingValues(key));
        }

        public void SetValue(string key, string value)
        {
            PresetFieldDefinition field = PresetSchema.Get(key);

            if (!field.CanEdit(_role))
                throw new InvalidOperationException(
                    "Field is read-only: " + key);

            if (field.Kind == PresetValueKind.List)
                throw new InvalidOperationException(
                    "Use SetValues() for list fields.");

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (key == "folderDepth")
            {
                int depth;
                if (!int.TryParse(value, out depth) || depth < 1 || depth > 32)
                    throw new ArgumentException(
                        "Folder Depth must be a positive integer.");
            }

            List<string> values = GetExistingValues(key);

            int index = field.Kind == PresetValueKind.Single
                ? 0
                : _systemIndex;

            if (index >= values.Count)
                throw new InvalidOperationException(
                    "Preset index out of range: " + key);

            values[index] = value;
        }

        public void SetValues(string key, IEnumerable<string> values)
        {
            PresetFieldDefinition field = PresetSchema.Get(key);

            if (!field.CanEdit(_role) ||
                field.Kind != PresetValueKind.List)
                throw new InvalidOperationException(
                    "Cannot edit list field: " + key);

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            List<string> updated = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            // 기존 Server 초기화 정책에서 빈 목록은
            // 기본값으로 대체될 수 있으므로 이번 UI에서는 허용하지 않음.


            _workingCopy[key] = updated;
        }

        public Dictionary<string, List<string>> GetResult()
        {
            return Clone(_workingCopy);
        }

        private List<string> GetExistingValues(string key)
        {
            List<string> values;

            if (!_workingCopy.TryGetValue(key, out values) ||
                values == null)
                throw new InvalidOperationException(
                    "Missing preset field: " + key);

            return values;
        }

        private static Dictionary<string, List<string>> Clone(
            Dictionary<string, List<string>> source)
        {
            return source.ToDictionary(
                pair => pair.Key,
                pair => pair.Value == null
                    ? new List<string>()
                    : new List<string>(pair.Value)
            );
        }
    }
}
