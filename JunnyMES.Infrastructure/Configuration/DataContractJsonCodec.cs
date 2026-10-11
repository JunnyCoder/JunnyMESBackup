using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using JunnyMES.Infrastructure.Configuration;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace JunnyMES.Infrastructure.Configuration
{
    public interface IJsonSerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string json);
    }

    public sealed class DataContractJsonCodec : IJsonSerializer
    {
        public string Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                Serializer<T>().WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("JSON is empty.");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                try { return (T)Serializer<T>().ReadObject(stream); }
                catch (SerializationException ex) { throw new InvalidDataException("Invalid JSON: " + ex.Message, ex); }
            }
        }
        private static DataContractJsonSerializer Serializer<T>()
        {
            return new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
            { UseSimpleDictionaryFormat = true, MaxItemsInObjectGraph = 65536 });
        }
    }
}
