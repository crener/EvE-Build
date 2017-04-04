using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using EvE_Build_WPF.Code.Containers;
using YamlDotNet.RepresentationModel;

namespace EvE_Build_WPF.Code
{
    class FileParser
    {
        public static bool CheckSaveDirectoryExists()
        {
            string directory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return false;
            }
            return true;
        }

        public static Dictionary<int, Item> ParseBlueprintData()
        {
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static" +
                          Path.DirectorySeparatorChar + "blueprints.yaml";
            if (!File.Exists(path)) throw new FileNotFoundException();

            Dictionary<int, Item> items = new Dictionary<int, Item>();

            using (StreamReader data = new StreamReader(path))
            {
                YamlStream file = new YamlStream();
                file.Load(data);

                YamlMappingNode root = (YamlMappingNode)file.Documents[0].RootNode;

                foreach (KeyValuePair<YamlNode, YamlNode> node in root)
                {
                    try
                    {
                        Item candidate = new Item();

                        int blue;
                        if (int.TryParse((string)node.Key, out blue)) candidate.BlueprintId = blue;

                        YamlMappingNode currentEntry = (YamlMappingNode)node.Value;
                        YamlMappingNode activityNode = null;

                        foreach (var item in currentEntry)
                        {
                            int temp;
                            switch (item.Key.ToString())
                            {
                                case "activities":
                                    activityNode = (YamlMappingNode)item.Value;
                                    break;
                                case "maxProductionLimit":
                                    if (int.TryParse(item.Value.ToString(), out temp))
                                        candidate.ProdLimit = temp;
                                    break;
                                case "blueprintTypeID":
                                    if (int.TryParse(item.Value.ToString(), out temp) && temp != candidate.BlueprintId)
                                        candidate.BlueprintId = temp;
                                    break;
                            }
                        }

                        if (activityNode == null) continue;

                        foreach (KeyValuePair<YamlNode, YamlNode> activity in activityNode)
                        {
                            switch (activity.Key.ToString())
                            {
                                #region Manufacturing data extraction
                                case "manufacturing":
                                    if (activity.Value.AllNodes.Contains("materials"))
                                    {
                                        foreach (YamlNode mat in (YamlSequenceNode)activity.Value["materials"])
                                        {
                                            int type = int.Parse(mat["typeID"].ToString());
                                            long qty = long.Parse(mat["quantity"].ToString());

                                            candidate.AddProductMaterial(type, qty);
                                        }
                                    }

                                    if (activity.Value.AllNodes.Contains("products"))
                                    {
                                        foreach (YamlNode prod in (YamlSequenceNode)activity.Value["products"])
                                        {
                                            candidate.ProdId = int.Parse(prod["typeID"].ToString());
                                            candidate.ProdQty = int.Parse(prod["quantity"].ToString());
                                        }
                                    }

                                    if (activity.Value.AllNodes.Contains("skills"))
                                    {
                                        foreach (YamlNode skill in (YamlSequenceNode)activity.Value["skills"])
                                        {
                                            int type = int.Parse(skill["typeID"].ToString());
                                            int level = int.Parse(skill["level"].ToString());

                                            candidate.AddProductSkill(type, level);
                                        }
                                    }

                                    if (activity.Value.AllNodes.Contains("time"))
                                    {
                                        candidate.ProdTime = int.Parse(activity.Value["time"].ToString());
                                    }
                                    break;
                                #endregion

                                #region Copy data extraction
                                case "copying":
                                    if (activity.Value.AllNodes.Contains("materials"))
                                    {
                                        foreach (YamlNode mat in (YamlSequenceNode)activity.Value["materials"])
                                        {
                                            int type = int.Parse(mat["typeID"].ToString());
                                            long qty = long.Parse(mat["quantity"].ToString());

                                            candidate.AddCopyMaterial(type, qty);
                                        }
                                    }

                                    if (activity.Value.AllNodes.Contains("skills"))
                                    {
                                        foreach (YamlNode skill in (YamlSequenceNode)activity.Value["skills"])
                                        {
                                            int type = int.Parse(skill["typeID"].ToString());
                                            int level = int.Parse(skill["level"].ToString());

                                            candidate.AddCopySkill(type, level);
                                        }
                                    }

                                    if (activity.Value.AllNodes.Contains("time"))
                                    {
                                        candidate.CopyTime = int.Parse(activity.Value["time"].ToString());
                                    }
                                    break;
                                #endregion
#if DEBUG
                                case "research_material":
                                case "research_time":
                                case "invention":
                                    //Ignore all for now!!
                                    break;
#endif
                            }
                        }

                        if (candidate.CheckValididty() && !items.ContainsKey(candidate.BlueprintId))
                        {
                            items.Add(candidate.BlueprintId, candidate);
                        }
                    }
                    catch (Exception e)
                    {
                        //keep the conversion process from dying from an error in one of the nodes
                        Console.WriteLine(e);
                    }
                }
            }

            return items;
        }

        public static void ParseItemDetails(ref Dictionary<int, Item> itemCollection)
        {
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static" + Path.DirectorySeparatorChar + "typeIDs.yaml";
            if (!File.Exists(path)) throw new FileNotFoundException();

            using (StreamReader data = new StreamReader(path))
            {
                YamlStream file = new YamlStream();
                file.Load(data);

                YamlMappingNode rootNode = (YamlMappingNode)file.Documents[0].RootNode;

                foreach (KeyValuePair<YamlNode, YamlNode> node in rootNode)
                {
                    int blue;
                    Item current;
                    bool blueprint = true;

                    if (!int.TryParse((string)node.Key, out blue) || blue == 0) continue;
                    if (!itemCollection.TryGetValue(blue, out current))
                    {
                        foreach (KeyValuePair<int, Item> pair in itemCollection)
                        {
                            if (pair.Value.ProdId == blue)
                            {
                                current = pair.Value;
                                blueprint = false;
                                break;
                            }
                        }

                        if (current == null) continue;
                    }

                    YamlMappingNode currentEntry = (YamlMappingNode)node.Value;

                    if (blueprint)
                    {
                        foreach (KeyValuePair<YamlNode, YamlNode> item in currentEntry)
                        {
                            int temp;
                            switch (item.Key.ToString())
                            {
                                case "groupID":
                                    if (int.TryParse(item.Value.ToString(), out temp))
                                        current.GroudId = temp;
                                    break;
                                case "name":
                                    if (item.Value.AllNodes.Contains("en") && item.Value["en"].ToString() != "")
                                        current.BlueprintName = item.Value["en"].ToString();
                                    break;
                                case "raceID":
                                    current.Race = (Race)Enum.Parse(typeof(Race), item.Value.ToString());
                                    break;
                                case "marketGroupID":
                                    if (int.TryParse(item.Value.ToString(), out temp))
                                        current.MarketGroupId = temp;
                                    break;
                                case "basePrice":
                                    decimal longTemp;
                                    if (decimal.TryParse(item.Value.ToString(), out longTemp))
                                        current.BlueprintBasePrice = longTemp;
                                    break;
                                case "factionID":
                                    if (int.TryParse(item.Value.ToString(), out temp))
                                        current.FactionId = temp;
                                    break;
                                case "sofFactionName":
                                    current.SofFaction = item.Value.ToString();
                                    break;
                                case "published":
                                    if (!bool.Parse(item.Value.ToString()))
                                        itemCollection.Remove(current.BlueprintId);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<YamlNode, YamlNode> item in currentEntry)
                        {
                            switch (item.Key.ToString())
                            {
                                case "name":
                                    if (item.Value.AllNodes.Contains("en") && item.Value["en"].ToString() != "")
                                        current.ProdName = item.Value["en"].ToString();
                                    break;
                                case "basePrice":
                                    decimal longTemp;
                                    if (decimal.TryParse(item.Value.ToString(), out longTemp))
                                        current.ProdBasePrice = longTemp;
                                    break;
                                case "published":
                                    if (!bool.Parse(item.Value.ToString()))
                                        itemCollection.Remove(current.BlueprintId);
                                    break;
                                case "volume":
                                    decimal volume;
                                    if(decimal.TryParse(item.Value.ToString(), out volume))
                                        current.ProdVolume = volume;
                                    break;
                                case "factionID":
                                    int faction;
                                    if(int.TryParse(item.Value.ToString(), out faction))
                                        current.FactionId = faction;
                                    break;
                            }
                        }
                    }
                }
            }

            //make sure that all items actually have market data
            List<int> killList = new List<int>();
            foreach (KeyValuePair<int, Item> item in itemCollection)
                if (!item.Value.CheckItemViability()) killList.Add(item.Key);

            foreach (int i in killList) itemCollection.Remove(i);
        }

        public static List<MarketItem> ParseMarketGroupData()
        {
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static" +
                          Path.DirectorySeparatorChar + "invMarketGroups.csv";
            if (!File.Exists(path)) throw new FileNotFoundException();

            List<MarketItem> collection = new List<MarketItem>();
            try
            {
                using (StreamReader data = new StreamReader(path))
                {
                    string line = data.ReadLine();

                    while (!data.EndOfStream)
                    {
                        line = data.ReadLine();
                        if (line == null) continue;
                        string[] lineElements = line.Split(',');

                        if (lineElements[0].Contains("\"")) continue;

                        MarketItem item = new MarketItem();
                        item.MarketId = int.Parse(lineElements[0]);
                        item.ParentGroupId = lineElements[1] == "None" ? int.Parse(lineElements[0]) : -1;
                        item.Name = lineElements[2];
                        item.Description = lineElements[3];

                        collection.Add(item);
                    }
                }
            }
            catch (AccessViolationException ave)
            {
                Console.WriteLine(ave);
            }

            return collection;
        }
    }
}
