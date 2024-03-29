﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EvE_Build_WPF.Code.Containers;
using YamlDotNet.RepresentationModel;

namespace EvE_Build_WPF.Code
{
    class FileParser
    {
        private static readonly char S = Path.DirectorySeparatorChar;
        private static readonly string CurrentDirectory = Directory.GetCurrentDirectory();

        public static readonly string BlueprintsFile = CurrentDirectory + S + "static" + S + "blueprints.yaml";
        public static readonly string TypeIdFile = CurrentDirectory + S + "static" + S + "typeIDs.yaml";
        public static readonly string MarketGroupFile = CurrentDirectory + S + "static" + S + "invMarketGroups.csv";

        private YamlMappingNode types;

        public Dictionary<int, Item> ParseBlueprintData()
        {
            if (!File.Exists(BlueprintsFile)) throw new FileNotFoundException();

            Dictionary<int, Item> items = new Dictionary<int, Item>();

            using (StreamReader data = new StreamReader(BlueprintsFile))
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

                        if(currentEntry["activities"] != null) activityNode = (YamlMappingNode)currentEntry["activities"];
                        else continue;

                        if (currentEntry["maxProductionLimit"] != null) candidate.ProductionLimit = int.Parse(currentEntry["maxProductionLimit"].ToString());
                        if(currentEntry["blueprintTypeID"] != null) candidate.BlueprintId = int.Parse(currentEntry["blueprintTypeID"].ToString());

                        ExtractItemData(activityNode, ref candidate);

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

        private void ExtractItemData(YamlMappingNode activityNode, ref Item candidate)
        {
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
                                candidate.ProductId = int.Parse(prod["typeID"].ToString());
                                candidate.ProductionQuantity = int.Parse(prod["quantity"].ToString());
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
                            candidate.ProductionTime = int.Parse(activity.Value["time"].ToString());
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
                }
            }
        }

        /*private void ExtractItemData(YamlMappingNode activityNode, ref Item candidate)
        {
            YamlNode tempNode;

            if((tempNode = activityNode["manufacturing"]) != null)
            {
                if (tempNode.AllNodes.Contains("materials"))
                {
                    foreach (YamlNode mat in (YamlSequenceNode)tempNode["materials"])
                    {
                        int type = int.Parse(mat["typeID"].ToString());
                        long qty = long.Parse(mat["quantity"].ToString());

                        candidate.AddProductMaterial(type, qty);
                    }
                }

                if (tempNode.AllNodes.Contains("products"))
                {
                    foreach (YamlNode prod in (YamlSequenceNode)tempNode["products"])
                    {
                        candidate.ProductId = int.Parse(prod["typeID"].ToString());
                        candidate.ProductionQuantity = int.Parse(prod["quantity"].ToString());
                    }
                }

                if (tempNode.AllNodes.Contains("skills"))
                {
                    foreach (YamlNode skill in (YamlSequenceNode)tempNode["skills"])
                    {
                        int type = int.Parse(skill["typeID"].ToString());
                        int level = int.Parse(skill["level"].ToString());

                        candidate.AddProductSkill(type, level);
                    }
                }

                if (tempNode.AllNodes.Contains("time"))
                {
                    candidate.ProductionTime = int.Parse(tempNode["time"].ToString());
                }
            }

            if((tempNode = activityNode["copying"]) != null)
            {
                if (tempNode.AllNodes.Contains("materials"))
                {
                    foreach (YamlNode mat in (YamlSequenceNode)tempNode["materials"])
                    {
                        int type = int.Parse(mat["typeID"].ToString());
                        long qty = long.Parse(mat["quantity"].ToString());

                        candidate.AddCopyMaterial(type, qty);
                    }
                }

                if (tempNode.AllNodes.Contains("skills"))
                {
                    foreach (YamlNode skill in (YamlSequenceNode)tempNode["skills"])
                    {
                        int type = int.Parse(skill["typeID"].ToString());
                        int level = int.Parse(skill["level"].ToString());

                        candidate.AddCopySkill(type, level);
                    }
                }

                if (tempNode.AllNodes.Contains("time"))
                {
                    candidate.ProductionTime = int.Parse(tempNode["time"].ToString());
                }
            }
        }
        */

        public  void ParseItemDetails(ref Dictionary<int, Item> itemCollection)
        {
            if (!File.Exists(TypeIdFile)) throw new FileNotFoundException();

            using (StreamReader data = new StreamReader(TypeIdFile))
            {
                YamlStream file = new YamlStream();
                file.Load(data);

                types = (YamlMappingNode)file.Documents[0].RootNode;

                foreach (KeyValuePair<YamlNode, YamlNode> node in types)
                {
                    int blue;
                    Item current;

                    if (!int.TryParse((string)node.Key, out blue) || blue == 0) continue;
                    if (!itemCollection.TryGetValue(blue, out current))
                    {
                        foreach (KeyValuePair<int, Item> pair in itemCollection)
                        {
                            if (pair.Value.ProductId == blue)
                            {
                                current = pair.Value;
                                break;
                            }
                        }

                        if (current == null) continue;
                    }

                    //blueprint data
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
                                if (item.Value.AllNodes.Contains("en") && item.Value["en"].ToString() != "")
                                    current.BlueprintName = item.Value["en"].ToString();
                                break;
                            case "raceID":
                                current.Race = (Race)Enum.Parse(typeof(Race), item.Value.ToString());
                                break;
                            case "basePrice":
                                decimal longTemp;
                                if (decimal.TryParse(item.Value.ToString(), out longTemp))
                                    current.BlueprintBasePrice = longTemp;
                                break;
                            case "factionID":
                                if (int.TryParse(item.Value.ToString(), out temp))
                                    current.FactionId = (Faction)Enum.Parse(typeof(Faction), item.Value.ToString());
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


                //product data
                foreach (Item item in itemCollection.Values)
                {
                    int id = item.ProductId;
                    YamlNode node;

                    if (types.Children.TryGetValue(new YamlScalarNode(id.ToString()), out node))
                    {
                        YamlMappingNode mapping = (YamlMappingNode)node;
                        foreach (KeyValuePair<YamlNode, YamlNode> allNodes in mapping.Children)
                        {
                            switch (allNodes.Key.ToString())
                            {
                                case "name":
                                    if (allNodes.Value.AllNodes.Contains("en") && allNodes.Value["en"].ToString() != "")
                                    {
                                        item.ProductName = allNodes.Value["en"].ToString();
                                        if (CheckNameForSubFaction(item.ProductName)) item.isSubFaction = true;
                                    }
                                    break;
                                case "basePrice":
                                    decimal longTemp;
                                    if (decimal.TryParse(allNodes.Value.ToString(), out longTemp))
                                        item.ProductBasePrice = longTemp;
                                    break;
                                case "published":
                                    if (!bool.Parse(allNodes.Value.ToString()))
                                        itemCollection.Remove(item.BlueprintId);
                                    break;
                                case "volume":
                                    decimal volume;
                                    if (decimal.TryParse(allNodes.Value.ToString(), out volume))
                                        item.ProductVolume = volume;
                                    break;
                                case "factionID":
                                    int faction;
                                    if (int.TryParse(allNodes.Value.ToString(), out faction))
                                        item.FactionId = (Faction)Enum.Parse(typeof(Faction), allNodes.Value.ToString());
                                    break;
                                case "sofFactionName":
                                    item.SofFaction = allNodes.Value.ToString();
                                    break;
                                case "marketGroupID":
                                    int temp;
                                    if (int.TryParse(allNodes.Value.ToString(), out temp))
                                        item.MarketGroupId = temp;
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
            if (!File.Exists(MarketGroupFile)) throw new FileNotFoundException();

            List<MarketItem> collection = new List<MarketItem>();
            try
            {
                using (StreamReader data = new StreamReader(MarketGroupFile))
                {
                    string line = data.ReadLine();

                    while (!data.EndOfStream)
                    {
                        line = data.ReadLine();
                        if (line == null) continue;
                        string[] lineElements = line.Split(',');

                        if (lineElements[0].Contains("\"")) continue;

                        MarketItem item = new MarketItem
                        {
                            MarketId = int.Parse(lineElements[0]),
                            ParentGroupId = lineElements[1] == "None" ? -1 : int.Parse(lineElements[1]),
                            Name = lineElements[2],
                            Description = lineElements[3]
                        };

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

        public ConcurrentDictionary<int, MaterialItem> GatherMaterials(ConcurrentDictionary<int, Item> items)
        {
            YamlNode nameKey = new YamlScalarNode("name");
            YamlNode langKey = new YamlScalarNode("en");
            ConcurrentDictionary<int, MaterialItem> materials = new ConcurrentDictionary<int, MaterialItem>();

            foreach (KeyValuePair<int, Item> item in items)
            {
                foreach (Material material in item.Value.ProductMaterial)
                {
                    if (!materials.ContainsKey(material.Type) && !items.ContainsKey(material.Type))
                    {
                        MaterialItem materialItem = new MaterialItem(material.Type);

                        try
                        {
                            YamlNode materialKey = new YamlScalarNode(material.Type.ToString());

                            materialItem.Name = types.Children[materialKey][nameKey][langKey].ToString();
                        }
                        catch (KeyNotFoundException)
                        {
                            materialItem.Name = "Unknown " + material.Type;
                        }

                        materials.AddOrUpdate(material.Type, materialItem, MaterialItem.merdge);
                    }
                }
            }

            return materials;
        }

        private static bool CheckNameForSubFaction(string name)
        {
            return name.Contains("True Sansha") ||
                name.Contains("Shadow Serpentis") ||
                name.Contains("Republic Fleet") ||
                name.Contains("Imperial Navy") ||
                name.Contains("Khanid Navy") ||
                name.Contains("Federation Navy") ||
                name.Contains("Dread Guristas") ||
                name.Contains("Dark Blood") ||
                name.Contains("Ammatar Navy") ||
                name.Contains("Domination") ||
                name.Contains("Civilian") ||
                name.Contains("CONCORD") ||
                name.Contains("Caldari Navy");
        }
    }
}
