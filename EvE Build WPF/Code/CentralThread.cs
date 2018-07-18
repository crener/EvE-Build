using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using EvE_Build_WPF.Code.Containers;

namespace EvE_Build_WPF.Code
{
    class CentralThread
    {
        //private static readonly string requestUrl = "http://api.eve-central.com/api/marketstat?&usesystem=";
        private static readonly string requestUrl = "http://api.evemarketer.com/ec/marketstat?&usesystem=";
        private static readonly string typeSeparator = "&typeid=";
        private static readonly int singleFetchAmount = 100;
        private static readonly int averageIdLength = 6;
        public static event EventHandler stationDataUpdated;

        private ConcurrentDictionary<int, MaterialItem> materials;
        private ConcurrentDictionary<int, Item> items;
        private Station[] stations;
        private int updateDelay;
        private int timeout;

        public CentralThread(ref ConcurrentDictionary<int, MaterialItem> materials, ref ConcurrentDictionary<int, Item> items)
        {
            this.materials = materials;
            this.items = items;

            Settings.SettingsChanged += RefreshSettings;
            RefreshSettings(); //initial load of settings

            Thread thread = new Thread(UpdateCycle)
            {
                Name = "Fetcher",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            thread.Start();
        }

        private void RefreshSettings(object sender, EventArgs eventArgs)
        {
            RefreshSettings();
        }

        private void RefreshSettings()
        {
            stations = Settings.Stations;
            updateDelay = Settings.UpdateDelay * 1000;
            timeout = Settings.WebTimeout * 1000;
        }

        private async void UpdateCycle()
        {
            do
            {
                //get all material data first
                foreach (Station station in stations)
                    MaterialCycle(station.StationId);

                foreach (Station station in stations)
                    ItemCycle(station.StationId);

                await Task.Delay(updateDelay);
            } while (true);
        }

        private void ItemCycle(int stationId)
        {
            int count = 0, cycle = 1;
            using (IEnumerator<KeyValuePair<int, Item>> enumerator = items.GetEnumerator())
            {
                do
                {
                    StringBuilder details =
                        new StringBuilder(requestUrl.Length + stationId.ToString().Length +
                                          singleFetchAmount * (typeSeparator.Length + averageIdLength));
                    details.Append(requestUrl).Append(stationId);

                    while (count < singleFetchAmount * cycle && count < items.Count && enumerator.MoveNext())
                    {
                        if (!Settings.isItemBlocked(enumerator.Current.Value.ProductId))
                            details.Append(typeSeparator + enumerator.Current.Value.ProductId);
                        ++count;
                    }
                    ++cycle;

                    ExtractPrices(WebRequest(details.ToString()), stationId);
                } while (count != items.Count);
            }

            if (stationDataUpdated != null) stationDataUpdated(null, EventArgs.Empty);
        }

        private void MaterialCycle(int stationId)
        {
            int count = 0, cycle = 1;
            using (IEnumerator<KeyValuePair<int, MaterialItem>> enumerator = materials.GetEnumerator())
            {
                do
                {
                    StringBuilder details =
                        new StringBuilder(requestUrl.Length + stationId.ToString().Length +
                                          singleFetchAmount * (typeSeparator.Length + averageIdLength));
                    details.Append(requestUrl).Append(stationId);

                    while (count < singleFetchAmount * cycle && count < materials.Count && enumerator.MoveNext())
                    {
                        details.Append(typeSeparator + enumerator.Current.Value.Id);
                        ++count;
                    }
                    ++cycle;

                    ExtractPrices(WebRequest(details.ToString()), stationId);
                } while (count != materials.Count);
            }

            if (stationDataUpdated != null) stationDataUpdated(null, EventArgs.Empty);
        }

        /// <summary>
        /// parses the eve central data via the xml reader
        /// </summary>
        /// <param name="xmlData">data returned from eve central</param>
        /// <param name="currentStation">the int id of the station that the data is from</param>
        private void ExtractPrices(string xmlData, int currentStation)
        {
            if (string.IsNullOrEmpty(xmlData)) return;
            XmlReader reader = XmlReader.Create(new StringReader(xmlData));
            if (reader.ReadState == ReadState.Error) return;

            try
            {
                while (reader.Read())
                {
                    string idLiteral;
                    if (reader.HasAttributes && reader.Name == "type" && (idLiteral = reader.GetAttribute("id")) != null)
                    {
                        int id;
                        IEveCentralItem item;
                        if (int.TryParse(idLiteral, out id))
                        {
                            if (items.ContainsKey(id)) item = items[id];
                            else if (materials.ContainsKey(id)) item = materials[id];
                            else continue;
                        }
                        else continue;

                        while (reader.Read() && (reader.Name != "type" || reader.NodeType != XmlNodeType.EndElement))
                        {
                            if (reader.Name == "buy" && reader.NodeType == XmlNodeType.Element)
                                item.UpdateBuyCost(currentStation, ExtractAttributeValue(reader, "max"));
                            else if (reader.Name == "sell" && reader.NodeType == XmlNodeType.Element)
                                item.UpdateSellPrice(currentStation, ExtractAttributeValue(reader, "min"));
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private decimal ExtractAttributeValue(XmlReader reader, string targetAttribute)
        {
            int depth = reader.Depth;
            while (reader.Read() && (reader.Depth != depth || reader.NodeType == XmlNodeType.EndElement))
            {
                if (reader.Name == targetAttribute)
                {
                    reader.Read();
                    decimal cost;
                    if (decimal.TryParse(reader.Value, out cost))
                        return cost;
                    break;
                }
            }

            return 0m;
        }

        private string WebRequest(string url)
        {
            try
            {
                HttpWebRequest request = System.Net.WebRequest.CreateHttp(url);
                request.Method = "GET";
                request.Timeout = timeout;

                string responseString;

                using (WebResponse response = request.GetResponse())
                using (StreamReader str = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    responseString = str.ReadToEnd();

                return responseString;
            }
            catch (WebException) { }
            catch (NullReferenceException) { }

            return "";
        }
    }
}
