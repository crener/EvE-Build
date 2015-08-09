using System;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace EvE_Build
{
    public class YAML
    {
        public Item[] ImportData(string blueprintsFile, string nameFile)
        {
            Item[] Items = new Item[YdnItemSetup(blueprintsFile)];

            YdnItemImport(blueprintsFile, ref Items);
            YdnSetName(nameFile, "en", ref Items);
            return Items;
        }

        int YdnItemSetup(string fileLocation)
        {
            int itemCount = 0;
            StreamReader file = new StreamReader(fileLocation);
            YamlStream yaml = new YamlStream();
            yaml.Load(file);

            var map = (YamlMappingNode)yaml.Documents[0].RootNode;
            foreach (var entry in map.Children)
            {

                //add one for each itme in the document to work out how many 
                //item object to make for the data import
                ++itemCount;
            }
            return itemCount;
        }

        void YdnItemImport(string fileLocation, ref Item[] items)
        {
            //get input file
            StreamReader file = new StreamReader(fileLocation);
            YamlStream yaml = new YamlStream();
            yaml.Load(file);

            int itemCount = 0;
            var map = (YamlMappingNode)yaml.Documents[0].RootNode;
            foreach (var entry in map.Children)
            {
                //create all the values that an item needs
                int blueprintID = 0,
                    copyTime = -1,
                    //METime = -1,
                    //TETime = -1,
                    prodLmt = -1,
                    itemID = 0,
                    prodQty = 1,
                    prodTime = 0;
                    //invID = 0,
                    //invQty = 1,
                    //invTime = 0;
                int[,] prodMats = new int[20, 2],
                    invMats = new int[6, 2],
                    copyMats = new int[5, 2],
                    prodskills = new int[8, 2],
                    MEskills = new int[8, 2],
                    TEskills = new int[8, 2],
                    copyskills = new int[5, 2];
                //float invProb = 0f;

                blueprintID = Int32.Parse(((YamlScalarNode)entry.Key).Value);
                var currentEntry = (YamlMappingNode)map.Children[entry.Key];
                YamlMappingNode activities = map;

                foreach(var item in currentEntry){
                    if ((item.Key).ToString() == "activities")
                    {
                        activities = (YamlMappingNode)currentEntry.Children[item.Key];
                    }
                    else if ((item.Key).ToString() == "maxProductionLimit")
                    {
                        prodLmt = Int32.Parse((item.Value).ToString());
                    }
                }
                foreach (var activity in activities)
                {
                    if ((activity.Key).ToString() == "manufacturing")
                    {
                        // start extracting manufacturing data
                        var manufacturing = (YamlMappingNode)activities.Children[activity.Key];
                        YamlSequenceNode materials, skills;
                        foreach (var man in manufacturing)
                        {
                            if ((man.Key).ToString() == "materials")
                            {
                                materials = (YamlSequenceNode)manufacturing.Children[man.Key];
                                int i = 0;
                                foreach (YamlMappingNode thing in materials)
                                {
                                    prodMats[i, 0] = Int32.Parse((thing.Children[new YamlScalarNode("quantity")]).ToString());
                                    prodMats[i, 1] = Int32.Parse((thing.Children[new YamlScalarNode("typeID")]).ToString());
                                    ++i;
                                }
                            }
                            else if ((man.Key).ToString() == "products")
                            {
                                int i = 0;
                                var prod = (YamlSequenceNode)manufacturing.Children[man.Key];
                                foreach (YamlMappingNode thing in prod)
                                {
                                    prodTime = Int32.Parse((thing.Children[new YamlScalarNode("quantity")]).ToString());
                                    itemID = Int32.Parse((thing.Children[new YamlScalarNode("typeID")]).ToString());
                                    ++i;
                                }
                            }
                            else if ((man.Key).ToString() == "skills")
                            {
                                skills = (YamlSequenceNode)manufacturing.Children[man.Key];
                                int i = 0;
                                foreach (YamlMappingNode thing in skills)
                                {
                                    prodskills[i, 0] = Int32.Parse((thing.Children[new YamlScalarNode("level")]).ToString());
                                    prodskills[i, 1] = Int32.Parse((thing.Children[new YamlScalarNode("typeID")]).ToString());
                                    ++i;
                                }
                            }
                            else if ((man.Key).ToString() == "time")
                            {
                                prodTime = Int32.Parse((man.Value).ToString());
                            }
                        }
                    }
                    else if ((activity.Key).ToString() == "copying")
                    {
                        // start extracting manufacturing data
                        var manufacturing = (YamlMappingNode)activities.Children[activity.Key];
                        YamlSequenceNode materials, skills;
                        foreach (var man in manufacturing)
                        {
                            if ((man.Key).ToString() == "materials")
                            {
                                materials = (YamlSequenceNode)manufacturing.Children[man.Key];
                                int i = 0;
                                foreach (YamlMappingNode thing in materials)
                                {
                                    copyMats[i, 0] = Int32.Parse((thing.Children[new YamlScalarNode("quantity")]).ToString());
                                    copyMats[i, 1] = Int32.Parse((thing.Children[new YamlScalarNode("typeID")]).ToString());
                                    ++i;
                                }
                            }
                            else if ((man.Key).ToString() == "skills")
                            {
                                skills = (YamlSequenceNode)manufacturing.Children[man.Key];
                                int i = 0;
                                foreach (YamlMappingNode thing in skills)
                                {
                                    prodskills[i, 0] = Int32.Parse((thing.Children[new YamlScalarNode("level")]).ToString());
                                    prodskills[i, 1] = Int32.Parse((thing.Children[new YamlScalarNode("typeID")]).ToString());
                                    ++i;
                                }
                            }
                            else if ((man.Key).ToString() == "time")
                            {
                                prodTime = Int32.Parse((man.Value).ToString());
                            }
                        }
                    }
                }

                //pass in all the values that have been collected
                items[itemCount] = new Item(blueprintID, itemID);
                items[itemCount].setProdskills(prodskills);
                items[itemCount].setProdMats(prodMats);
                items[itemCount].setProdTime(prodTime);
                items[itemCount].setProdQty(prodQty);
                items[itemCount].setProdLimit(prodLmt);
                items[itemCount].setCopyTime(copyTime);
                items[itemCount].setCopySkills(copyskills);
                items[itemCount].setCopyMats(copyMats);

                ++itemCount;
            }
            itemCount = 0;
            file.Close();
        }

        void YdnSetName(string fileLocation, string language, ref Item[] items)
        {
            StreamReader file = new StreamReader(fileLocation);
            YamlStream yaml = new YamlStream();
            yaml.Load(file);

            int itemID, itemPosition = 0;
            bool found = false;
            var map = (YamlMappingNode)yaml.Documents[0].RootNode;
            //figure out if there is an item for the current entry
            foreach (var entry in map.Children)
            {
                itemID = Int32.Parse((entry.Key).ToString());
                for (int i = 0; i < items.Length && found == false; ++i){
                    if (items[i].getTypeID() == itemID)
                    {
                        //item has been located 
                        found = true;
                        itemPosition = i;
                    }
                }
                if (found == false){
                    //item couldn't be found do move onto the next item
                    continue;
                }

                //time to get the name value of the item
                var item = (YamlMappingNode)map.Children[entry.Key];
                YamlNode nameNode = new YamlScalarNode("name");
                var name = (YamlMappingNode)item.Children[nameNode];
                string itemName = "";

                foreach (var langName in name)
                {
                    if ((langName.Key).ToString() == language)
                    {
                        itemName = (langName.Value).ToString();
                    }
                }

                //set the item name
                items[itemPosition].setName(itemName);

                //reset
                found = false;
            }
        }
        public string[] YdnNameFromID(string fileLocation, int[] type, string language)
        {

            int[] typeId = type;
            string[] name = new string[typeId.Length];
            int r = 0;

            StreamReader file = new StreamReader(fileLocation);
            YamlStream yaml = new YamlStream();
            yaml.Load(file);

            int itemID, itemPosition = 0;
            bool found = false;
            var map = (YamlMappingNode)yaml.Documents[0].RootNode;
            //figure out if there is an item for the current entry
            foreach (var entry in map.Children)
            {
                itemID = Int32.Parse((entry.Key).ToString());
                for (int i = 0; i < typeId.Length && found == false; ++i)
                {
                    if (typeId[i] == itemID)
                    {
                        //item has been located 
                        found = true;
                        itemPosition = i;
                    }
                }
                if (found == false)
                {
                    //item couldn't be found do move onto the next item
                    continue;
                }

                //time to get the name value of the item
                var item = (YamlMappingNode)map.Children[entry.Key];
                //YamlNode nameNode = new YamlScalarNode("name");
                var nameNode = (YamlMappingNode)item.Children[(new YamlScalarNode("name"))];
                string itemName = "";

                foreach (var langName in nameNode)
                {
                    if ((langName.Key).ToString() == language)
                    {
                        itemName = (langName.Value).ToString();
                        break;
                    }
                }

                //set the item name
                name[r] = itemName;
                ++r;

                //reset
                found = false;
            }

            return name;
        }
        public int[] YdnMatType(Item[] items)
        {
            int[] typeId = new int[items.Length - 1];
            int r = 0;

            foreach (var item in items)
            {
                int[,] materials = item.getProdMats();
                for (int i = 0; i < (materials.Length / 2) - 1; ++i)
                {
                    //find valid material
                    if (materials[i, 1] != 0)
                    {
                        //check if the materials has already been listed
                        bool found = false;
                        foreach (var type in typeId)
                        {
                            if (materials[i, 1] == type)
                            {
                                //item already exists
                                found = true;
                                break;
                            }
                        }

                        //check the material isn't an item
                        foreach (var thing in items)
                        {
                            if (thing.getTypeID() == materials[i, 1])
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found == false)
                        {
                            typeId[r] = materials[i, 1];
                            ++r;
                        }
                    }
                }
            }

            //remove null values
            int[] temp = new int[r];
            for (int i = 0; i < r; ++i)
            {
                temp[i] = typeId[i];
            }
            typeId = new int[r];
            typeId = temp;

            return typeId;
        }
        public int[] YdnCopyType(Item[] items)
        {
            int[] typeId = new int[items.Length - 1];
            int r = 0;

            foreach (var item in items)
            {
                int[,] materials = item.getCopyMats();
                for (int i = 0; i < (materials.Length / 2) - 1; ++i)
                {
                    //find valid material
                    if (materials[i, 1] != 0)
                    {
                        //check if the materials has already been listed
                        bool found = false;
                        foreach (var type in typeId)
                        {
                            if (materials[i, 1] == type)
                            {
                                //item already exists
                                found = true;
                                break;
                            }
                        }

                        //check the material isn't an item
                        foreach (var thing in items)
                        {
                            if (thing.getTypeID() == materials[i, 1])
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found == false)
                        {
                            typeId[r] = materials[i, 1];
                            ++r;
                        }
                    }
                }
            }

            //remove null values
            int[] temp = new int[r];
            for (int i = 0; i < r; ++i)
            {
                temp[i] = typeId[i];
            }
            typeId = new int[r];
            typeId = temp;

            return typeId;
        }
        public int[] YdnGetAllSkills(Item[] items)
        {
            int[] copySkills = YdnCopySkillType(items),
                prodSkills = YdnMatSkillType(items),
                output = new int[(copySkills.Length - 1) + (prodSkills.Length - 1)];

            for (int i = 0; i < copySkills.Length - 1 ; ++i)
            {
                output[i] = copySkills[i];
            }

            for (int i = 0; i < prodSkills.Length - 1; ++i)
            {
                output[(copySkills.Length - 1) + i] = prodSkills[i];
            }

            return output;
        }
        public int[] YdnCopySkillType(Item[] items)
        {
            int[] typeId = new int[items.Length - 1];
            int r = 0;

            foreach (var item in items)
            {
                int[,] materials = item.getCopySkill();

                if (materials[0, 0] == 0)
                {
                    continue; 
                }

                for (int i = 0; i < (materials.Length / 2) - 1; ++i)
                {
                    //find valid material
                    if (materials[i, 1] != 0)
                    {
                        //check if the materials has already been listed
                        bool found = false;
                        foreach (var type in typeId)
                        {
                            if (materials[i, 1] == type)
                            {
                                //item already exists
                                found = true;
                                break;
                            }
                        }

                        //check the material isn't an item
                        foreach (var thing in items)
                        {
                            if (thing.getTypeID() == materials[i, 1])
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found == false)
                        {
                            typeId[r] = materials[i, 1];
                            ++r;
                        }
                    }
                }
            }

            if (r == 0)
            {
                ++r;
            }

            //remove null values
            int[] temp = new int[r];
            for (int i = 0; i < r; ++i)
            {
                temp[i] = typeId[i];
            }
            typeId = new int[r];
            typeId = temp;

            return typeId;
        }
        public int[] YdnMatSkillType(Item[] items)
        {
            int[] typeId = new int[items.Length - 1];
            int r = 0;

            foreach (var item in items)
            {
                int[,] materials = item.getProdSkill();

                if (materials[0, 0] == 0)
                {
                    continue;
                }

                for (int i = 0; i < (materials.Length / 2) - 1; ++i)
                {
                    //find valid material
                    if (materials[i, 1] != 0)
                    {
                        //check if the materials has already been listed
                        bool found = false;
                        foreach (var type in typeId)
                        {
                            if (materials[i, 1] == type)
                            {
                                //item already exists
                                found = true;
                                break;
                            }
                        }

                        //check the material isn't an item
                        foreach (var thing in items)
                        {
                            if (thing.getTypeID() == materials[i, 1])
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found == false)
                        {
                            typeId[r] = materials[i, 1];
                            ++r;
                        }
                    }
                }
            }

            //remove null values
            int[] temp = new int[r];
            for (int i = 0; i < r; ++i)
            {
                temp[i] = typeId[i];
            }
            typeId = new int[r];
            typeId = temp;

            return typeId;
        }
    }
}