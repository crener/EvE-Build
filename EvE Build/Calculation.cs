using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Windows.Forms;
using System.IO;

namespace EvE_Build
{
    class Calculation
    {
        public Item[] items;

        Material[] prodMats;
        int[] skills;
        string[] skillNames;

        Int64[,] shoppingList;
        public List<string> listItems;

        WebInterface eveCentral = new WebInterface();

        //application settings
        public bool updateOnStartup = false;
        public string[] stationNames = new string[5];
        public int[] stationIds = new int[5];
        public int updateInterval = 5;

        //form interaction
        ListBox itemSelectAll = null;
        Label BpoCost,
            RunsToPay,
            MaterialVolume;
        TrackBar MESlider = null,
            TESlider = null;
        ToolStripProgressBar ToolProgress;
        ToolStripLabel ToolProgLbl,
            ToolError;
        CheckBox BaseMaterials,
            sellorBuyCheck;
        DataGridView ManufacturingTable,
            ProfitView,
            ShoppingCart;
        NumericUpDown RunSelect;
        Thread littleMinion,
            eveCentralBot;
        TreeView GroupView;

        public Calculation() { }

        public Calculation(ListBox itemSelectAll,
        Label BpoCost,
        Label RunsToPay,
        Label MaterialVolume,
        TrackBar MESlider,
        TrackBar TESlider,
        ToolStripProgressBar ToolProgress,
        ToolStripLabel ToolProgLbl,
        ToolStripLabel ToolError,
        CheckBox BaseMaterials,
        CheckBox sellorBuyCheck,
        DataGridView ManufacturingTable,
        DataGridView ProfitView,
        DataGridView ShoppingCart,
        NumericUpDown RunSelect,
        Thread littleMinion,
        TreeView group)
        {
            this.itemSelectAll = itemSelectAll;
            this.BpoCost = BpoCost;
            this.RunsToPay = RunsToPay;
            this.MaterialVolume = MaterialVolume;
            this.MESlider = MESlider;
            this.TESlider = TESlider;
            this.ToolProgress = ToolProgress;
            this.ToolProgLbl = ToolProgLbl;
            this.ToolError = ToolError;
            this.BaseMaterials = BaseMaterials;
            this.sellorBuyCheck = sellorBuyCheck;
            this.ManufacturingTable = ManufacturingTable;
            this.ProfitView = ProfitView;
            this.ShoppingCart = ShoppingCart;
            this.RunSelect = RunSelect;
            this.littleMinion = littleMinion;
            GroupView = group;
            Settings();
        }

        public bool checkAlphanumeric(string text)
        {
            if (text == null)
            {
                return false;
            }
            Regex remove = new Regex("[^A-Za-z0-9 ]+");
            string compair = remove.Replace(text, "");
            if (text == compair)
            {
                return true;
            }
            return false;
        }

        public void littleMinionStart(Thread thread)
        {
            littleMinion = thread;
        }

        public void CloseThreads()
        {
            if (eveCentralBot != null && eveCentralBot.IsAlive)
            {
                eveCentralBot.Abort();
            }
            if (littleMinion != null && littleMinion.IsAlive)
            {
                littleMinion.Abort();
            }
        }

        public void populateItemList()
        {
            
            try
            {
                //create and populate items for the itemselector
                YAML importer = new YAML();
                items = importer.ImportData();
                
                List<string> data = new List<string>();
                string[] tmpStorage = new string[items.Length];
                int i = 0;
                foreach (var item in items)
                {
                    if (item.getName() != "" && checkAlphanumeric(item.getName()))
                    {
                        tmpStorage[i] = item.getName();
                        ++i;
                    }
                }

                //remove null and sort
                Array.Sort(tmpStorage);
                for (int j = 0; tmpStorage.Length > j; ++j)
                {
                    if (tmpStorage[j] != null)
                    {
                        data.Add(tmpStorage[j]);
                    }
                }
                listItems = data;

                prodMats = importer.YdnMatTypeMat(items);
                importer.extractMaterialNames(ref prodMats, "StaticData/typeIDs.yaml", "en");

                skills = importer.YdnGetAllSkills(items);
                skillNames = importer.YdnNameFromID("StaticData/typeIDs.yaml", skills, "en");

                if (itemSelectAll.InvokeRequired)
                {
                    itemSelectAll.Invoke(new MethodInvoker(delegate
                    {
                        itemSelectAll.DataSource = data;
                    }));
                }
                else
                {
                    itemSelectAll.DataSource = data;
                }
            }
            catch (ThreadAbortException)
            {
                littleMinion.Abort();
            }

            //Start eve central thread for grabbing eve data periodialy
            eveCentralBot = new Thread(EveThread);
            eveCentralBot.Name = "eveCentralBot";
            eveCentralBot.Start();
        }

        void EveThread()
        {
            WebInterface eveBotInterface = new WebInterface();
            int[] tempID = new int[prodMats.Length - 1];
            for (int i = 0; i < prodMats.Length - 1; ++i)
            {
                tempID[i] = prodMats[i].ID;
            }

            try
            {
                int stationCount = 0;
                float progress = 0.0f;
                foreach (var station in stationIds)
                {
                    if (station != 0)
                    {
                        ++stationCount;
                    }
                }

                //sleep for the refresh time if the user doesn't want to load data on startup
                if (updateOnStartup == false)
                {
                    if (updateInterval > 1)
                    {
                        ToolProgLbl.Text = "Update on start up disabled, waiting " + updateInterval + " min";
                    }
                    else
                    {
                        ToolProgLbl.Text = "Update on start up disabled, waiting " + updateInterval + " mins";
                    }
                    ToolError.Text = "";
                    Thread.Sleep(updateInterval * 60000);
                }

                while (true)
                {
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt")
                        + " Update cycle started");

                    ToolError.Text = "";
                    ToolProgLbl.Text = "Updating Material Data";
                    setProgress((int)progress);
                    int loadChuck = 50;
                    int division = (items.Length - 1) / loadChuck;

                    //update station data
                    for (int l = 0; l < 5 && stationIds[l] != 0; ++l)
                    {
                        int upto = 0;
                        int[] search = new int[50];
                        string data = "";

                        if (l != 0)
                        {
                            progress = (l * 100.0f) / stationCount;
                        }
                        else
                        {
                            progress = 0;
                        }
                        setProgress((int)progress);

                        while (upto != prodMats.Length - 1)
                        {
                            progress += (100.0f / stationCount) / division;
                            setProgress((int)progress);

                            for (int i = 0; i <= 49 && upto != prodMats.Length - 1; ++i)
                            {
                                search[i] = prodMats[upto].ID;
                                ++upto;
                            }

                            if (data != "")
                            {
                                string temp = "";
                                try
                                {
                                    temp = eveBotInterface.getWebData(stationIds[l], search);
                                }
                                catch (WebException)
                                {
                                    ToolError.Text = "WebError, some items will not have updated information";
                                }

                                if (temp != "")
                                {
                                    data = data.Remove(data.Length - 29);
                                    temp = temp.Remove(0, 106);
                                    data = string.Concat(data, temp);
                                }
                            }
                            else
                            {
                                try
                                {
                                    data = eveBotInterface.getWebData(stationIds[l], search);
                                }
                                catch (WebException)
                                {
                                    ToolError.Text = "WebError, some items will not have updated information";
                                }
                            }

                            search = new int[50];
                        }

                        //check that there is data returned
                        Int64[,] dataCheck = eveBotInterface.extractPrice(data, tempID);
                        Random ran = new Random();
                        int strike = 0,
                            first,
                            second;
                        for (int i = 0; i <= 5; ++i)
                        {
                            first = ran.Next((dataCheck.Length / 2) - 1);
                            second = ran.Next(2);
                            if (dataCheck[first, second] == 0)
                            {
                                ++strike;
                            }
                        }

                        //there are too many 0 values so don't save the imported data for calulation ussage
                        if (strike < 3)
                        {
                            //place data into the correct array
                            for (int i = 0; i < upto; ++i)
                            {
                                //item layer
                                for (int m = 0; m < 2; ++m)
                                {
                                    //prodMatPrices[l, i, m] = dataCheck[i, m];
                                    prodMats[i].price[l, m] = dataCheck[i, m];
                                }
                            }
                        }
                    }

                    //update item data
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt")
                        + " Starting Item Update");
                    ToolProgLbl.Text = "Updating Item Data";
                    progress = 0;
                    setProgress((int)progress);

                    for (int l = 0; l < 5 && stationIds[l] != 0; ++l)
                    {
                        int upto = 0;
                        int[] search = new int[50];
                        string data = "";
                        loadChuck = 50;

                        if (l != 0)
                        {
                            progress = (l * 100.0f) / stationCount;
                        }
                        else
                        {
                            progress = 0;
                        }
                        setProgress((int)progress);
                        division = (items.Length - 1) / loadChuck;

                        while (upto != items.Length - 1)
                        {
                            progress += (100.0f / stationCount) / division;
                            setProgress((int)progress);

                            for (int i = 0; i <= loadChuck - 1 && upto != items.Length - 1; ++i)
                            {
                                search[i] = items[upto].getTypeID();
                                ++upto;
                            }

                            if (data != "")
                            {
                                string temp = "";
                                try
                                {
                                    temp = eveBotInterface.getWebData(stationIds[l], search);
                                }
                                catch (WebException)
                                {
                                    ToolError.Text = "WebError, some items will not have updated information";
                                }


                                if (temp != "")
                                {
                                    data = data.Remove(data.Length - 29);
                                    temp = temp.Remove(0, 106);
                                    data = string.Concat(data, temp);
                                }
                            }
                            else
                            {
                                try
                                {
                                    data = eveBotInterface.getWebData(stationIds[l], search);
                                }
                                catch (WebException)
                                {
                                    ToolError.Text = "WebError, some items will not have updated information";
                                }
                            }


                            search = new int[loadChuck];
                        }
                        //check that there is data returned

                        int[] itemIDCollection = new int[items.Length - 1];
                        for (int i = 0; i < itemIDCollection.Length; ++i)
                        {
                            itemIDCollection[i] = items[i].getTypeID();
                        }

                        Int64[,] dataCheck = eveBotInterface.extractPrice(data, itemIDCollection);
                        Random ran = new Random();
                        int strike = 0,
                            first,
                            second;
                        for (int i = 0; i < 20; ++i)
                        {
                            first = ran.Next((dataCheck.Length / 2) - 1);
                            second = ran.Next(2);
                            if (dataCheck[first, second] == 0)
                            {
                                ++strike;
                            }
                        }

                        //there are too many 0 values so don't save the imported data for calulation ussage
                        if (strike < 20)
                        {
                            //place data into the correct array
                            for (int i = 0; i < upto; ++i)
                            {
                                //item layer
                                for (int m = 0; m < 2; ++m)
                                {
                                    if (m == 0)
                                    {
                                        items[i].setBuyPrice(l, dataCheck[i, m]);
                                    }
                                    else if (m == 1)
                                    {
                                        items[i].setSellPrice(l, dataCheck[i, m]);
                                    }
                                }
                            }
                        }


                    }
                    ToolProgLbl.Text = "Data Updated";
                    setProgress(0);
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt")
                        + " Update cycle completed");
                    Thread.Sleep(updateInterval * 60000);
                }
            }
            catch (ThreadAbortException)
            {
                eveCentralBot.Abort();
                // Thead was aborted
            }
        }

        private void setProgress(int value)
        {
            if (ToolProgress.GetCurrentParent().InvokeRequired)
            {
                ToolProgress.GetCurrentParent().Invoke(new MethodInvoker(delegate
                {
                    ToolProgress.Value = value;
                }));
            }
        }

        public void WorkOutData(int itemIndex)
        {
            if (items == null)
            {
                return;
            }

            //work out the material costs
            ItemProporties(itemIndex);

            //fill in runs needed until BPO pays for itself
            Int64 bestPrice = 0;
            int station = 0;
            Item item = items[itemIndex];
            for (int i = 0; i < 5; ++i)
            {
                if (stationIds[i] != 0)
                {
                    Int64 itemCost = getItemValue(items[itemIndex],
                        MESlider.Value, i);

                    itemCost = item.getSellPrice(i) - (itemCost / item.getProdQty());
                    if (itemCost > bestPrice || itemCost > bestPrice && bestPrice == 0)
                    {
                        bestPrice = itemCost;
                        station = i;
                    }
                }
            }
            //get the amount of runs required to pay for the BPO
            int runs = 0;
            if (bestPrice != 0)
            {
                runs = CorrectRounding((item.getBlueprintPrice() / bestPrice) + 0.5f);
            }

            BpoCost.Text = format(item.getBlueprintPrice().ToString()) + " isk";

            if (runs <= 0)
            {
                RunsToPay.Text = "not profitable";
            }
            else
            {
                RunsToPay.Text = runs + " runs in " + stationNames[station];
            }

            Profit(itemIndex);
        }

        private void Profit(int itemIndex)
        {
            //create a datatable and setup
            DataTable table = new DataTable();
            table.Columns.Add("Station", typeof(string));
            table.Columns.Add("Build Cost", typeof(string));
            table.Columns.Add("Item Cost", typeof(string));
            table.Columns.Add("Sell Proft?", typeof(string));
            table.Columns.Add("Buy Proft?", typeof(string));
            table.Columns.Add("Isk per Hour", typeof(string));
            table.Columns.Add("Investment/Profit", typeof(float));

            Item current = items[itemIndex];

            bool second = false;
            int quantity = 0;
            Int64[] stationPrice = new Int64[5];
            foreach (int value in current.getProdMats())
            {
                if (value == 0)
                {
                    continue;
                }

                if (second == false)
                {
                    float ME = (1 - (0.01f * MESlider.Value));

                    quantity = CorrectRounding(value * ME);
                    second = true;
                }
                else if (second == true)
                {
                    //populate the next row
                    //item in productMats
                    string name = "";
                    bool found2 = false;
                    for (int i = 0; i < prodMats.Length - 1 && found2 == false; ++i)
                    {
                        if (prodMats[i].ID == value)
                        {
                            name = prodMats[i].name;
                            for (int k = 0; k < 5; ++k)
                            {
                                stationPrice[k] += prodMats[i].price[k, 1] * quantity;
                            }
                            found2 = true;
                            break;
                        }
                    }
                    if (found2 == false)
                    {
                        //item is in items
                        for (int i = 0; i < items.Length - 1 && found2 == false; ++i)
                        {
                            if (items[i].getTypeID() == value)
                            {
                                for (int k = 0; k < 5; ++k)
                                {
                                    if (BaseMaterials.Checked == false)
                                    {
                                        stationPrice[k] += getItemValue(items[i], 10, k) * quantity;
                                    }
                                    else
                                    {
                                        stationPrice[k] += items[i].getSellPrice(k) * quantity;
                                    }
                                }
                            }
                        }
                    }
                    second = false;
                }
            }
            //populate the table
            string[] stationBuild = new string[5];
            Int64 subPrice = 0,
                subPrice2 = 0;
            for (int i = 0; i < 5 && stationPrice[i] != 0; ++i)
            {
                subPrice = stationPrice[i] / current.getProdQty();
                subPrice2 = subPrice * Convert.ToInt64(RunSelect.Value);
                stationBuild[i] = format(subPrice2.ToString());
            }
            for (int s = 0; s < 5; ++s)
            {
                if (stationIds[s] != 0)
                {
                    Int64 sellProfit = current.getSellPrice(s) - (stationPrice[s] / current.getProdQty()),
                        buyProfit = current.getBuyPrice(s) - (stationPrice[s] / current.getProdQty()),
                        iskHr = new Int64();
                    float buildTime = ((current.getProdTime() / 60f) / 60f) * (float)(1 - (0.01 * TESlider.Value)),
                        iskInv;

                    //profit baseed on buy or sell
                    if (sellorBuyCheck.Checked)
                    {
                        //profit based on buy
                        iskHr = (Int64)((buyProfit * current.getProdQty()) / buildTime);
                        iskInv = ((buyProfit * 1.0f) / stationPrice[s]);
                    }
                    else
                    {
                        //profit based on sell
                        iskHr = (Int64)((sellProfit * current.getProdQty()) / buildTime);
                        iskInv = ((sellProfit * 1.0f) / stationPrice[s]);
                    }


                    if (stationPrice[s] != 0)
                    {
                        table.Rows.Add(stationNames[s],
                            stationBuild[s],
                            format(current.getSellPrice(s).ToString()),
                            format((sellProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            format((buyProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            format(iskHr.ToString()),
                            iskInv);
                    }
                    else
                    {
                        //no station price data, don't bother with staion specific stuff
                        table.Rows.Add(stationNames[s],
                            stationBuild[s],
                            format(current.getSellPrice(s).ToString()),
                            format((sellProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            format((buyProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            "0",
                            0.0f);
                    }
                }
            }
            //put the data into the table so the user can see it
            ProfitView.DataSource = new DataTable();
            ProfitView.DataSource = table;
        }

        private void ItemProporties(int itemIndex)
        {
            if (items == null || prodMats == null)
            {
                return;
            }

            Item current = items[itemIndex];

            //update data
            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add(stationNames[0], typeof(string));
            table.Columns.Add(stationNames[1], typeof(string));
            table.Columns.Add(stationNames[2], typeof(string));
            table.Columns.Add(stationNames[3], typeof(string));
            table.Columns.Add(stationNames[4], typeof(string));

            bool second = false;
            int quantity = 0;
            float volume = 0f;
            foreach (int value in current.getProdMats())
            {
                if (value == 0)
                {
                    continue;
                }

                if (second == false)
                {
                    quantity = CorrectRounding(value * (1 - (0.01f * MESlider.Value))) * Convert.ToInt32(RunSelect.Value);
                    second = true;
                }
                else if (second == true)
                {
                    //populate the next row of the table
                    string name = "";
                    bool found2 = false;
                    for (int i = 0; i < prodMats.Length - 1 && found2 == false; ++i)
                    {
                        if (prodMats[i].ID == value)
                        {
                            name = prodMats[i].name;
                            table.Rows.Add(name, quantity, cost(i, 0, quantity),
                                cost(i, 1, quantity), cost(i, 2, quantity),
                                cost(i, 3, quantity), cost(i, 4, quantity));
                            found2 = true;

                            volume += prodMats[i].volume * quantity;
                        }
                    }
                    if (found2 == false)
                    {
                        //item is not normal, and must be an item that is in the item list
                        for (int i = 0; i < items.Length - 1 && found2 == false; ++i)
                        {
                            if (items[i].getTypeID() == value)
                            {
                                if (BaseMaterials.Checked == false)
                                {
                                    table.Rows.Add(items[i].getName(), quantity,
                                        format((getItemValue(items[i], 10, 0) * quantity).ToString()),
                                    format((getItemValue(items[i], 10, 1) * quantity).ToString()),
                                    format((getItemValue(items[i], 10, 2) * quantity).ToString()),
                                    format((getItemValue(items[i], 10, 3) * quantity).ToString()),
                                    format((getItemValue(items[i], 10, 4) * quantity).ToString()));

                                    volume += (items[i].getVolume() * quantity);
                                }
                                else
                                {
                                    //return the market value of the current item
                                    table.Rows.Add(items[i].getName(), quantity,
                                        format((items[i].getSellPrice(0) * quantity).ToString()),
                                    format((items[i].getSellPrice(1) * quantity).ToString()),
                                    format((items[i].getSellPrice(2) * quantity).ToString()),
                                    format((items[i].getSellPrice(3) * quantity).ToString()),
                                    format((items[i].getSellPrice(4) * quantity).ToString()));

                                    volume += (items[i].getVolume() * quantity);
                                }
                            }
                        }
                    }
                    second = false;
                }

                //material volume
                MaterialVolume.Text = volume + " m3";
            }

            //put the data into the table so the user can see it
            ManufacturingTable.DataSource = new DataTable();
            ManufacturingTable.DataSource = table;
        }

        public Int64 getItemValue(Item search, int ME, int stationIndex)
        {
            if (search == null || prodMats == null)
            {
                return 0;
            }

            Int64 cost = 0;
            for (int i = 0; i < search.getProdMats().Length / 2; ++i)
            {
                if (search.getProdMats()[i, 1] != 0)
                {
                    int id = (int)search.getProdMats()[i, 1],
                        qty = CorrectRounding((search.getProdMats()[i, 0]) * (1 - (0.01f * ME)));


                    bool found = false;
                    for (int m = 0; m < prodMats.Length - 1 && found == false; ++m)
                    {
                        if (prodMats[m].ID == id)
                        {
                            found = true;
                            cost += prodMats[m].price[stationIndex, 1] * qty;
                        }
                    }

                    if (found == false)
                    {
                        //item not found, must be an item

                        if (id == search.getTypeID())
                        {
                            return cost;
                        }

                        for (int m = 0; m < items.Length - 1 && found == false; ++m)
                        {
                            if (items[m].getTypeID() == id)
                            {
                                cost += getItemValue(items[m], ME, stationIndex) * qty;
                                found = true;
                            }
                        }
                    }
                }
            }
            return cost;
        }

        public Int64 getItemValue(int typeID, Int64 qty, int stationIndex)
        {
            if (items == null || prodMats == null || typeID == 0)
            {
                return 0;
            }
            Int64 cost = 0;

            bool found = false;
            int g = 0;
            while (found == false)
            {
                if (g < prodMats.Length - 1)
                {
                    if (prodMats[g].ID == typeID)
                    {
                        cost = prodMats[g].price[stationIndex, 1] * qty;
                        found = true;
                    }
                    ++g;
                }
                else
                {
                    cost = getItemValue(items[TypeIDtoItemIndex(typeID)], 10, stationIndex) * qty;
                    found = true;
                }
            }
            return cost;
        }

        private int CorrectRounding(float input)
        {
            int removeDigit = (int)input;
            float postDot = input - removeDigit;

            if (postDot > 0.0f)
            {
                return removeDigit + 1;
            }
            return removeDigit;
        }

        private string cost(int typeIdIndex, int stationNo, Int64 qty)
        {
            //Int64 numValue = prodMatPrices[stationNo, typeID, 1] * qty;
            Int64 numValue = prodMats[typeIdIndex].price[stationNo, 1] * qty;
            string output = (numValue).ToString();
            if (output == "0")
            {
                return "0.00";
            }
            else if (numValue < 10)
            {
                return "0.0" + numValue;
            }
            else if (numValue < 100)
            {
                return "0." + numValue;
            }

            return format(output);
        }

        private string format(string text)
        {
            bool negative = false;
            string output = text;
            if (Int64.Parse(text) < 0)
            {
                negative = true;
                output = output.Remove(0, 1);
            }

            //add indentations to make the cost look nice
            if (output == "0")
            {
                return "0.00";
            }
            else if (Int64.Parse(output) < 10)
            {
                return "0.0" + output;
            }
            else if (Int64.Parse(output) < 100)
            {
                return "0." + output;
            }


            int comma = (output.Length - 3) / 3;
            output = output.Insert(output.Length - 2, ".");
            for (int i = 1; i < comma + 1; ++i)
            {
                output = output.Insert((output.Length - 3) - (i * 3) - (i - 1), ",");
            }

            if (negative == true)
            {
                output = output.Insert(0, "-");
            }
            return output;
        }

        public int NametoItemIndex(string name)
        {
            if (items == null)
            {
                return 0;
            }

            bool found = false;
            for (int i = 0; found == false && i < items.Length; ++i)
            {
                if (items[i].getName() == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private int TypeIDtoItemIndex(int type)
        {
            if (items == null)
            {
                return 0;
            }

            bool found = false;
            for (int i = 0; found == false && i < items.Length; ++i)
            {
                if (items[i].getTypeID() == type)
                {
                    return i;
                }
            }
            return -1;
        }

        private string NametoBlueprintID(string name)
        {
            bool found = false;
            for (int i = 0; found == false && i < items.Length; ++i)
            {
                if (items[i].getName() == name)
                {
                    return items[i].getBlueprintTypeID().ToString();
                }
            }
            return "0";
        }

        public void SetupTreeView()
        {
            //populate top most view with items
            GroupView.Nodes.Add("Ammunition & Charges");
            GroupView.Nodes.Add("Drones");
            GroupView.Nodes.Add("Maufacture & Research");
            GroupView.Nodes.Add("Ship Equipment");
            GroupView.Nodes.Add("Ship Modifications");
            GroupView.Nodes.Add("Ships");
            GroupView.Nodes.Add("Structures");

            //populate ammunition and crystals
            //TODO fix this
            #region AmmunitionAndCharges
            GroupView.Nodes[0].Nodes.Add("Frequency Crystals");
            GroupView.Nodes[0].Nodes.Add("Hybrid Crystals");
            GroupView.Nodes[0].Nodes.Add("Missiles");
            GroupView.Nodes[0].Nodes.Add("Probes");
            GroupView.Nodes[0].Nodes.Add("Projectile Ammo");
            GroupView.Nodes[0].Nodes.Add("Bombs");
            GroupView.Nodes[0].Nodes.Add("Cap Booster Charges");
            GroupView.Nodes[0].Nodes.Add("Mining Crystals");
            GroupView.Nodes[0].Nodes.Add("Nanite Repair Paste");
            GroupView.Nodes[0].Nodes.Add("Scripts");
            //for (int i = 0; i < 2; ++i)
            //{
            //    GroupView.Nodes[0].Nodes[i].Nodes.Add("Extra Large");
            //    GroupView.Nodes[0].Nodes[i].Nodes.Add("Large");
            //    GroupView.Nodes[0].Nodes[i].Nodes.Add("Medium");
            //    GroupView.Nodes[0].Nodes[i].Nodes.Add("Orbital Strike");
            //    GroupView.Nodes[0].Nodes[i].Nodes.Add("Small");
            //}
            //GroupView.Nodes[0].Nodes[4].Nodes.Add("Extra Large");
            //GroupView.Nodes[0].Nodes[4].Nodes.Add("Large");
            //GroupView.Nodes[0].Nodes[4].Nodes.Add("Medium");
            //GroupView.Nodes[0].Nodes[4].Nodes.Add("Orbital Strike");
            //GroupView.Nodes[0].Nodes[4].Nodes.Add("Small");

            ////fill in the items for frequency crystals
            //GroupView.Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(503));
            //GroupView.Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(105));
            //GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(103));
            //GroupView.Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1599));
            //GroupView.Nodes[0].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(102));

            ////fill in the items for hybrid charges
            //GroupView.Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(504));
            //GroupView.Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(106));
            //GroupView.Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(108));
            //GroupView.Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1600));
            //GroupView.Nodes[0].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(107));

            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Auto-Targeting");
            //GroupView.Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(914));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Citadel Cruise Missiles");
            //GroupView.Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1287));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Citadel Torpedos");
            //GroupView.Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1193));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Cruise Missiles");
            //GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(921));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Defender");
            //GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes.AddRange(PopulateTreeCategory(116));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Heavy Assult Missiles");
            //GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes.AddRange(PopulateTreeCategory(971));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Heavy Missiles");
            //GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes.AddRange(PopulateTreeCategory(924));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Light Missiles");
            //GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes.AddRange(PopulateTreeCategory(1287));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Rockets");
            //GroupView.Nodes[0].Nodes[2].Nodes[8].Nodes.AddRange(PopulateTreeCategory(1287));
            //GroupView.Nodes[0].Nodes[2].Nodes.Add("Torpedoes");
            //GroupView.Nodes[0].Nodes[2].Nodes[9].Nodes.AddRange(PopulateTreeCategory(1287));

            //Bombs
            GroupView.Nodes[0].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1015));

            //Cap Booster Charges
            GroupView.Nodes[0].Nodes[6].Nodes.AddRange(PopulateTreeCategory(139));

            //Mining Crystals
            GroupView.Nodes[0].Nodes[7].Nodes.AddRange(PopulateTreeCategory(593));

            //Nanite Repair Paste
            GroupView.Nodes[0].Nodes[8].Nodes.AddRange(PopulateTreeCategory(1103));

            //scripts
            GroupView.Nodes[0].Nodes[9].Nodes.AddRange(PopulateTreeCategory(1094));
            #endregion

            //populate Drones
            #region Drones
            GroupView.Nodes[1].Nodes.Add("Combat Drones");
            GroupView.Nodes[1].Nodes.Add("Combat Utility Drones");
            GroupView.Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(843));
            GroupView.Nodes[1].Nodes.Add("Electronic Warefare Drones");
            GroupView.Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(841));
            GroupView.Nodes[1].Nodes.Add("Logistic Drones");
            GroupView.Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(842));
            GroupView.Nodes[1].Nodes.Add("Mining Drones");
            GroupView.Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(158));
            GroupView.Nodes[1].Nodes.Add("Salvage Drones");
            GroupView.Nodes[1].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1646));

            //fill combat utility drones
            GroupView.Nodes[1].Nodes[0].Nodes.Add("Fighter Bombers");
            GroupView.Nodes[1].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1310));
            GroupView.Nodes[1].Nodes[0].Nodes.Add("Fighters");
            GroupView.Nodes[1].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(840));
            GroupView.Nodes[1].Nodes[0].Nodes.Add("Heavy Attack Drones");
            GroupView.Nodes[1].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(839));
            GroupView.Nodes[1].Nodes[0].Nodes.Add("Light Scout Drones");
            GroupView.Nodes[1].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(837));
            GroupView.Nodes[1].Nodes[0].Nodes.Add("Medium Scout Drones");
            GroupView.Nodes[1].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(838));
            GroupView.Nodes[1].Nodes[0].Nodes.Add("Sentry Drones");
            GroupView.Nodes[1].Nodes[0].Nodes[5].Nodes.AddRange(PopulateTreeCategory(911));
            #endregion

            //populate Manufacture and research
            #region ManufactureAndResearch
            GroupView.Nodes[2].Nodes.Add("Components");
            GroupView.Nodes[2].Nodes.Add("R.Db");
            GroupView.Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1907));

            GroupView.Nodes[2].Nodes[0].Nodes.Add("Advanced Capital Ship Components");
            GroupView.Nodes[2].Nodes[0].Nodes.Add("Advanced Components");
            GroupView.Nodes[2].Nodes[0].Nodes.Add("Outpost Components");
            GroupView.Nodes[2].Nodes[0].Nodes[2].Nodes.Add("Construction Platform");
            GroupView.Nodes[2].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1864));
            GroupView.Nodes[2].Nodes[0].Nodes[2].Nodes.Add("Station Components");
            GroupView.Nodes[2].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1865));
            GroupView.Nodes[2].Nodes[0].Nodes.Add("Fuel Blocks");
            GroupView.Nodes[2].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1870));
            GroupView.Nodes[2].Nodes[0].Nodes.Add("R.A.M.");
            GroupView.Nodes[2].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1908));
            GroupView.Nodes[2].Nodes[0].Nodes.Add("Standard Capital System Components");
            GroupView.Nodes[2].Nodes[0].Nodes[5].Nodes.AddRange(PopulateTreeCategory(781));
            GroupView.Nodes[2].Nodes[0].Nodes.Add("SubSystem Components");
            GroupView.Nodes[2].Nodes[0].Nodes[6].Nodes.AddRange(PopulateTreeCategory(1147));

            for (int i = 0; i <=1; ++i)
            {
                GroupView.Nodes[2].Nodes[0].Nodes[i].Nodes.Add("Amarr");
                GroupView.Nodes[2].Nodes[0].Nodes[i].Nodes.Add("Caldari");
                GroupView.Nodes[2].Nodes[0].Nodes[i].Nodes.Add("Gallente");
                GroupView.Nodes[2].Nodes[0].Nodes[i].Nodes.Add("Minmatar");
            }

            GroupView.Nodes[2].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1884));
            GroupView.Nodes[2].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1885));
            GroupView.Nodes[2].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1886));
            GroupView.Nodes[2].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1887));

            GroupView.Nodes[2].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(802));
            GroupView.Nodes[2].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(803));
            GroupView.Nodes[2].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1888));
            GroupView.Nodes[2].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1889));

            #endregion

            //populate Ship equipment
            #region ShipEquipment
            //TODO finish this
            #endregion

            //ship modifications
            #region ShipModification
            //TODO finish this
            #endregion

            //ships
            #region Ships
            GroupView.Nodes[5].Nodes.Add("Battlecruisers");
            GroupView.Nodes[5].Nodes.Add("Battleships");
            GroupView.Nodes[5].Nodes.Add("Capital Ships");
            GroupView.Nodes[5].Nodes.Add("Cruisers");
            GroupView.Nodes[5].Nodes.Add("Destroyers");
            GroupView.Nodes[5].Nodes.Add("Frigates");
            GroupView.Nodes[5].Nodes.Add("Industrial Ships");
            GroupView.Nodes[5].Nodes.Add("Mining Barges");
            GroupView.Nodes[5].Nodes.Add("Shuttles");

            #region BC
            GroupView.Nodes[5].Nodes[0].Nodes.Add("Advanced Battlecruisers");
            GroupView.Nodes[5].Nodes[0].Nodes.Add("Faction Battlecruisers");
            GroupView.Nodes[5].Nodes[0].Nodes.Add("Standard Battlecruisers");

            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(470));
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(471));
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(472));
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(473));

            GroupView.Nodes[5].Nodes[0].Nodes[1].Nodes.Add("Navy");
            GroupView.Nodes[5].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1704));

            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes.Add("Command Ships");
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(825));
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(828));
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(831));
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(834));
            #endregion
            #region BS
            GroupView.Nodes[5].Nodes[1].Nodes.Add("Advanced Battlecruisers");
            GroupView.Nodes[5].Nodes[1].Nodes.Add("Faction Battlecruisers");
            GroupView.Nodes[5].Nodes[1].Nodes.Add("Standard Battlecruisers");

            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(79));
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(80));
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(81));
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[1].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(82));

            GroupView.Nodes[5].Nodes[1].Nodes[1].Nodes.Add("Navy");
            GroupView.Nodes[5].Nodes[1].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1379));
            GroupView.Nodes[5].Nodes[1].Nodes[1].Nodes.Add("Pirate");
            GroupView.Nodes[5].Nodes[1].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1380));

            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes.Add("Black Ops");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1076));
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1077));
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1078));
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1079));

            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes.Add("Marauders");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1081));
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1082));
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1083));
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[1].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1084));
            #endregion
            #region CapitalShips
            GroupView.Nodes[5].Nodes[2].Nodes.Add("Capital Industrial Ships");
            GroupView.Nodes[5].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1048));
            GroupView.Nodes[5].Nodes[2].Nodes.Add("Carriers");
            GroupView.Nodes[5].Nodes[2].Nodes.Add("Dreadnoughts");
            GroupView.Nodes[5].Nodes[2].Nodes.Add("Freighters");
            GroupView.Nodes[5].Nodes[2].Nodes.Add("Jump frighters");
            GroupView.Nodes[5].Nodes[2].Nodes.Add("Titans");

            //carrier
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(818));
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(819));
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes.Add("Faction Carrier");
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1392));
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(820));
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[2].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(821));

            //dread
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(762));
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(763));
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(764));
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[2].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(765));

            //frighter
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(767));
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(768));
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(769));
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(770));
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes.Add("Ore");
            GroupView.Nodes[5].Nodes[2].Nodes[3].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1950));

            //jump frighter
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1090));
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1091));
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1092));
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[2].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1093));

            //titans
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes[0].Nodes.AddRange(PopulateTreeCategory(813));
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes[1].Nodes.AddRange(PopulateTreeCategory(814));
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes[2].Nodes.AddRange(PopulateTreeCategory(815));
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[2].Nodes[5].Nodes[3].Nodes.AddRange(PopulateTreeCategory(816));
            #endregion
            #region Cruisers

            #endregion
            #region Destroyers

            #endregion
            #region Frigates

            #endregion
            #region IndustrualShips

            #endregion
            #region MiningBarges
            GroupView.Nodes[5].Nodes[7].Nodes.Add("Exhumers");
            GroupView.Nodes[5].Nodes[7].Nodes[0].Nodes.AddRange(PopulateTreeCategory(874));
            GroupView.Nodes[5].Nodes[7].Nodes.Add("Mining Barges");
            GroupView.Nodes[5].Nodes[7].Nodes[1].Nodes.AddRange(PopulateTreeCategory(494));
            #endregion
            #region Shuttles
            GroupView.Nodes[5].Nodes[8].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[8].Nodes[0].Nodes.AddRange(PopulateTreeCategory(393));
            GroupView.Nodes[5].Nodes[8].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[8].Nodes[1].Nodes.AddRange(PopulateTreeCategory(394));
            GroupView.Nodes[5].Nodes[8].Nodes.Add("Faction Shuttles");
            GroupView.Nodes[5].Nodes[8].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1631));
            GroupView.Nodes[5].Nodes[8].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[8].Nodes[3].Nodes.AddRange(PopulateTreeCategory(395));
            GroupView.Nodes[5].Nodes[8].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[8].Nodes[4].Nodes.AddRange(PopulateTreeCategory(396));
            #endregion
            #endregion

            //structures
            #region Structures
            //TODO finish this
            #endregion
        }

        TreeNode[] PopulateTreeCategory(int marketID)
        {
            if (items == null)
            {
                TreeNode[] fail = new TreeNode[1];
                fail[0] = new TreeNode("Not Ready");
                return fail;
            }

            TreeNode[] output = new TreeNode[1];
            int j = 0;
            foreach (Item thing in items)
            {
                if (thing.getMarketGroupID() == marketID)
                {
                    TreeNode[] temp = new TreeNode[j + 1];
                    for (int u = 0; u <= j - 1; ++u)
                    {
                        temp[u] = output[u];
                    }

                    temp[j] = new TreeNode(thing.getName());
                    output = temp;

                    ++j;
                }
            }

            if(output[0] == null)
            {
                output[0] = new TreeNode("Incorrect DATA");
            }

            return output;
        }

        public void populateShoppingCart(Item item)
        {
            //update shopping list
            //shoppingList = getItemMaterials(item, shoppingList);
            shoppingList = MerdgeItemMaterials(item.getProdMats(), shoppingList);

            Int64[,,] price = new Int64[5, (shoppingList.Length / 2), 1];
            int items = 0;
            for (int s = 0; s < 5; ++s)
            {
                for (int p = 0; p < (shoppingList.Length / 2); ++p)
                {
                    //price[station, item, 0]
                    price[s, p, 0] = getItemValue(Convert.ToInt32(shoppingList[p, 1]), shoppingList[p, 0], s);
                    items = p;
                }
            }

            //create and populate a table
            DataTable shoppingCart = new DataTable();
            shoppingCart.Columns.Add("Name", typeof(string));
            shoppingCart.Columns.Add("Quantity", typeof(Int64));
            shoppingCart.Columns.Add(stationNames[0], typeof(string));
            shoppingCart.Columns.Add(stationNames[1], typeof(string));
            shoppingCart.Columns.Add(stationNames[2], typeof(string));
            shoppingCart.Columns.Add(stationNames[3], typeof(string));
            shoppingCart.Columns.Add(stationNames[4], typeof(string));

            for (int i = 0; i <= items; ++i)
            {
                shoppingCart.Rows.Add(MaterialName(Convert.ToInt32(shoppingList[i, 1])),
                    shoppingList[i, 0],
                    format(price[0, i, 0].ToString()),
                    format(price[1, i, 0].ToString()),
                    format(price[2, i, 0].ToString()),
                    format(price[3, i, 0].ToString()),
                    format(price[4, i, 0].ToString()));
            }

            ShoppingCart.DataSource = shoppingCart;
        }

        public void EmptyShoppingCart()
        {
            shoppingList = null;
        }

        private string MaterialName(int typeID)
        {
            if (prodMats == null || items == null)
            {
                return " ";
            }

            for (int i = 0; i < prodMats.Length - 1; ++i)
            {
                if (prodMats[i].ID == typeID)
                {
                    return prodMats[i].name;
                }
            }

            foreach (var name in items)
            {
                if (name.getTypeID() == typeID)
                {
                    return name.getName();
                }
            }

            return " ";
        }

        private Int64[,] getItemMaterials(Item item, Int64[,] existingList)
        {
            if (existingList == null)
            {
                //apply the current ME level
                Int64[,] material = RemoveZero(item.getProdMats());
                for (int i = 0; i < (material.Length / 2) - 1; ++i)
                {
                    material[i, 0] = CorrectRounding(material[i, 0] * (1 - (0.01f * MESlider.Value))) * Convert.ToInt32(RunSelect.Value);
                }
                return material;
            }

            Int64[,] materials = RemoveZero(item.getProdMats()),
                output = new Int64[((existingList.Length + materials.Length) / 2) - 1, 2],
                existing = existingList;
            int runs = Convert.ToInt32(RunSelect.Value),
                filled = 0;
            bool itemFound = false;

            //apply the current ME level
            for (int i = 0; i < (materials.Length / 2) - 1; ++i)
            {
                materials[i, 0] = CorrectRounding(materials[i, 0] * (1 - (0.01f * MESlider.Value))) *
                    Convert.ToInt32(RunSelect.Value);
            }

            //merdge doubles with output
            for (int m = 0; m < (materials.Length / 2) - 1; ++m)
            {
                if (materials[m, 1] == 0)
                {
                    continue;
                }

                for (int i = 0; i <= (existing.Length / 2) - 1 && itemFound == false; ++i)
                {
                    if (materials[m, 1] == existing[i, 1])
                    {
                        output[m, 0] = (materials[m, 0] * runs) + existing[i, 0];
                        output[m, 1] = materials[m, 1];
                        itemFound = true;
                        ++filled;

                        materials[m, 0] = 0;
                        materials[m, 1] = 0;
                        existing[m, 0] = 0;
                        existing[m, 0] = 0;
                    }
                }
                itemFound = false;
            }

            int skip = 0;
            for (int j = 0; j < ((output.Length / 2) - 2); ++j)
            {
                int merdge = -1;
                for (int i = skip; i < ((materials.Length / 2) - 1) && merdge == 0 - 1; ++i)
                {
                    if (output[j, 1] != materials[i, 1] && materials[i, 1] != 0)
                    {
                        merdge = i;
                        ++skip;
                    }
                }
                if (merdge != -1)
                {
                    output[filled, 0] = materials[merdge, 0];
                    output[filled, 1] = materials[merdge, 1];
                    ++filled;
                }
            }

            skip = 0;
            for (int j = 0; j < ((output.Length / 2) - 2); ++j)
            {
                int merdge = -1;
                for (int i = skip; i < ((existing.Length / 2) - 1) && merdge == 0 - 1; ++i)
                {
                    if (output[j, 1] != existing[i, 1] && existing[i, 1] != 0)
                    {
                        merdge = i;
                        ++skip;
                    }
                }
                if (merdge != -1)
                {
                    output[filled, 0] = existing[merdge, 0];
                    output[filled, 1] = existing[merdge, 1];
                    ++filled;
                }
            }

            //output = removeDoubleMerdge(output, materials, ref filled, existingList);
            //output = removeDoubleMerdge(output, existing, ref filled, materials);

            return RemoveZero(output);
        }

        private Int64[,] MerdgeItemMaterials(Int64[,] dataset1, Int64[,] dataSet2)
        {
            if (dataSet2 == null)
            {
                //apply the current ME level
                Int64[,] material = dataset1;
                for (int i = 0; i < (material.Length / 2) - 1; ++i)
                {
                    material[i, 0] = CorrectRounding(material[i, 0] * (1 - (0.01f * MESlider.Value))) *
                        Convert.ToInt32(RunSelect.Value);
                }
                return RemoveZero(material);
            }

            Int64[,] dataset2 = dataSet2,
                output = new Int64[((dataset1.Length + dataset2.Length) / 2) - 1, 2];

            //move dataset 1 to output and apply ME level + runs
            int position = 0;
            for (int i = 0; i < (dataset1.Length / 2) - 1; ++i)
            {
                if (dataset1[i, 1] != 0)
                {
                    output[i, 0] = CorrectRounding(dataset1[i, 0] * (1 - (0.01f * MESlider.Value))) *
                        Convert.ToInt32(RunSelect.Value);
                    output[i, 1] = dataset1[i, 1];
                    position = i;
                }
            }

            //merdge already existing items to output

            for (int i = 0; i < (output.Length / 2) - 1; ++i)
            {
                if (output[i, 0] != 0)
                {
                    bool found = false;
                    for (int j = 0; j < (dataset2.Length / 2) - 1; ++j)
                    {
                        if (output[i, 1] == dataset2[j, 1] && found == false)
                        {
                            output[i, 0] += dataset2[j, 0];
                            dataset2[j, 0] = 0;
                            dataset2[j, 1] = 0;
                            found = true;
                        }
                    }
                    //if (found == false)
                    //{

                    //}
                }
            }

            //add items that don't already exist in the output
            for (int i = 0; i < (output.Length / 2) - 1; ++i)
            {
                if (output[i, 0] != 0)
                {
                    for (int j = 0; j < (dataset2.Length / 2) - 1; ++j)
                    {
                        if (dataset2[j, 0] != 0)
                        {
                            output[position, 0] = dataset2[j, 0];
                            output[position, 1] = dataset2[j, 1];
                            ++position;
                            dataset2[j, 0] = 0;
                            dataset2[j, 1] = 0;
                        }
                    }
                }
            }

            return RemoveZero(output);
        }

        private Int64[,] RemoveZero(Int64[,] input)
        {
            //figure out how many non-null values there are
            int valid = 0;
            Int64[,] output = input;
            for (int i = 0; i < (output.Length / 2) - 1; ++i)
            {
                if (output[i, 0] != 0)
                {
                    ++valid;
                }
            }

            //remove nulls
            Int64[,] temp = output;
            output = new Int64[valid, 2];
            for (int i = 0; i < valid; ++i)
            {
                if (temp[i, 1] != 0)
                {
                    output[i, 0] = temp[i, 0];
                    output[i, 1] = temp[i, 1];
                }
            }

            return output;
        }

        public void Settings()
        {
            setDefaultSettings();
            string documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                evePath = documentPath + "\\EVE\\zz EvE-Build",
                settingsPath = evePath + "\\Settings.txt";

            //figure out if a file already exists
            StreamReader reader;

            if (!(Directory.Exists(evePath)))
            {
                Directory.CreateDirectory(evePath);
            }

            try
            {
                reader = new StreamReader(settingsPath);
                string line = "";

                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "Stations")
                    {
                        line = reader.ReadLine();
                        string temp;
                        for (int i = 0; i < 5; ++i)
                        {
                            temp = line.Remove(line.IndexOf(","));
                            stationNames[i] = temp;

                            temp = line.Substring(line.IndexOf(",") + 1);
                            stationIds[i] = Int32.Parse(temp);
                            line = reader.ReadLine();
                        }
                    }

                    if (line.StartsWith("UpdateStart"))
                    {
                        string temp = line.Remove(line.IndexOf(" ") + 1);
                        if (temp == "False")
                        {
                            updateOnStartup = false;
                        }
                        else if (temp == "True")
                        {
                            updateOnStartup = true;
                        }
                    }

                    if (line.StartsWith("UpdateInterval"))
                    {
                        string temp = line.Substring(line.IndexOf(" ") + 1);
                        updateInterval = Int32.Parse(temp);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                //start generating the default settings for a new file
                StreamWriter newSettings = new StreamWriter(settingsPath);
                newSettings.WriteLine("Stations");

                for (int i = 0; i < stationNames.Length; ++i)
                {
                    newSettings.WriteLine(stationNames[i] + "," + stationIds[i]);
                }

                newSettings.WriteLine("UpdateStart: " + updateOnStartup.ToString());
                newSettings.WriteLine("UpdateInterval: " + updateInterval);

                newSettings.Close();
            }
        }

        void setDefaultSettings()
        {
            //set the default values
            stationNames = new string[5];
            stationNames[0] = "Jita";
            stationNames[1] = "Amarr";
            stationNames[2] = "";
            stationNames[3] = "";
            stationNames[4] = "";

            stationIds = new int[5];
            stationIds[0] = 30000142;
            stationIds[1] = 30002187;
            stationIds[2] = 0;
            stationIds[3] = 0;
            stationIds[4] = 0;

            updateOnStartup = true;
        }
    }
}
