
using System;
using System.Collections.Generic;
using System.Linq;

namespace JunnyMES.Core.Configuration
{
    public static class PresetEditMerger
    {
        public static Dictionary<string, List<string>> Clone(
            Dictionary<string, List<string>> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.ToDictionary(
                x => x.Key,
                x => x.Value == null
                    ? new List<string>()
                    : new List<string>(x.Value));
        }

        public static Dictionary<string, List<string>> Merge(
            Dictionary<string, List<string>> current,
            Dictionary<string, List<string>> original,
            Dictionary<string, List<string>> edited,
            PresetEditorRole role,
            int clientSystemIndex)
        {
            var result = Clone(current);

            foreach (var field in PresetSchema.Fields)
            {
                if (!field.CanEdit(role))
                    continue;

                string key = field.Key;

                var before = GetValues(original, key);
                var after = GetValues(edited, key);
                var target = GetValues(result, key);

                if (field.Kind == PresetValueKind.List)
                {
                    if (before.SequenceEqual(after))
                        continue;

                    if (!target.SequenceEqual(before))
                        throw new InvalidOperationException(
                            "다른 작업에서 변경한 설정입니다: " + key);

                    result[key] = new List<string>(after);
                    continue;
                }

                if (before.Count != after.Count)
                    throw new InvalidOperationException(
                        "설정 항목 수가 변경되었습니다: " + key);

                int count = field.Kind == PresetValueKind.Single
                    ? 1 : before.Count;

                for (int i = 0; i < count; i++)
                {
                    if (before[i] == after[i])
                        continue;

                    if (role == PresetEditorRole.Client &&
                        field.Kind == PresetValueKind.PerSystem &&
                        i != clientSystemIndex)
                    {
                        throw new InvalidOperationException(
                            "다른 시스템의 설정을 변경할 수 없습니다.");
                    }

                    if (i >= target.Count)
                        throw new InvalidOperationException(
                            "설정 인덱스가 올바르지 않습니다: " + key);

                    if (target[i] != before[i])
                        throw new InvalidOperationException(
                            "다른 작업에서 변경한 설정입니다: " + key);

                    target[i] = after[i];
                }
            }

            return result;
        }

        private static List<string> GetValues(
            Dictionary<string, List<string>> preset,
            string key)
        {
            if (preset == null ||
                !preset.TryGetValue(key, out var values) ||
                values == null)
            {
                throw new InvalidOperationException(
                    "설정 항목이 없습니다: " + key);
            }

            return values;
        }
    }
}
