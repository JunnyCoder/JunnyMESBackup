using System.Collections.Generic;

namespace JunnyMES.Core.Interfaces
{
    public interface IPresetStore
    {
        Dictionary<string, List<string>> Load();

        void Save(
            Dictionary<string, List<string>> preset);
    }
}