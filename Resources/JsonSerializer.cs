using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AutoFollow.Resources
{
    public static class JsonSerializer
    {
        public static string Serialize<T>(T value) where T : class
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof (T));
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, value);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Log.Info("Exception Serializing '{0}' - {1}", typeof (T), ex);
                return null;
            }
        }

        public static T Deserialize<T>(string json) where T : class
        {
            try
            {
                using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof (T));
                    return serializer.ReadObject(stream) as T;
                }
            }
            catch (Exception ex)
            {
                Log.Info("Exception Deserializing '{0}' - {1}", typeof (T), ex);
                return null;
            }
        }

        public static T Deserialize<T>(string json, T instance) where T : class, new()
        {
            try
            {
                using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof (T));                    
                    var newObj = serializer.ReadObject(stream) as T;
                    PropertyCopy.Copy(newObj, instance);
                    return instance;
                }
            }
            catch (Exception ex)
            {
                Log.Info("Exception Deserializing '{0}' - {1}", typeof (T), ex);
                return null;
            }
        }
    }

}
