using Newtonsoft.Json;
using System.IO;

namespace audio_recorder_UI
{
    public static class JSONSerializer
    {
        /// <summary>
        /// Deserialize an object from a file
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="path">Path to file</param>
        /// <returns></returns>
        public static T Deserialize<T>(string path)
        {
            using (StreamReader file = new StreamReader(path))
            {
                T obj = JsonConvert.DeserializeObject<T>(file.ReadToEnd());
                file.Close();
                return obj;
            }
        }

        /// <summary>
        /// Serialize an object to a file
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <param name="obj">Object to serialize</param>
        public static void Serialize(string path, object obj)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(JsonConvert.SerializeObject(obj, Formatting.Indented));
                file.Close();
            }
        }
    }
}