using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;

namespace AutoFollow.Resources
{
    public class FileStore<T> where T : class, new()
    {
        public FileStore()
        {
            var type = typeof(T);
            FilenameWithoutExtension = type.Name;
            Source = InitializeModel(new T());
            Load();
        }

        public string SaveFilePath => Path.Combine(FileUtils.SettingsFolder, FilenameWithoutExtension + ".json");

        public delegate void FileStoreEvent();
        public event FileStoreEvent Loaded = () => {};
        public event FileStoreEvent Saved = () => {};

        public void Load()
        {
            var json = FileUtils.ReadFromTextFile(SaveFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                JsonSerializer.Deserialize(json, Source);                
                Loaded();
            }
        }

        public void Save()
        {
            var result = JsonSerializer.Serialize(Source);
            FileUtils.WriteToTextFile(SaveFilePath, result);
            Log.Info("{0} saved.", FilenameWithoutExtension);
            Saved();
        }

        public string FilenameWithoutExtension { get; set; }

        public T Source { get; set; }

        private static TA InitializeModel<TA>(TA model) where TA : new()
        {
            // Set to default with default value attribute
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(model))
            {
                var myAttribute = (DefaultValueAttribute)property.Attributes[typeof(DefaultValueAttribute)];
                if (myAttribute != null)
                {
                    property.SetValue(model, myAttribute.Value);
                }
            }

            // Create empty collections, nulls are annoying.
            foreach (var property in typeof(TA).GetProperties())
            {
                if (typeof (IEnumerable<>).IsAssignableFrom(property.PropertyType))
                {
                    var constructor = property.PropertyType.GetConstructor(Type.EmptyTypes);
                    property.SetValue(model, constructor != null ? constructor.Invoke(null) : null);
                }
            }

            return model;
        }
    }
}

