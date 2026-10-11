using System.Collections.Generic;
namespace JunnyMES.Infrastructure.Configuration
{
    public static class PresetJsonSerializer
    {
        private static readonly DataContractJsonCodec Codec = new DataContractJsonCodec();
        public static string Serialize(Dictionary<string, List<string>> value) { return Codec.Serialize(value); }
        public static Dictionary<string, List<string>> Deserialize(string json) { return Codec.Deserialize<Dictionary<string, List<string>>>(json); }
    }
}
