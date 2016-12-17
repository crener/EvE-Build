using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace YAMLImportTester
{
    class Program
    {

        Item[] items;
        int itemCount = 0;

        const string mats = "materials",
            prod = "products",
            skill = "skills:",
            time = "time",
            quantity = "quantity",
            type = "typeID";

        static void Main(string[] args)
        {
            Program app = new Program();
            app.DoStuff();
        }

        void DoStuff()
        {

            items = new Item[YdnItemSetup("b.yaml") + 1];
            //getFile("b.yaml", 0);
            //Console.WriteLine("Done, importing item data");
            YdnItemImport("b.yaml");
            YdnSetName("typeIDs.yaml", "en");

            //getFile("typeIDs.yaml", 1);
            //Console.WriteLine("Done, naming items");

            string line = Console.ReadLine();
            int look = 1, oldLook = look;
            if (line == "") { look = 0; }
            else if (line != "test") { look = Int32.Parse(line); }

            while (look != 0 || line == "test")
            {
                bool found = false;
                if (look != oldLook)
                {
                    for (int i = 0; i <= items.Length - 3 && found == false; ++i)
                    {
                        if (items[i].getTypeID() == look)
                        {
                            //output data about the data
                            Console.WriteLine("Name: " + items[i].getName());
                            Console.WriteLine("production Qty: " + items[i].getProdQty());
                            found = true;
                            look = oldLook;
                        }
                    }

                    //tell user if the item exists or not
                    if (found == false)
                    {
                        Console.WriteLine("Item not found");
                    }
                }
                else
                {
                    if (line == "test")
                    {
                        Console.WriteLine("TESTING BEGINS");
                        testAll();
                        Console.WriteLine("TESTING COMPLETE");
                        line = string.Empty;
                    }
                }

                //get next item
                line = Console.ReadLine();
                if (line == "") { look = 0; }
                else { try { look = Int32.Parse(line); } catch (Exception) { } }
            }
        }

        void getFile(string filename, int fileType)
        {
            string[] data;
            if (fileType == 0)
            {
                data = new string[120];
            }
            else
            {
                data = new string[850];
            }
            string line;
            bool start = false;
            System.IO.StreamReader file;

            try
            {
                //load file line by line
                file = new System.IO.StreamReader(filename);
            }
            catch (Exception)
            {
                Console.WriteLine("Error Occurred While opening file, quitting operation");
                return;
            }

            int count = 0;
            //bool done = false;
            //while (done == false)
            //{
            while ((line = file.ReadLine()) != null)
            {
                //line = file.ReadLine();

                //start reading

                if (WorkOutDepth(line, fileType) == 0)
                {
                    //element is the first
                    if (start == false)
                    {
                        //start is false so this must be the first element
                        start = true;
                        data[count] = line;
                        count += 1;
                    }
                    else
                    {
                        //reached end of item (back to depth 0)


                        if (fileType == 0)
                        {
                            //create item to represent the new object
                            createItem(data);
                        }
                        else if (fileType == 1)
                        {
                            //get name of an object
                            nameItem(data);
                        }

                        //restart the process
                        count = 1;
                        for (int i = 0; i < data.Length; ++i)
                        {
                            //reset data array values fore readability
                            data[i] = string.Empty;
                        }

                        data[0] = line;
                    }
                }
                else if (WorkOutDepth(line, fileType) >= 0)
                {
                    data[count] = line;
                    count += 1;
                }


                //if (line == "" || line == null)
                //{
                //    done = true;
                //}
            }
            file.Close();
        }

        int WorkOutDepth(string searchLine, int mode)
        {
            string tab = "    ";
            int depth = 0;

            if (searchLine == "" || searchLine == "'")
            {
                //the line is empty so to avoid errors say the depth is 1
                return 1;
            }
            //else if (searchLine == null && mode == 0)
            //{
            //    return 0;
            //}


            while (searchLine.StartsWith(tab) == true)
            {
                //keep looking for tab until it fails, then return depth
                tab = tab + tab;
                depth += 1;
            }

            return depth;
        }

        int setupBasics(string fileLocation)
        {
            System.IO.StreamReader file;
            string line;
            int itemAmount = 0;

            //attempt to load the file
            try
            {
                //load file line by line
                file = new System.IO.StreamReader(fileLocation);
            }
            catch (Exception)
            {
                Console.WriteLine("Error Occurred, quitting operation");
                return -1;
            }

            while ((line = file.ReadLine()) != null)
            {
                if (WorkOutDepth(line, 1) == 0)
                {
                    //depth is 0 so it must be a new item +1 to total
                    ++itemAmount;
                }
            }
            file.Close();

            return itemAmount;
        }

        void createItem(string[] lines)
        {
            //start by disecting the lines
            int blueprintID = Int32.Parse(lines[0].Remove(lines[0].Length - 1)),
                copyTime = -1,
                METime = -1,
                TETime = -1,
                prodLmt = -1,
                itemID = 0,
                productionQty = 1,
                productionTime = 0,
                scanLine = 0,
                invID = 0,
                invQty = 1,
                invTime = 0;
            int[,] prodMats = new int[20, 2],
                invMats = new int[6, 2],
                copyMats = new int[5, 2],
                prodskills = new int[8, 2],
                MEskills = new int[8, 2],
                TEskills = new int[8, 2],
                copyskills = new int[5, 2];
            float invProb = 0f;

            try
            {
                //figure out copy time
                if (lines[2].IndexOf("copying") > 0)
                {
                    //determin if copy is advanced
                    if (lines[3].IndexOf("materials") > 0)
                    {
                        //advanced copy found
                        bool found = false;
                        if (lines[3].IndexOf("materials: []") > 0)
                        {
                            //empty array found.....
                            bool found2 = false;
                            for (int i = 3; found2 == false; ++i)
                            {
                                if (lines[i].IndexOf("time") > 0)
                                {
                                    productionTime = ExtractNumbValue(lines[i], 18);
                                    scanLine = i + 1;
                                    found2 = true;
                                }
                            }
                            found = true;
                        }

                        for (int i = 4; found == false; i += 2)
                        {
                            copyMats[(i / 2) - 2, 0] = ExtractNumbValue(lines[i], 26); //quantity
                            copyMats[(i / 2) - 2, 1] = ExtractNumbValue(lines[i + 1], 24); //type

                            //find out if the edge of the materials has been reached
                            if (lines[i + 2].IndexOf("skills") > 0)
                            {
                                found = true;
                                bool found2 = false;
                                for (int l = i + 3; found2 == false; l += 2)
                                {
                                    prodskills[(l / 2) - 3, 0] = ExtractNumbValue(lines[l], 23); //quantity
                                    prodskills[(l / 2) - 3, 1] = ExtractNumbValue(lines[l + 1], 24); //type

                                    if (lines[l + 2].IndexOf("time") > 0)
                                    {
                                        //copy time has been found
                                        productionTime = ExtractNumbValue(lines[l + 2], 18);
                                        found2 = true;
                                        scanLine = l + 2;
                                    }
                                }
                            }
                            else if (lines[i + 2].IndexOf("time") > 0)
                            {
                                //no skills found so there must be a time value here
                                productionTime = ExtractNumbValue(lines[i + 2], 18);
                                found = true;
                                scanLine = i + 2;
                            }
                            else if (lines[i + 2].IndexOf("products") > 0)
                            {
                                found = true;
                                if (lines[i + 3].IndexOf("skills") > 0)
                                {
                                    bool found2 = false;
                                    for (int l = i + 4; found2 == false; l += 2)
                                    {
                                        prodskills[(l / 2) - 4, 0] = ExtractNumbValue(lines[l], 23); //quantity
                                        prodskills[(l / 2) - 4, 1] = ExtractNumbValue(lines[l + 1], 24); //type

                                        if (lines[l + 2].IndexOf("time") > 0)
                                        {
                                            //copy time has been found
                                            productionTime = ExtractNumbValue(lines[l + 2], 18);
                                            found2 = true;
                                            scanLine = l + 3;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (lines[3].IndexOf("skills") > 0)
                    {
                        bool found = false;
                        for (int l = 4; found == false; l += 2)
                        {
                            prodskills[(l / 2) - 2, 0] = ExtractNumbValue(lines[l + 1], 23); //quantity
                            prodskills[(l / 2) - 2, 1] = ExtractNumbValue(lines[l + 1], 24); //type

                            if (lines[l + 2].IndexOf("time") > 0)
                            {
                                //copy time has been found
                                productionTime = ExtractNumbValue(lines[l + 2], 18);
                                found = true;
                                scanLine = l + 2;
                            }
                        }
                    }
                    else if (lines[3].Length >= 19)
                    {
                        copyTime = ExtractNumbValue(lines[3], 18);
                        scanLine = 4;
                    }
                }

                //invention?
                if (lines[scanLine] == "        invention:")
                {

                    //TODO add invention recognition

                    int temp = scanLine;
                    for (int i = scanLine; lines[i].IndexOf("manufacturing") < 0; ++i)
                    {
                        ++temp;
                    }
                    scanLine = temp;
                }

                //figure out the materials and itemID
                if (lines[scanLine].IndexOf("manufacturing") > 0)
                {
                    bool found = false;
                    for (int i = scanLine + 2; found == false; i += 2)
                    {
                        prodMats[(i / 2) - (scanLine / 2 + 1), 0] = ExtractNumbValue(lines[i], 26); //quantity
                        prodMats[(i / 2) - (scanLine / 2 + 1), 1] = ExtractNumbValue(lines[i + 1], 24); //type

                        //find out if the edge of the materials has been reached
                        if (lines[i + 2].IndexOf("products") > 0)
                        {
                            found = true;
                            productionQty = ExtractNumbValue(lines[i + 3], 26);
                            itemID = ExtractNumbValue(lines[i + 4], 24);

                            //find skills
                            if (lines[i + 5].IndexOf("skills") > 0)
                            {
                                bool found2 = false;
                                for (int l = i + 6; found2 == false; l += 2)
                                {
                                    prodskills[(l / 2) - (i / 2), 0] = ExtractNumbValue(lines[l], 23); //quantity
                                    prodskills[(l / 2) - (i / 2), 1] = ExtractNumbValue(lines[l + 1], 24); //type

                                    if (lines[l + 2].IndexOf("time") > 0)
                                    {
                                        //production time has been found
                                        productionTime = ExtractNumbValue(lines[l + 2], 19);
                                        found2 = true;
                                        scanLine = l + 2;
                                    }
                                }
                            }
                            else
                            {
                                //no skills found so there must be a time value here
                                productionTime = ExtractNumbValue(lines[i + 5], 18);
                                scanLine = i + 5;
                            }
                        }
                        else if (lines[i + 2].IndexOf("skills") > 0)
                        {
                            found = true;
                            bool found2 = false;
                            for (int l = i + 3; found2 == false; l += 2)
                            {
                                prodskills[(l / 2) - 4, 0] = ExtractNumbValue(lines[l], 23); //quantity
                                prodskills[(l / 2) - 3, 1] = ExtractNumbValue(lines[l + 1], 24); //type

                                if (lines[l + 2].IndexOf("time") > 0)
                                {
                                    //production time has been found
                                    productionTime = ExtractNumbValue(lines[l + 2], 18);
                                    found2 = true;
                                    scanLine = l + 2;
                                }
                            }
                        }
                    }
                }

                //TODO add research ME and TE

                //find production limit
                bool gotIt = false;
                for (int i = scanLine; gotIt == false; ++i)
                {
                    if (lines[i].IndexOf("maxProductionLimit") > 0)
                    {
                        prodLmt = ExtractNumbValue(lines[i], 24);
                        gotIt = true;
                    }
                }

                //pass data to items
                items[itemCount] = new Item(blueprintID, itemID);
                items[itemCount].setProdskills(prodskills);
                items[itemCount].setProdMats(prodMats);
                items[itemCount].setProdTime(productionTime);
                items[itemCount].setProdQty(productionQty);
                items[itemCount].setProdLimit(prodLmt);
                items[itemCount].setCopyTime(copyTime);
                items[itemCount].setCopySkills(copyskills);
                items[itemCount].setCopyMats(copyMats);

                itemCount += 1;
            }
            catch (Exception)
            {
                Console.WriteLine("Skipped item: " + blueprintID + " due to an error");
            }
        }

        void nameItem(string[] lines)
        {
            int extractID = Int32.Parse(lines[0].Remove(lines[0].Length - 1)),
                itemID = 0,
                maxID = items.Length - 3;
            bool idFound = false;

            //locate the item which relates to the current data
            for (int i = 0; idFound == false; ++i)
            {
                if (extractID == items[i].getTypeID())
                {
                    idFound = true;
                    itemID = i;
                }
                else if (i >= maxID)
                {
                    return;
                }
            }

            if (idFound == false)
            {
                return;
            }

            string name = "";
            bool nameExtracted = false;
            for (int i = 1; i <= lines.Length && nameExtracted == false; ++i)
            {
                if (lines[i] == "    name:")
                {
                    for (int l = i + 1; l <= i + 6 && nameExtracted == false; ++l)
                    {
                        if (lines[l].IndexOf("en") > 0)
                        {
                            name = ExtractStringValue(lines[l], 12);
                            nameExtracted = true;
                        }
                    }
                }
            }

            //add a name to the item
            items[itemID].setName(name);
        }

        int ExtractNumbValue(string data, int distance)
        {
            if (data.IndexOf("[]") > 0)
            {
                //array is empty, return a 0
                return 0;
            }
            else
            {
                return Int32.Parse(data.Remove(0, distance));
            }
        }

        string ExtractStringValue(string data, int distance)
        {
            if (data.Length > distance)
            {
                return data.Remove(0, distance);
            }
            return "";
        }

        void testAll()
        {
            for (int i = 0; i <= items.Length - 3; ++i)
            {
                if (items[i].getName() == "")
                {
                    Console.WriteLine("item " + i + " has no name");
                }

                if (items[i].getProdQty() == 0)
                {
                    Console.WriteLine("item " + i + " has no production quantity");
                }
            }
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

        void YdnItemImport(string fileLocation)
        {
            //get input file
            StreamReader file = new StreamReader(fileLocation);
            YamlStream yaml = new YamlStream();
            yaml.Load(file);

            var map = (YamlMappingNode)yaml.Documents[0].RootNode;
            foreach (var entry in map.Children)
            {
                //create all the values that an item needs
                int blueprintID = 0,
                    copyTime = -1,
                    METime = -1,
                    TETime = -1,
                    prodLmt = -1,
                    itemID = 0,
                    productionQty = 1,
                    productionTime = 0,
                    invID = 0,
                    invQty = 1,
                    invTime = 0;
                int[,] prodMats = new int[20, 2],
                    invMats = new int[6, 2],
                    copyMats = new int[5, 2],
                    prodskills = new int[8, 2],
                    MEskills = new int[8, 2],
                    TEskills = new int[8, 2],
                    copyskills = new int[5, 2];
                float invProb = 0f;

                blueprintID = Int32.Parse(((YamlScalarNode)entry.Key).Value);
                var currentEntry = (YamlMappingNode)map.Children[entry.Key];

                foreach (var item in currentEntry)
                {
                    if ((item.Key).ToString() == "activities")
                    {
                        YamlMappingNode activities = (YamlMappingNode)currentEntry.Children[(new YamlScalarNode("activities"))];

                        //skip current item if it is empty
                        if (activities == null)
                        {
                            continue;
                        }

                        //start grabbing data from the item
                        YamlMappingNode currentActivity;
                        foreach (var activity in activities)
                        {
                            //var currentActivity = activity.Value as YamlMappingNode;
                            currentActivity = (YamlMappingNode)activities.Children[(new YamlScalarNode((activity.Key).ToString()))];
                            if ((activity.Key).ToString() == "copying")
                            {
                                if (currentActivity.Children.Count > 1)
                                {
                                    YamlMappingNode subSearch;
                                    foreach (var search in currentActivity)
                                    {
                                        subSearch = (YamlMappingNode)currentActivity.Children[(new YamlScalarNode((search.Key).ToString()))];
                                        //subSearch2 = search;
                                        //get copy data
                                        if ((search.Key).ToString() == mats)
                                        {
                                            //contains mats... extract them
                                            YdnExtract(subSearch, mats, ref copyMats);
                                        }
                                        else if ((search.Key).ToString() == prod)
                                        {
                                            //contains products... extract them
                                            Console.WriteLine("production does have a line");
                                        }
                                        else if ((search.Key).ToString() == skill)
                                        {
                                            //contains skills... extract them
                                            YdnExtract(subSearch, skill, ref copyskills);
                                        }
                                        else if ((search.Key).ToString() == skill)
                                        {
                                            //contains time... extract it
                                            YdnExtract(subSearch, time, ref copyTime);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var search in currentActivity)
                                    {
                                        if ((search.Key).ToString() == mats)
                                        {
                                            //contains mats... extract them
                                            YdnExtract(currentActivity, mats, ref copyMats);
                                        }
                                        else if ((search.Key).ToString() == prod)
                                        {
                                            //contains products... extract them
                                            Console.WriteLine("production does have a line");
                                        }
                                        else if ((search.Key).ToString() == skill)
                                        {
                                            //contains skills... extract them
                                            YdnExtract(currentActivity, skill, ref copyskills);
                                        }
                                        else if ((search.Key).ToString() == time)
                                        {
                                            //contains time... extract it
                                            YdnExtract(currentActivity, time, ref copyTime);
                                        }
                                    }
                                }
                            }
                            else if ((activity.Key).ToString() == "manufacturing")
                            {
                                //YamlMappingNode subSearch;
                                foreach (var search in currentActivity)
                                {

                                    //get copy data
                                    if ((search.Key).ToString() == mats)
                                    {

                                        Console.WriteLine("item, key: " + search.Key + ", value: " + search.Value);

                                        //contains mats... extract them
                                        YamlScalarNode searchTerm = new YamlScalarNode("materials");
                                        YamlMappingNode subSearch = (YamlMappingNode)currentActivity.Children[searchTerm];
                                        YdnExtract(subSearch, mats, ref prodMats);
                                    }
                                    else if ((search.Key).ToString() == prod)
                                    {
                                        //contains products... extract them
                                        var subSearch = (YamlMappingNode)currentActivity.Children[(new YamlScalarNode((search.Key).ToString()))];
                                        YdnExtract(subSearch, prod, ref productionQty, ref itemID);
                                    }
                                    else if ((search.Key).ToString() == skill)
                                    {
                                        //contains skills... extract them
                                        var subSearch = (YamlMappingNode)currentActivity.Children[(new YamlScalarNode((search.Key).ToString()))];
                                        YdnExtract(subSearch, skill, ref prodskills);
                                    }
                                    else if ((search.Key).ToString() == time)
                                    {
                                        //contains time... extract it

                                        //YdnExtract(search, time, ref productionTime);
                                    }
                                }

                                //YamlMappingNode subSearch;
                                //for (int i = 0; i <= Int32.Parse((currentActivity.Children).ToString()); ++i )
                                //{
                                //    YamlMappingNode test = (YamlMappingNode)currentActivity.Children[i] as YamlMappingNode;
                                //    //get copy data
                                //    //if (( .Key).ToString() == mats)
                                //    if (false)
                                //    {
                                //        //contains mats... extract them
                                //        //(search.Value).ToString();
                                //        subSearch = (YamlMappingNode)currentActivity.Children[(new YamlScalarNode((search).ToString()))];
                                //        YdnExtract(subSearch, mats, ref prodMats);
                                //    }
                                //    //else if ((search.Key).ToString() == prod)
                                //    else if (false)
                                //    {
                                //        //contains products... extract them
                                //        subSearch = (YamlMappingNode)currentActivity.Children[(new YamlScalarNode((search.Key).ToString()))];
                                //        YdnExtract(subSearch, prod, ref productionQty, ref itemID);
                                //    }
                                //    //else if ((search.Key).ToString() == skill)
                                //    else if (false)
                                //    {
                                //        //contains skills... extract them
                                //        subSearch = (YamlMappingNode)currentActivity.Children[(new YamlScalarNode((search.Key).ToString()))];
                                //        YdnExtract(subSearch, skill, ref prodskills);
                                //    }
                                //    //else if ((search.Key).ToString() == time)
                                //    else if (false)
                                //    {
                                //        //contains time... extract it

                                //        //YdnExtract(search, time, ref productionTime);
                                //    }
                                //}
                            }
                        }
                    }
                    else if ((item.Key).ToString() == "maxProductionLimit")
                    {
                        prodLmt = Int32.Parse((item.Value).ToString());
                    }
                }

                //pass in all the values that have been collected
                items[itemCount] = new Item(blueprintID, itemID);
                items[itemCount].setProdskills(prodskills);
                items[itemCount].setProdMats(prodMats);
                items[itemCount].setProdTime(productionTime);
                items[itemCount].setProdQty(productionQty);
                items[itemCount].setProdLimit(prodLmt);
                items[itemCount].setCopyTime(copyTime);
                items[itemCount].setCopySkills(copyskills);
                items[itemCount].setCopyMats(copyMats);
                ++itemCount;
            }
            itemCount = 0;
            file.Close();
        }

        void YdnSetName(string fileLocation, string language)
        {
            StreamReader file = new StreamReader(fileLocation);
            YamlStream yaml = new YamlStream();
            yaml.Load(file);

            int itemID, itemPosition = 0;
            bool found = true;
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

        void YdnExtract(YamlMappingNode currentActivity, string toSearch, ref int[,] output)
        {
            YamlMappingNode data = currentActivity;
            int count = 0;
            foreach (var value in data.Children)
            {
                if ((value.Key).ToString() == "quantity")
                {
                    output[count, 0] = Int32.Parse((value.Value).ToString());
                }
                else if ((value.Key).ToString() == "typeID")
                {
                    output[count, 1] = Int32.Parse((value.Value).ToString());
                    ++count;
                }
            }
        }

        void YdnExtract(YamlMappingNode currentActivity, string toSearch, ref int qty, ref int type)
        {
            int count = 0;
            foreach (var value in currentActivity)
            {
                if ((value.Key).ToString() == "quantity")
                {
                    qty = Int32.Parse((value.Value).ToString());
                }
                else if ((value.Key).ToString() == "typeID")
                {
                    type = Int32.Parse((value.Value).ToString());
                    ++count;
                }
            }
        }

        void YdnExtract(YamlMappingNode currentActivity, string toSearch, ref int time)
        {
            foreach (var value in currentActivity)
            {
                if ((value.Key).ToString() == toSearch)
                {
                    time = Int32.Parse((value.Value).ToString());
                }
            }

            //time = Int32.Parse((currentActivity.Value).ToString());
        }

        void StringExtract(string scanText)
        {

        }
    }
}
