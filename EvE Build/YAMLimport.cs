using System;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace EvE_Build
{
    public class YAML
    {
        int[] blacklist = new int[79];
        YamlStream blue, name;

        public YAML()
        {
            #region blacklistData
            blacklist[0] = 3927;
            blacklist[1] = 4364;
            blacklist[2] = 4389;
            blacklist[3] = 32812;
            blacklist[4] = 33624;
            blacklist[5] = 33626;
            blacklist[6] = 33628;
            blacklist[7] = 33630;
            blacklist[8] = 33632;
            blacklist[9] = 33634;
            blacklist[10] = 33636;
            blacklist[11] = 33638;
            blacklist[12] = 33640;
            blacklist[13] = 33642;
            blacklist[14] = 33644;
            blacklist[15] = 33646;
            blacklist[16] = 33648;
            blacklist[17] = 33650;
            blacklist[18] = 33652;
            blacklist[19] = 33654;
            blacklist[20] = 33656;
            blacklist[21] = 33658;
            blacklist[22] = 33660;
            blacklist[23] = 33662;
            blacklist[24] = 33664;
            blacklist[25] = 33666;
            blacklist[26] = 33668;
            blacklist[27] = 33670;
            blacklist[28] = 33684;
            blacklist[29] = 33686;
            blacklist[30] = 33688;
            blacklist[31] = 33690;
            blacklist[32] = 33692;
            blacklist[33] = 33694;
            blacklist[34] = 33696;
            blacklist[35] = 33870;
            blacklist[36] = 33872;
            blacklist[37] = 33874;
            blacklist[38] = 33876;
            blacklist[39] = 33878;
            blacklist[40] = 33880;
            blacklist[41] = 33882;
            blacklist[42] = 33884;
            blacklist[43] = 34119;
            blacklist[44] = 34153;
            blacklist[45] = 34214;
            blacklist[46] = 34216;
            blacklist[47] = 34218;
            blacklist[48] = 34220;
            blacklist[49] = 34222;
            blacklist[50] = 34224;
            blacklist[51] = 34226;
            blacklist[52] = 34228;
            blacklist[53] = 34230;
            blacklist[54] = 34232;
            blacklist[55] = 34234;
            blacklist[56] = 34236;
            blacklist[57] = 34238;
            blacklist[58] = 34240;
            blacklist[59] = 34242;
            blacklist[60] = 34244;
            blacklist[61] = 34246;
            blacklist[62] = 34248;
            blacklist[63] = 34250;
            blacklist[64] = 34252;
            blacklist[65] = 34254;
            blacklist[66] = 34256;
            blacklist[67] = 34258;
            blacklist[68] = 34340;
            blacklist[69] = 34342;
            blacklist[70] = 34344;
            blacklist[71] = 34346;
            blacklist[72] = 34442;
            blacklist[73] = 34444;
            blacklist[74] = 34446;
            blacklist[75] = 681;
            blacklist[76] = 682;
            blacklist[77] = 3927;
            blacklist[78] = 935;
            #endregion

            StreamReader file = new StreamReader("StaticData/blueprints.Yaml");
            blue = new YamlStream();
            blue.Load(file);
            file.Close();

            file = new StreamReader("StaticData/typeIDs.yaml");
            name = new YamlStream();
            name.Load(file);
            file.Close();
        }
        public Item[] ImportData()
        {
            Item[] Items = new Item[YdnItemSetup(blue)];
            Items = YdnItemImport(blue, ref Items);

            YdnSetName(name, "en", ref Items);
            return Items;
        }
        int YdnItemSetup(YamlStream fileLocation)
        {
            int itemCount = 0;
            var map = (YamlMappingNode)fileLocation.Documents[0].RootNode;
            foreach (var entry in map.Children)
            {

                //add one for each itme in the document to work out how many 
                //item object to make for the data import
                ++itemCount;
            }
            return itemCount - (blacklist.Length - 1);
        }
        Item[] YdnItemImport(YamlStream fileLocation, ref Item[] items)
        {
            int itemCount = 0;
            //var map = (YamlMappingNode)yaml.Documents[0].RootNode;
            var map = (YamlMappingNode)fileLocation.Documents[0].RootNode;
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

                if (Blacklist(blueprintID))
                {
                    continue;
                }

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
                                    prodQty = Int32.Parse((thing.Children[new YamlScalarNode("quantity")]).ToString());
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
                Item potentialItem = new Item(blueprintID, itemID);
                potentialItem.setProdskills(prodskills);
                potentialItem.setProdMats(prodMats);
                potentialItem.setProdTime(prodTime);
                potentialItem.setProdQty(prodQty);
                potentialItem.setProdLimit(prodLmt);
                potentialItem.setCopyTime(copyTime);
                potentialItem.setCopySkills(copyskills);
                potentialItem.setCopyMats(copyMats);
                potentialItem.setProdLimit(prodLmt);

                if (QualityControl(potentialItem))
                {
                    items[itemCount] = potentialItem;
                    ++itemCount;
                }
            }
            itemCount = 0;
            //file.Close();

            //Remove Nulls
            int total = 0;
            foreach (Item item in items)
            {
                if (item != null)
                {
                    ++total;
                }
            }
            Item[] output = new Item[total];
            total = 0;
            foreach (Item thing in items)
            {
                if (thing != null)
                {
                    output[total] = thing;
                    ++total;
                }
            }
            return output;
        }
        void YdnSetName(YamlStream fileLocation, string language, ref Item[] items)
        {
            int itemID, itemPosition = 0;
            bool found = false;
            var map = (YamlMappingNode)fileLocation.Documents[0].RootNode;
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
                    for (int i = 0; i < items.Length && found == false; ++i)
                    {
                        if (items[i].getBlueprintTypeID() == itemID)
                        {
                            //item blueprint has been located 
                            YamlMappingNode price = (YamlMappingNode)map.Children[entry.Key];
                            foreach (var priceNode in price)
                            {
                                if (priceNode.Key.ToString() == "basePrice")
                                {
                                    string temp = priceNode.Value.ToString();
                                    temp = temp.Remove(temp.Length - 2, 1);
                                    Int64 blueprice = Convert.ToInt64(temp) * 10;

                                    items[i].setBlueprintPrice(blueprice);
                                }
                            }
                        }
                    }
                    continue;
                }

                string itemName = "";
                float volume = 0f,
                    mass = 0f;
                int groupID = 0,
                    marketGroupID = 0,
                    faction = 0,
                    race = 0;

                YamlMappingNode details = (YamlMappingNode)map.Children[entry.Key];
                foreach (var prop in details.Children)
                {
                    if (prop.Key.ToString() == "name")
                    {
                        var item = (YamlMappingNode)map.Children[entry.Key];
                        YamlNode nameNode = new YamlScalarNode("name");
                        var name = (YamlMappingNode)item.Children[nameNode];

                        foreach (var langName in name)
                        {
                            if ((langName.Key).ToString() == language)
                            {
                                itemName = (langName.Value).ToString();
                                items[itemPosition].setName(itemName);
                            }
                        }
                    }
                    else if (prop.Key.ToString() == "groupID")
                    {
                        groupID = Convert.ToInt32(prop.Value.ToString());
                        items[itemPosition].setGroupID(groupID);
                    }
                    else if (prop.Key.ToString() == "marketGroupID")
                    {
                        marketGroupID = Convert.ToInt32(prop.Value.ToString());
                        items[itemPosition].setMarketGroupID(marketGroupID);
                    }
                    else if (prop.Key.ToString() == "volume")
                    {
                        volume = Convert.ToSingle(prop.Value.ToString());
                        items[itemPosition].setVolume(volume);
                    }
                    else if (prop.Key.ToString() == "mass")
                    {
                        mass = Convert.ToSingle(prop.Value.ToString());
                        items[itemPosition].setMass(mass);
                    }
                    else if (prop.Key.ToString() == "raceID")
                    {
                        race = Convert.ToInt32(prop.Value.ToString());
                        items[itemPosition].setRace(race);
                    }
                    else if (prop.Key.ToString() == "factionID")
                    {
                        faction = Convert.ToInt32(prop.Value.ToString());
                        items[itemPosition].setFaction(faction);
                    }
                }

                //reset
                found = false;
            }
        }
        public string[] YdnNameFromID(string fileLocation, int[] type, string language)
        {
            int[] typeId = type;
            string[] name = new string[typeId.Length];
            int r = 0;

            int itemID, itemPosition = 0;
            bool found = false;
            var map = (YamlMappingNode)this.name.Documents[0].RootNode;
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
                name[itemPosition] = itemName;
                ++r;

                //reset
                found = false;
            }

            return name;
        }
        public int[] YdnMatTypeint(Item[] items)
        {
            int[] typeId = new int[items.Length - 1];
            int r = 0;

            foreach (var item in items)
            {
                Int64[,] materials = item.getProdMats();
                for (int i = 0; i < (materials.Length / 2) - 1; ++i)
                {
                    //find valid material
                    if (materials[i, 1] != 0)
                    {
                        //check if the material has already been listed
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
                            typeId[r] = Convert.ToInt32(materials[i, 1]);
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
        public Material[] YdnMatTypeMat(Item[] items)
        {
            Material[] typeId = new Material[items.Length - 1];
            for (int i = 0; i < items.Length - 1; ++i)
            {
                typeId[i] = new Material();
            }
            int r = 0;

            foreach (var item in items)
            {
                Int64[,] materials = item.getProdMats();
                for (int i = 0; i < (materials.Length / 2) - 1; ++i)
                {
                    //find valid material
                    if (materials[i, 1] != 0)
                    {
                        //check if the material has already been listed
                        bool found = false;
                        for(int j = 0; j < typeId.Length; ++j)
                        {
                            if (materials[i, 1] == typeId[j].ID)
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

                        //add item to the material list
                        if (found == false)
                        {
                            typeId[r].ID = Convert.ToInt32(materials[i, 1]);
                            ++r;
                        }
                    }
                }
            }

            //remove null values
            Material[] temp = new Material[r];
            for (int i = 0; i < r; ++i)
            {
                temp[i] = typeId[i];
            }
            typeId = new Material[r];
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
        private bool QualityControl(Item thing)
        {
            //ensure the item has materials
            if (thing.getProdMats()[0, 0] == 0 && thing.getProdMats()[0, 1] == 0)
            {
                return false;
            }

            return true;
        }
        bool Blacklist(int check)
        {
            foreach (int banned in blacklist)
            {
                if (check == banned)
                {
                    return true;
                }
            }
            return false;
        }
        public void extractMaterialNames(ref Material[] mat, string fileLocation, string language){

            int[] typeId = new int[mat.Length - 1];
            for (int i = 0; i < mat.Length - 1; ++i )
            {
                typeId[i] = mat[i].ID;
            }

            int r = 0;
            int itemID, itemPosition = 0;
            bool found = false;
            var map = (YamlMappingNode)name.Documents[0].RootNode;
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
                YamlMappingNode details = (YamlMappingNode)map.Children[entry.Key];
                foreach (var prop in details.Children)
                {
                    if (prop.Key.ToString() == "name")
                    {
                        var item = (YamlMappingNode)map.Children[entry.Key];
                        YamlNode nameNode = new YamlScalarNode("name");
                        var name = (YamlMappingNode)item.Children[nameNode];

                        foreach (var langName in name)
                        {
                            if ((langName.Key).ToString() == language)
                            {
                                string itemName = (langName.Value).ToString();
                                mat[itemPosition].name = itemName;
                            }
                        }
                    }
                    else if (prop.Key.ToString() == "groupID")
                    {
                        int groupID = Convert.ToInt32(prop.Value.ToString());
                        mat[itemPosition].groupID = groupID;
                    }
                    else if (prop.Key.ToString() == "marketGroupID")
                    {
                        int marketGroupID = Convert.ToInt32(prop.Value.ToString());
                        mat[itemPosition].marketGroupID = marketGroupID;
                    }
                    else if (prop.Key.ToString() == "volume")
                    {
                        float volume = Convert.ToSingle(prop.Value.ToString());
                        mat[itemPosition].volume = volume;
                    }
                    else if (prop.Key.ToString() == "mass")
                    {
                        float mass = Convert.ToSingle(prop.Value.ToString());
                        mat[itemPosition].mass = mass;
                    }
                    else if (prop.Key.ToString() == "raceID")
                    {
                        int race = Convert.ToInt32(prop.Value.ToString());
                        mat[itemPosition].race = race;
                    }
                    else if (prop.Key.ToString() == "factionID")
                    {
                        int faction = Convert.ToInt32(prop.Value.ToString());
                        mat[itemPosition].faction = faction;
                    }
                }

                ++r;

                //reset
                found = false;
            }
        }
    }
}