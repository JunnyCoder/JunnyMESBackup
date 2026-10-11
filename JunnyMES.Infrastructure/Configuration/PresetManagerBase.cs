using System;
using System.Collections.Generic;
using JunnyMES.Core.Interfaces;
namespace JunnyMES.Infrastructure.Configuration
{
    public abstract class PresetManagerBase
    {
        protected readonly IPresetStore Store;
        public Dictionary<string,List<string>> Preset { get; set; }
        protected PresetManagerBase(IPresetStore store) { Store = store ?? throw new ArgumentNullException(nameof(store)); }
        public virtual void SavePreset() { Store.Save(DemoPaths.Normalize(Preset)); }
        public virtual void SaveEditedPreset(Dictionary<string,List<string>> edited) { Store.Save(DemoPaths.Normalize(edited)); }
    }
}
