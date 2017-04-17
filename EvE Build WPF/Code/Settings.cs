using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using EvE_Build_WPF.Code.Containers;
using static System.IO.Path;

namespace EvE_Build_WPF.Code
{
    static class Settings
    {
        public static event EventHandler settingsChanged;

        private static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + DirectorySeparatorChar + "EVE" +
                      DirectorySeparatorChar + "EvE-Build";
        private static SettingObject settings { get; set; }
        private static readonly string FilePath = DirectoryPath + DirectorySeparatorChar + "settings.txt";
        private static bool isDone;

        private static List<int> blockList { get; set; }

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
            blockList = new List<int>
                {
                    44102, //Defender Launcher
                    44111, //Tahron's Custom Heat Sink
                    44112, //Vadari's Custom Gyrostabilizer
                    44113, //Kaatara's Custom Magnetic Field Stabilizer
                    44114, //Torelle's Custom Magnetic Field Stabilizer
                    45010 //Focused Warp Scrambling Script
                };

            using (StreamReader file = new StreamReader(FilePath))
            {
                json = file.ReadToEnd();
            }

            settings = JsonConvert.DeserializeObject<SettingObject>(json);
        }

        public static Station[] Stations
        {
            get { return settings.Stations.ToArray(); }
        }

        public static int UpdateDelay
        {
            get { return settings.ThreadUpdateInterval; }
            set
            {
                settings.ThreadUpdateInterval = value;
                TriggerSettingChanged();
            }
        }

        public static int WebTimeout
        {
            get { return settings.WebRequestTimeout; }
            set
            {
                settings.WebRequestTimeout = value;
                TriggerSettingChanged();
            }
        }

        public static void RemoveStation(int id)
        {
            for (int i = 0; i < settings.Stations.Count; i++)
            {
                if (settings.Stations[i].StationId == id)
                {
                    settings.Stations.RemoveAt(i);
                    TriggerSettingChanged();
                    return;
                }
            }
        }

        public static bool isItemBlocked(int itemId)
        {
            return blockList.Contains(itemId);
        }

        public static void AddStation(Station newStation)
        {
            settings.Stations.Add(newStation);
            TriggerSettingChanged();
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

        private static void TriggerSettingChanged()
        {
            if (settingsChanged == null) return;

            settingsChanged(settings, EventArgs.Empty);
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
            public int ThreadUpdateInterval { get; set; }
            public int WebRequestTimeout { get; set; }

            public SettingObject()
            {
                Stations = new List<Station>();
                ThreadUpdateInterval = 60;
                WebRequestTimeout = 9;
            }
        }
    }
}
