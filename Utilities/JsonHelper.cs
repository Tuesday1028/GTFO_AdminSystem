using Clonesoft.Json;
using System.IO;

namespace Hikaria.AdminSystem.Utilities
{
    public static class JsonHelper
    {
        public static void SaveToDisk(object value, string path, string fileName)
        {
            string json = Serialize(value);
            string fullPath = Path.Combine(path, fileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(fullPath, json);
        }

        public static void TryRead<T>(string path, string fileName, out T output) where T : new()
        {
            string text;
            string fullPath = Path.Combine(path, fileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!File.Exists(fullPath))
            {
                File.Create(fullPath);
                output = new T();
                return;
            }
            text = File.ReadAllText(fullPath);
            output = JsonConvert.DeserializeObject<T>(text);
        }

        public static string Serialize(object value)
        {
            JsonConvert.SerializeObject(value, Formatting.Indented);
            return value.ToString();
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
