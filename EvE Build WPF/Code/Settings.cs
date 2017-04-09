using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using EvE_Build_WPF.Code.Containers;
using static System.IO.Path;

namespace EvE_Build_WPF.Code
{
    static class Settings
    {
        public static SettingObject settings { get; private set; }
        private static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + DirectorySeparatorChar + "EVE" +
                      DirectorySeparatorChar + "EvE-Build";
        private static readonly string FilePath = DirectoryPath + DirectorySeparatorChar + "settings.txt";
        private static bool isDone = false;

        public static void Load()
        {
            if (!isDone && (!Directory.Exists(DirectoryPath) || !File.Exists(FilePath)))
            {
                settings = CreateDefaultValues();
                isDone = true;

                Save();
                return;
            }

            string json = "";

            using (StreamReader file = new StreamReader(FilePath))
            {
                json = file.ReadToEnd();
            }

            settings = JsonConvert.DeserializeObject<SettingObject>(json);
        }

        public static void Save()
        {
            if (!File.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);

            string json = JsonConvert.SerializeObject(settings);

            using (StreamWriter file = new StreamWriter(FilePath))
            {
                file.Write(json);
            }
        }

        private static SettingObject CreateDefaultValues()
        {
            SettingObject defaultSettings = new SettingObject();

            //Major trade hubs
            defaultSettings.Stations.Add(new Station(30000142, "Jita"));
            defaultSettings.Stations.Add(new Station(30002187, "Amarr"));
            defaultSettings.Stations.Add(new Station(30002053, "Hek"));
            defaultSettings.Stations.Add(new Station(30002510, "Rens"));

            return defaultSettings;
        }

        public class SettingObject
        {
            public List<Station> Stations { get; set; }

            public SettingObject()
            {
                Stations = new List<Station>();
            }
        }
    }
}
