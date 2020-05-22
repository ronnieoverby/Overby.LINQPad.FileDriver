using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace Overby.LINQPad.FileDriver
{
    public static class Serializer
    {
        public static void Serialize<T>(TextWriter writer, T value)
        {
            CreateJsonSerializer().Serialize(writer, value);
        }

        public static T Deserialize<T>(TextReader reader)
        {
            using var jReader = new JsonTextReader(reader);
            return CreateJsonSerializer().Deserialize<T>(jReader);
        }

        public static void Save<T>(string filePath, T value)
        {
            // less-simpler implementation to deal with hidden files/permissions

            using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            Serialize(writer, value);
            writer.Flush();
            stream.SetLength(stream.Position);
        }

        public static T Load<T>(string filePath)
        {
            using var textReader = new StreamReader(filePath);
            return Deserialize<T>(textReader);
        }

        public static JsonSerializer CreateJsonSerializer() => JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        });
    }
}