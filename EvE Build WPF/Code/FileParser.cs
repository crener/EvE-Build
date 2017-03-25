using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using EvE_Build_WPF.Code.Containers;
using YamlDotNet.RepresentationModel;

namespace EvE_Build_WPF.Code
{
    class FileParser
    {
        public FileParser()
        {
            string directory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static";

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        public Dictionary<int, Item> ParseBlueprintData()
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
                    Item candidate = new Item();

                    int blue;
                    if (int.TryParse((string)node.Key, out blue)) candidate.BlueId = blue;

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
                                if (int.TryParse(item.Value.ToString(), out temp) && temp != candidate.BlueId)
                                    candidate.BlueId = temp;
                                break;
                            default:
                                ;
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

                                        if (candidate.ProdId == 0)
                                        {
                                            ;
                                        }
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
                            case "research_material":
                            case "research_time":
                            case "invention":
                                //Ignore all for now!!
                                break;
                            default:
                                ;
                                break;
                        }
                    }

                    if (candidate.CheckValididty() && !items.ContainsKey(candidate.BlueId))
                    {
                        items.Add(candidate.BlueId, candidate);
                    }
                }
            }

            return items;
        }

        public void ParseItemDetails(ref Dictionary<int, Item> itemCollection)
        {
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static" + Path.DirectorySeparatorChar + "typeIDs.yaml";
            if (!File.Exists(path)) throw new FileNotFoundException();

            using (StreamReader data = new StreamReader(path))
            {
                YamlStream file = new YamlStream();
                file.Load(data);

                foreach (KeyValuePair<YamlNode, YamlNode> node in (YamlMappingNode)file.Documents[0].RootNode)
                {
                    int blue;
                    Item current;

                    if (!int.TryParse((string)node.Key, out blue) || blue == 0) continue;
                    if (!itemCollection.TryGetValue(blue, out current)) continue;

                    YamlMappingNode currentEntry = (YamlMappingNode)node.Value;

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
                                if (item.Value.ToString() == "")
                                    current.Name = item.Value["en"].ToString();
                                break;
                            case "raceID":
                                current.Race = (Race)Enum.Parse(typeof(Race), item.Value.ToString());
                                break;
                            case "marketGroupID":
                                if (int.TryParse(item.Value.ToString(), out temp))
                                    current.MarketGroupId = temp;
                                break;
                            case "basePrice":
                                if (int.TryParse(item.Value.ToString(), out temp))
                                    current.BasePrice = temp;
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
                                {
                                    itemCollection.Remove(current.BlueId);
                                    continue;
                                }
                                break;
                            case "portionSize":
                            //if (int.TryParse(item.Value.ToString(), out temp))
                            //    candidate. = temp;
                            //break;
                            case "volume":
                            case "traits":
                            case "masteries":
                            case "radius":
                            case "graphicID":
                            case "mass":
                            case "sofMaterialSetID":
                            case "soundID":
                            case "description":
                            case "capacity":
                            case "iconID":
                                //Ignore all for now
                                break;
                        }
                    }
                }
            }
        }

        public List<MarketItem> ParseMarketGroupData()
        {
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static" +
                          Path.DirectorySeparatorChar + "invMarketGroups.csv";
            if (!File.Exists(path)) throw new FileNotFoundException();

            List<MarketItem> collection = new List<MarketItem>();

            using (StreamReader data = new StreamReader(path))
            {
                string line = data.ReadLine();

                while (!data.EndOfStream)
                {
                    line = data.ReadLine();
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

            return collection;
        }
    }
}
