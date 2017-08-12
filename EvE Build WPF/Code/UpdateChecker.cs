using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Core;
using Newtonsoft.Json;

namespace EvE_Build_WPF.Code
{
    public static class UpdateChecker
    {
        private const float version = 0.6f;
        private static readonly string UpdateUrl = "http://build.martinkruger.me?v=" + version.ToString("N3");
        private const string DownloadUrl = "https://github.com/crener/EvE-Build/releases";


        public static void CheckForUpdates()
        {
            string updateJson = downloadJson();
            updateData info = JsonConvert.DeserializeObject<updateData>(updateJson);

            //check if program needs to be updated
            if (info.ToolUpdateVersion > version)
            {
                MessageBoxResult answer = MessageBox.Show(
                    "There is a new version of EvE Build avaliable. Would you like to go to the download page?",
                    "Update avaliable", MessageBoxButton.YesNo);

                if (answer == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(DownloadUrl);
                    return;
                }
            }

            //check for new eve data
            if (info.ApiUpdateTime > File.GetCreationTime(FileParser.blueprintsFile))
            {
                MessageBoxResult answer = MessageBox.Show(
                    "There are new Eve Online files avaliable. Would you like to download them?",
                    "Update avaliable", MessageBoxButton.YesNo);

                if (answer == MessageBoxResult.Yes)
                    DownloadEveFiles(info);
            }
        }

        public static void DownloadEveFiles()
        {
            string json = downloadJson();
            if (downloadJson() == null)
                throw new Exception("Could not download EvE Json data");

            DownloadEveFiles(JsonConvert.DeserializeObject<updateData>(json));
        }

        private static void DownloadEveFiles(updateData data)
        {
            WebRequest request = WebRequest.Create(data.ApiUpdateLink);

            if (!Directory.Exists("static")) Directory.CreateDirectory("static");

            using (WebClient client = new WebClient())
            {
                char s = Path.DirectorySeparatorChar;

                //get official Static Export
                try
                {
                    client.DownloadFile(data.ApiUpdateLink, "EveStaticData.zip");
                    ZipFile.ExtractToDirectory("EveStaticData.zip", "temp");

                    if (Directory.Exists("temp" + s + "sde" + s + "fsd"))
                    {
                        {
                            string path = "temp" + s + "sde" + s + "fsd" + s + "blueprints.yaml";
                            if (File.Exists(FileParser.blueprintsFile)) File.Delete(FileParser.blueprintsFile);
                            File.Move(path, FileParser.blueprintsFile);
                        }

                        {
                            string path = "temp" + s + "sde" + s + "fsd" + s + "typeIDs.yaml";
                            if (File.Exists(FileParser.typeIdFile)) File.Delete(FileParser.typeIdFile);
                            File.Move(path, "static" + s + "typeIDs.yaml");
                        }
                    }
                }
                finally
                {
                    if (File.Exists("EveStaticData.zip")) File.Delete("EveStaticData.zip");
                    if (Directory.Exists("temp")) Directory.Delete("temp", true);
                }

                //get FuzzWorks market data information
                try
                {
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    client.DownloadFile("http://www.fuzzwork.co.uk/dump/latest/invMarketGroups.csv.bz2", "inv.bz2");

                    byte[] buffer = new byte[4096];

                    using (Stream streamIn = new FileStream("inv.bz2", FileMode.Open, FileAccess.Read))
                    using (var gzipStream = new BZip2InputStream(streamIn))
                    {
                        using (var fileStreamOut = File.Create(FileParser.marketGroupFile))
                        {
                            StreamUtils.Copy(gzipStream, fileStreamOut, buffer);
                        }
                    }
                }
                finally
                {
                    if(File.Exists("inv.bz2")) File.Delete("inv.bz2");
                }
            }
        }

        private static string downloadJson()
        {
            try
            {
                WebRequest request = WebRequest.Create(UpdateUrl);
                WebResponse response = request.GetResponse();
                string result;

                using (StreamReader data = new StreamReader(response.GetResponseStream()))
                    result = data.ReadToEnd();

                return result;
            }
            catch (WebSocketException) { }

            return null;
        }

        private class updateData
        {
            public DateTime ToolUpdateTime { get; set; }
            public float ToolUpdateVersion { get; set; }

            public DateTime ApiUpdateTime { get; set; }
            public string ApiUpdateLink { get; set; }
        }
    }
}
