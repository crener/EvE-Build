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
        Label RunsToPay,
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
            int loadChuck = 20;

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
                    int division = (items.Length - 1) / loadChuck;

                    //update station data
                    for (int l = 0; l < 5 && stationIds[l] != 0; ++l)
                    {
                        int upto = 0;
                        int[] search = new int[loadChuck];
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

                            for (int i = 0; i <= loadChuck - 1 && upto != prodMats.Length - 1; ++i)
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

                            search = new int[loadChuck];
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
                        int[] search = new int[loadChuck];
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
                if (value > 100)
                {
                    value = 100;
                }
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

        public string format(string text)
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

            #region  frequency crystals
            GroupView.Nodes[0].Nodes[0].Nodes.Add("Advanced Beam Laser Crystals");
            GroupView.Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[0].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(866));
            GroupView.Nodes[0].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(867));
            GroupView.Nodes[0].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(868));
            GroupView.Nodes[0].Nodes[0].Nodes.Add("Advanced Pulse Laser Crystals");
            GroupView.Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[0].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(869));
            GroupView.Nodes[0].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(870));
            GroupView.Nodes[0].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(871));
            //GroupView.Nodes[0].Nodes[0].Nodes.Add("Faction Crystals");
            //GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateSubAmmoCat(1));
            //GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1007));
            //GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(995));
            //GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(996));
            //GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(997));
            GroupView.Nodes[0].Nodes[0].Nodes.Add("Standard Crystals");
            GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(503));
            GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(105));
            GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(103));
            GroupView.Nodes[0].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(102));
            GroupView.Nodes[0].Nodes[0].Nodes.Add("Orbital Strike");
            GroupView.Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1599));
            #endregion
            #region  Hybrid Charges
            GroupView.Nodes[0].Nodes[1].Nodes.Add("Advanced Blaster Crystals");
            GroupView.Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[0].Nodes[1].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(860));
            GroupView.Nodes[0].Nodes[1].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(861));
            GroupView.Nodes[0].Nodes[1].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(862));
            GroupView.Nodes[0].Nodes[1].Nodes.Add("Advanced Railgun Crystals");
            GroupView.Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[0].Nodes[1].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(863));
            GroupView.Nodes[0].Nodes[1].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(864));
            GroupView.Nodes[0].Nodes[1].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(865));
            GroupView.Nodes[0].Nodes[1].Nodes.Add("Standard Crystals");
            GroupView.Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[0].Nodes[1].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(504));
            GroupView.Nodes[0].Nodes[1].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(106));
            GroupView.Nodes[0].Nodes[1].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(108));
            GroupView.Nodes[0].Nodes[1].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(107));
            GroupView.Nodes[0].Nodes[1].Nodes.Add("Orbital Strike");
            GroupView.Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1600));
            #endregion
            #region Missiles
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Auto-Targeting");
            GroupView.Nodes[0].Nodes[2].Nodes[0].Nodes.Add("Standard Auto-Targeting");
            GroupView.Nodes[0].Nodes[2].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(914));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Citadel Cruise Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[1].Nodes.Add("Standard Citadel Cruise");
            GroupView.Nodes[0].Nodes[2].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1287));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Citadel Trodedoes Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[2].Nodes.Add("Standard Citadel Trodedoes");
            GroupView.Nodes[0].Nodes[2].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1193));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Cruise Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes.Add("Advanced High Damage");
            GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(925));
            GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes.Add("Advanced High Precision");
            GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(918));
            GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes.Add("Standard Cruise Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(912));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Heavy Assault Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes.Add("Advanced Anti-Ship Heavy Assault Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(973));
            GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes.Add("Advanced Long Range Heavy Assault Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(972));
            GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes.Add("Standard Heavy Assault Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(971));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Heavy Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes.Add("Advanced High Damage Heavy Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes[0].Nodes.AddRange(PopulateTreeCategory(926));
            GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes.Add("Advanced High Precision Heavy Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes[1].Nodes.AddRange(PopulateTreeCategory(925));
            GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes.Add("Standard Heavy Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[5].Nodes[2].Nodes.AddRange(PopulateTreeCategory(924));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Light Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes.Add("Advanced High Damage Light Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes[0].Nodes.AddRange(PopulateTreeCategory(928));
            GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes.Add("Advanced High Precision Light Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes[1].Nodes.AddRange(PopulateTreeCategory(927));
            GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes.Add("Standard Light Missiles");
            GroupView.Nodes[0].Nodes[2].Nodes[6].Nodes[2].Nodes.AddRange(PopulateTreeCategory(920));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Rockets");
            GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes.Add("Advanced Anti-Ship Rockets");
            GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes[0].Nodes.AddRange(PopulateTreeCategory(930));
            GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes.Add("Advanced Long Range Rockets");
            GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes[1].Nodes.AddRange(PopulateTreeCategory(928));
            GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes.Add("Standard Rockets");
            GroupView.Nodes[0].Nodes[2].Nodes[7].Nodes[2].Nodes.AddRange(PopulateTreeCategory(922));
            GroupView.Nodes[0].Nodes[2].Nodes.Add("Defender");
            GroupView.Nodes[0].Nodes[2].Nodes[8].Nodes.AddRange(PopulateTreeCategory(116));
            #endregion
            #region Probes
            GroupView.Nodes[0].Nodes[3].Nodes.Add("Interdiction Probes");
            GroupView.Nodes[0].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1201));
            GroupView.Nodes[0].Nodes[3].Nodes.Add("Scan Probes");
            GroupView.Nodes[0].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1199));
            GroupView.Nodes[0].Nodes[3].Nodes.Add("Survay Probes");
            GroupView.Nodes[0].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1200));
            #endregion
            #region Projectile
            GroupView.Nodes[0].Nodes[4].Nodes.Add("Advanced Blaster Crystals");
            GroupView.Nodes[0].Nodes[4].Nodes[0].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[0].Nodes[4].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(854));
            GroupView.Nodes[0].Nodes[4].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(855));
            GroupView.Nodes[0].Nodes[4].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(856));
            GroupView.Nodes[0].Nodes[4].Nodes.Add("Advanced Railgun Crystals");
            GroupView.Nodes[0].Nodes[4].Nodes[1].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[0].Nodes[4].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(857));
            GroupView.Nodes[0].Nodes[4].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(858));
            GroupView.Nodes[0].Nodes[4].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(859));
            GroupView.Nodes[0].Nodes[4].Nodes.Add("Standard Crystals");
            GroupView.Nodes[0].Nodes[4].Nodes[2].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[0].Nodes[4].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(502));
            GroupView.Nodes[0].Nodes[4].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(109));
            GroupView.Nodes[0].Nodes[4].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(112));
            GroupView.Nodes[0].Nodes[4].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(113));
            GroupView.Nodes[0].Nodes[4].Nodes.Add("Orbital Strike");
            GroupView.Nodes[0].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1598));
            #endregion

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

            for (int i = 0; i <= 1; ++i)
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
            #region ShipEquipment
            #region EWar
            GroupView.Nodes[3].Nodes.Add("Electronic Warefare");
            GroupView.Nodes[3].Nodes[0].Nodes.Add("ECCM");
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Gravimetric Sensors");
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(725));
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Ladar Sensors");
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(726));
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Magnetometric Sensors");
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(727));
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Multi-Spectrum Sensors");
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(728));
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Radar Sensors");
            GroupView.Nodes[3].Nodes[0].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(729));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Electronic Counter Measures");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Gravimetric Jammers");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(717));
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Ladar Jammers");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(716));
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Magnetometric Jammers");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(715));
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Multi Spectrum Jammers");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(714));
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Radar Jammers");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(713));
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Signal Distortion Jammers");
            GroupView.Nodes[3].Nodes[0].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(712));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Sensor Backup Arrays");
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Gravimetric Backup Arrays");
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(720));
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Ladar Backup Arrays");
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(721));
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Magnetometric Backup Arrays");
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(722));
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Multi-Frequency Backup Arrays");
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(723));
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Radar Backup Arrays");
            GroupView.Nodes[3].Nodes[0].Nodes[2].Nodes[4].Nodes.AddRange(PopulateTreeCategory(724));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("ECM Burst");
            GroupView.Nodes[3].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(678));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Interdiction Sphere Laucher");
            GroupView.Nodes[3].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1937));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Projected ECCM");
            GroupView.Nodes[3].Nodes[0].Nodes[5].Nodes.AddRange(PopulateTreeCategory(686));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Remote Sensor Dampeners");
            GroupView.Nodes[3].Nodes[0].Nodes[6].Nodes.AddRange(PopulateTreeCategory(679));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Stasis Webifiers");
            GroupView.Nodes[3].Nodes[0].Nodes[7].Nodes.AddRange(PopulateTreeCategory(683));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Target Breaker");
            GroupView.Nodes[3].Nodes[0].Nodes[8].Nodes.AddRange(PopulateTreeCategory(1426));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Target Painters");
            GroupView.Nodes[3].Nodes[0].Nodes[9].Nodes.AddRange(PopulateTreeCategory(757));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Tracking Disruptors");
            GroupView.Nodes[3].Nodes[0].Nodes[10].Nodes.AddRange(PopulateTreeCategory(680));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Warp Disruption Field");
            GroupView.Nodes[3].Nodes[0].Nodes[11].Nodes.AddRange(PopulateTreeCategory(1085));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Warp Disruptors");
            GroupView.Nodes[3].Nodes[0].Nodes[12].Nodes.AddRange(PopulateTreeCategory(1935));

            GroupView.Nodes[3].Nodes[0].Nodes.Add("Warp Scramblers");
            GroupView.Nodes[3].Nodes[0].Nodes[13].Nodes.AddRange(PopulateTreeCategory(1936));
            #endregion
            #region Anti-Ewar
            GroupView.Nodes[3].Nodes.Add("Electronics and Sensor Upgrades");
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Automated Targeting Systems");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(670));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Cloaking Devices");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(675));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("CPU Upgrades");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(676));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Passive Targeting Systems");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(672));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Remote Sensor Boosters");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(673));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Sensor Boosters");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(671));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Signal Amplifiers");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(669));
            GroupView.Nodes[3].Nodes[1].Nodes.Add("Tractor Beams");
            GroupView.Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(872));
            #endregion
            #region Engineering Eq
            GroupView.Nodes[3].Nodes.Add("Engineering Equipment");
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Capacitor Batteries");
            GroupView.Nodes[3].Nodes[2].Nodes[0].Nodes.AddRange(PopulateSub(2));
            GroupView.Nodes[3].Nodes[2].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(705));
            GroupView.Nodes[3].Nodes[2].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(704));
            GroupView.Nodes[3].Nodes[2].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(702));
            GroupView.Nodes[3].Nodes[2].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(703));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Capacitor Boosters");
            GroupView.Nodes[3].Nodes[2].Nodes[1].Nodes.AddRange(PopulateSub(5));
            GroupView.Nodes[3].Nodes[2].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(701));
            GroupView.Nodes[3].Nodes[2].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(700));
            GroupView.Nodes[3].Nodes[2].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(699));
            GroupView.Nodes[3].Nodes[2].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(698));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Energy Destabilizers");
            GroupView.Nodes[3].Nodes[2].Nodes[2].Nodes.AddRange(PopulateSub(3));
            GroupView.Nodes[3].Nodes[2].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(691));
            GroupView.Nodes[3].Nodes[2].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(690));
            GroupView.Nodes[3].Nodes[2].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(689));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Energy Vampires");
            GroupView.Nodes[3].Nodes[2].Nodes[3].Nodes.AddRange(PopulateSub(3));
            GroupView.Nodes[3].Nodes[2].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(694));
            GroupView.Nodes[3].Nodes[2].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(693));
            GroupView.Nodes[3].Nodes[2].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(692));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Remote Capacitor Transmitters");
            GroupView.Nodes[3].Nodes[2].Nodes[4].Nodes.AddRange(PopulateSub(4));
            GroupView.Nodes[3].Nodes[2].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(910));
            GroupView.Nodes[3].Nodes[2].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(697));
            GroupView.Nodes[3].Nodes[2].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(696));
            GroupView.Nodes[3].Nodes[2].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(695));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Auxiliary Power Controls");
            GroupView.Nodes[3].Nodes[2].Nodes[5].Nodes.AddRange(PopulateTreeCategory(660));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Capacitor Flux Coils");
            GroupView.Nodes[3].Nodes[2].Nodes[6].Nodes.AddRange(PopulateTreeCategory(666));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Capacitor Power Relays");
            GroupView.Nodes[3].Nodes[2].Nodes[7].Nodes.AddRange(PopulateTreeCategory(667));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Capacitor Rechargers");
            GroupView.Nodes[3].Nodes[2].Nodes[8].Nodes.AddRange(PopulateTreeCategory(665));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Power Diagnostic Systems");
            GroupView.Nodes[3].Nodes[2].Nodes[9].Nodes.AddRange(PopulateTreeCategory(658));
            GroupView.Nodes[3].Nodes[2].Nodes.Add("Power Control Units");
            GroupView.Nodes[3].Nodes[2].Nodes[10].Nodes.AddRange(PopulateTreeCategory(659));
            #endregion
            #region Fleet
            GroupView.Nodes[3].Nodes.Add("Fleet Assistance Modules");
            GroupView.Nodes[3].Nodes[3].Nodes.Add("Warefare Links");
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes.Add("Armored Warefare Links");
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1634));
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes.Add("Information Warefare Links");
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1635));
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes.Add("Siege Warefare Links");
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1636));
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes.Add("SkirmishWarefare Links");
            GroupView.Nodes[3].Nodes[3].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1637));
            GroupView.Nodes[3].Nodes[3].Nodes.Add("Clone Vat Bays");
            GroupView.Nodes[3].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1642));
            GroupView.Nodes[3].Nodes[3].Nodes.Add("Command Processors");
            GroupView.Nodes[3].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1639));
            GroupView.Nodes[3].Nodes[3].Nodes.Add("Cynosural Field Generators");
            GroupView.Nodes[3].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1641));
            GroupView.Nodes[3].Nodes[3].Nodes.Add("Jump Portal Generators");
            GroupView.Nodes[3].Nodes[3].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1640));
            GroupView.Nodes[3].Nodes[3].Nodes.Add("Mining Foreman Links");
            GroupView.Nodes[3].Nodes[3].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1638));
            #endregion
            #region Harvest
            GroupView.Nodes[3].Nodes.Add("Harvest Equipment");
            GroupView.Nodes[3].Nodes[4].Nodes.Add("Gas Cloud Harvesters");
            GroupView.Nodes[3].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1037));
            GroupView.Nodes[3].Nodes[4].Nodes.Add("Ice Harvesters");
            GroupView.Nodes[3].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1038));
            GroupView.Nodes[3].Nodes[4].Nodes.Add("Mining Lasers");
            GroupView.Nodes[3].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1039));
            GroupView.Nodes[3].Nodes[4].Nodes.Add("Mining Upgrades");
            GroupView.Nodes[3].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(935));
            GroupView.Nodes[3].Nodes[4].Nodes.Add("Salvagers");
            GroupView.Nodes[3].Nodes[4].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1715));
            GroupView.Nodes[3].Nodes[4].Nodes.Add("Strip Miners");
            GroupView.Nodes[3].Nodes[4].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1040));
            #endregion
            #region H&A
            GroupView.Nodes[3].Nodes.Add("Hull & Armor");
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Armor Hardeners");
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes.Add("EM Armor Hardeners");
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1681));
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes.Add("Explosive Armor Hardeners");
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1680));
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes.Add("Kinectic Armor Hardeners");
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1679));
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes.Add("Thermal Armor Hardeners");
            GroupView.Nodes[3].Nodes[5].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1678));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Armor Plates");
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes.Add("100mm Armor Plate");
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1672));
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes.Add("1600mm Armor Plate");
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1676));
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes.Add("200mm Armor Plate");
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1673));
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes.Add("400mm Armor Plate");
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1674));
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes.Add("800mm Armor Plate");
            GroupView.Nodes[3].Nodes[5].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1675));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Armor Repairers");
            GroupView.Nodes[3].Nodes[5].Nodes[2].Nodes.AddRange(PopulateSub(4));
            GroupView.Nodes[3].Nodes[5].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1052));
            GroupView.Nodes[3].Nodes[5].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1051));
            GroupView.Nodes[3].Nodes[5].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1050));
            GroupView.Nodes[3].Nodes[5].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1049));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Energized Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes.Add("Energized Adaptive Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1686));
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes.Add("Energized EM Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1684));
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes.Add("Energized Explosive Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1682));
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes.Add("Energized Kinetic Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1685));
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes.Add("Energized Thermal Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[3].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1683));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Hull Repairers");
            GroupView.Nodes[3].Nodes[5].Nodes[4].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[3].Nodes[5].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1055));
            GroupView.Nodes[3].Nodes[5].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1054));
            GroupView.Nodes[3].Nodes[5].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1053));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Hull Upgrades");
            GroupView.Nodes[3].Nodes[5].Nodes[5].Nodes.Add("Expanded Cargoholds");
            GroupView.Nodes[3].Nodes[5].Nodes[5].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1197));
            GroupView.Nodes[3].Nodes[5].Nodes[5].Nodes.Add("Nanofiber Internal Structure");
            GroupView.Nodes[3].Nodes[5].Nodes[5].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1196));
            GroupView.Nodes[3].Nodes[5].Nodes[5].Nodes.Add("Reinforced Bulkheads");
            GroupView.Nodes[3].Nodes[5].Nodes[5].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1195));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Remote Armor Repairers");
            GroupView.Nodes[3].Nodes[5].Nodes[6].Nodes.AddRange(PopulateSub(4));
            GroupView.Nodes[3].Nodes[5].Nodes[6].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1063));
            GroupView.Nodes[3].Nodes[5].Nodes[6].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1062));
            GroupView.Nodes[3].Nodes[5].Nodes[6].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1061));
            GroupView.Nodes[3].Nodes[5].Nodes[6].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1060));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Remote Hull Repairers");
            GroupView.Nodes[3].Nodes[5].Nodes[7].Nodes.AddRange(PopulateSub(4));
            GroupView.Nodes[3].Nodes[5].Nodes[7].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1058));
            GroupView.Nodes[3].Nodes[5].Nodes[7].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1057));
            GroupView.Nodes[3].Nodes[5].Nodes[7].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1056));
            GroupView.Nodes[3].Nodes[5].Nodes[7].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1055));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Resistance Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes.Add("Adaptive Resistance Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1670));
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes.Add("EM Resistance Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1668));
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes.Add("Explosive Resistance Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1667));
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes.Add("Kinetic Resistance Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1666));
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes.Add("Thermal Resistance Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[8].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1665));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Damage Controls");
            GroupView.Nodes[3].Nodes[5].Nodes[9].Nodes.AddRange(PopulateTreeCategory(615));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Energized Armor Lighting");
            GroupView.Nodes[3].Nodes[5].Nodes[10].Nodes.AddRange(PopulateTreeCategory(1687));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Layered Plating");
            GroupView.Nodes[3].Nodes[5].Nodes[11].Nodes.AddRange(PopulateTreeCategory(1669));
            GroupView.Nodes[3].Nodes[5].Nodes.Add("Reactive Armor Hardener");
            GroupView.Nodes[3].Nodes[5].Nodes[12].Nodes.AddRange(PopulateTreeCategory(1416));
            #endregion
            #region Props
            GroupView.Nodes[3].Nodes.Add("Propulsion");
            GroupView.Nodes[3].Nodes[6].Nodes.Add("Propulsion Upgrades");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes.Add("Inertial Stabilizers");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1086));
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes.Add("Jump Economizers");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1941));
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes.Add("Overdrives");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1087));
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes.Add("Warp Accelerators");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1931));
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes.Add("Warp Core Stabilizers");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1088));
            GroupView.Nodes[3].Nodes[6].Nodes.Add("Afterburners");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(542));
            GroupView.Nodes[3].Nodes[6].Nodes.Add("Micro Jump Drives");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1650));
            GroupView.Nodes[3].Nodes[6].Nodes.Add("Microwarpdrives");
            GroupView.Nodes[3].Nodes[6].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(131));
            #endregion
            #region scanning
            GroupView.Nodes[3].Nodes.Add("Scanning Equipment");
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Analyzers");
            GroupView.Nodes[3].Nodes[7].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1718));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Cargo Scanners");
            GroupView.Nodes[3].Nodes[7].Nodes[1].Nodes.AddRange(PopulateTreeCategory(711));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Entosis Links");
            GroupView.Nodes[3].Nodes[7].Nodes[2].Nodes.AddRange(PopulateTreeCategory(2018));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Scan Probe Launchers");
            GroupView.Nodes[3].Nodes[7].Nodes[3].Nodes.AddRange(PopulateTreeCategory(712));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Scanning Upgrades");
            GroupView.Nodes[3].Nodes[7].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1709));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Ship Upgrades");
            GroupView.Nodes[3].Nodes[7].Nodes[5].Nodes.AddRange(PopulateTreeCategory(713));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Survay Probe Launchers");
            GroupView.Nodes[3].Nodes[7].Nodes[6].Nodes.AddRange(PopulateTreeCategory(1717));
            GroupView.Nodes[3].Nodes[7].Nodes.Add("Survay Scanners");
            GroupView.Nodes[3].Nodes[7].Nodes[7].Nodes.AddRange(PopulateTreeCategory(714));
            #endregion
            #region shield
            GroupView.Nodes[3].Nodes.Add("Shield");
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Remote Shield Boosters");
            GroupView.Nodes[3].Nodes[8].Nodes[0].Nodes.AddRange(PopulateSub(6));
            GroupView.Nodes[3].Nodes[8].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(600));
            GroupView.Nodes[3].Nodes[8].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(601));
            GroupView.Nodes[3].Nodes[8].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(602));
            GroupView.Nodes[3].Nodes[8].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(604));
            GroupView.Nodes[3].Nodes[8].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(603));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Boosters");
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes.Add("Boost Amplifiers");
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(601));
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes.AddRange(PopulateSub(7));
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(778));
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(612));
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(611));
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(610));
            GroupView.Nodes[3].Nodes[8].Nodes[1].Nodes[5].Nodes.AddRange(PopulateTreeCategory(609));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Extenders");
            GroupView.Nodes[3].Nodes[8].Nodes[2].Nodes.AddRange(PopulateSub(0));
            GroupView.Nodes[3].Nodes[8].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(608));
            GroupView.Nodes[3].Nodes[8].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(606));
            GroupView.Nodes[3].Nodes[8].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(605));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Hardeners");
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes.Add("Adaptive Shield Hardener");
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1696));
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes.Add("EM Shield Hardener");
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1695));
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes.Add("Explosive Shield Hardener");
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1694));
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes.Add("Kinetic Shield Hardener");
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1693));
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes.Add("Thermal Shield Hardener");
            GroupView.Nodes[3].Nodes[8].Nodes[3].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1694));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Resistance Amplifiers");
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes.Add("EM  Resistance Amplifier");
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1690));
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes.Add("Explosive Resistance Amplifier");
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1689));
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes.Add("Kinetic Resistance Amplifier");
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1688));
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes.Add("Thermal Resistance Amplifier");
            GroupView.Nodes[3].Nodes[8].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1687));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Flux Coils");
            GroupView.Nodes[3].Nodes[8].Nodes[5].Nodes.AddRange(PopulateTreeCategory(687));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Power Relays");
            GroupView.Nodes[3].Nodes[8].Nodes[6].Nodes.AddRange(PopulateTreeCategory(688));
            GroupView.Nodes[3].Nodes[8].Nodes.Add("Shield Rechargers");
            GroupView.Nodes[3].Nodes[8].Nodes[7].Nodes.AddRange(PopulateTreeCategory(126));
            #endregion
            #region smartbombs
            GroupView.Nodes[3].Nodes.Add("Smartbombs");
            GroupView.Nodes[3].Nodes[9].Nodes.AddRange(PopulateSub(2));
            GroupView.Nodes[3].Nodes[9].Nodes[0].Nodes.AddRange(PopulateTreeCategory(381));
            GroupView.Nodes[3].Nodes[9].Nodes[0].Nodes.AddRange(PopulateTreeCategory(383));
            GroupView.Nodes[3].Nodes[9].Nodes[0].Nodes.AddRange(PopulateTreeCategory(380));
            GroupView.Nodes[3].Nodes[9].Nodes[0].Nodes.AddRange(PopulateTreeCategory(382));
            #endregion
            #region T&B
            GroupView.Nodes[3].Nodes.Add("Turrents & Bays");
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Energy Turrets");
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes.Add("Beam Lasers");
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[0].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(773));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(569));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(568));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(567));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes.Add("Pulse Lasers");
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[1].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(774));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(573));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(574));
            GroupView.Nodes[3].Nodes[10].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(570));
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Hybrid Turrets");
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes.Add("Blasers");
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[0].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(771));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(563));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(562));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(561));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes.Add("Railguns");
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[1].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(772));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(566));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(565));
            GroupView.Nodes[3].Nodes[10].Nodes[1].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(564));
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Missile Lanuchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Citadel Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(777));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Cruise Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(643));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Heavy Assault Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(974));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Heavy Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(642));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Light Missile Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[4].Nodes.AddRange(PopulateTreeCategory(640));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Rapid Heavy Missile Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1827));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Rapid Light Missile Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[6].Nodes.AddRange(PopulateTreeCategory(641));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Rocket Laucher");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[7].Nodes.AddRange(PopulateTreeCategory(639));
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes.Add("Torpedo Laucher");
            GroupView.Nodes[3].Nodes[10].Nodes[2].Nodes[8].Nodes.AddRange(PopulateTreeCategory(644));
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Projectile Turrets");
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes.Add("Artillery Cannon");
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[0].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(775));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(579));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(578));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(577));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes.Add("Autocannons");
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[1].Nodes.AddRange(PopulateSub(1));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(776));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(576));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(575));
            GroupView.Nodes[3].Nodes[10].Nodes[3].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(574));
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Weapon Upgrades");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Ballistic Control System");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(645));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Gyrostabilizers");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(646));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Heat Sinks");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(647));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Magnetic Field Stabilizer");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(648));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Missile Guidance Computer");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[4].Nodes.AddRange(PopulateTreeCategory(2032));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Missile Guidance Enhancer");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[5].Nodes.AddRange(PopulateTreeCategory(2033));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Remote Tracking Computer");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[6].Nodes.AddRange(PopulateTreeCategory(708));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Siege Modules");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[7].Nodes.AddRange(PopulateTreeCategory(801));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Tracking Computer");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[8].Nodes.AddRange(PopulateTreeCategory(706));
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes.Add("Tracking Enhancer");
            GroupView.Nodes[3].Nodes[10].Nodes[4].Nodes[9].Nodes.AddRange(PopulateTreeCategory(707));
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Bomb Launchers");
            GroupView.Nodes[3].Nodes[10].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1014));
            GroupView.Nodes[3].Nodes[10].Nodes.Add("Doomsday Devices");
            GroupView.Nodes[3].Nodes[10].Nodes[6].Nodes.AddRange(PopulateTreeCategory(912));
            #endregion
            #region Drone
            GroupView.Nodes[3].Nodes.Add("Drone Upgrades");
            GroupView.Nodes[3].Nodes[11].Nodes.AddRange(PopulateTreeCategory(938));
            #endregion
            #endregion
            #region ShipModification
            #region Rigs
            GroupView.Nodes[4].Nodes.Add("Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Armor Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[0].Nodes.AddRange(PopulateRigs("Armor"));
            GroupView.Nodes[4].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1730));
            GroupView.Nodes[4].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1208));
            GroupView.Nodes[4].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1207));
            GroupView.Nodes[4].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1206));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Astronautic Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[1].Nodes.AddRange(PopulateRigs("Astronautic"));
            GroupView.Nodes[4].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1740));
            GroupView.Nodes[4].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1212));
            GroupView.Nodes[4].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1211));
            GroupView.Nodes[4].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1210));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Drone Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[2].Nodes.AddRange(PopulateRigs("Drone"));
            GroupView.Nodes[4].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1739));
            GroupView.Nodes[4].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1215));
            GroupView.Nodes[4].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1214));
            GroupView.Nodes[4].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1213));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Electronics Superiority Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[3].Nodes.AddRange(PopulateRigs("Electronics Superiority"));
            GroupView.Nodes[4].Nodes[0].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1737));
            GroupView.Nodes[4].Nodes[0].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1221));
            GroupView.Nodes[4].Nodes[0].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1220));
            GroupView.Nodes[4].Nodes[0].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1219));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Energy Weapon Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[4].Nodes.AddRange(PopulateRigs("Energy Weapon"));
            GroupView.Nodes[4].Nodes[0].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1735));
            GroupView.Nodes[4].Nodes[0].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1227));
            GroupView.Nodes[4].Nodes[0].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1226));
            GroupView.Nodes[4].Nodes[0].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1225));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Engineering Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[5].Nodes.AddRange(PopulateRigs("Engineering"));
            GroupView.Nodes[4].Nodes[0].Nodes[5].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1736));
            GroupView.Nodes[4].Nodes[0].Nodes[5].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1224));
            GroupView.Nodes[4].Nodes[0].Nodes[5].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1223));
            GroupView.Nodes[4].Nodes[0].Nodes[5].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1222));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Hybrid Weapon Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[6].Nodes.AddRange(PopulateRigs("Hybrid Weapon"));
            GroupView.Nodes[4].Nodes[0].Nodes[6].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1735));
            GroupView.Nodes[4].Nodes[0].Nodes[6].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1230));
            GroupView.Nodes[4].Nodes[0].Nodes[6].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1229));
            GroupView.Nodes[4].Nodes[0].Nodes[6].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1228));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Missile Launcher Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[7].Nodes.AddRange(PopulateRigs("Missile Launcher"));
            GroupView.Nodes[4].Nodes[0].Nodes[7].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1733));
            GroupView.Nodes[4].Nodes[0].Nodes[7].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1233));
            GroupView.Nodes[4].Nodes[0].Nodes[7].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1232));
            GroupView.Nodes[4].Nodes[0].Nodes[7].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1231));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Projectile Weapon Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[8].Nodes.AddRange(PopulateRigs("Projectile Weapon"));
            GroupView.Nodes[4].Nodes[0].Nodes[8].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1731));
            GroupView.Nodes[4].Nodes[0].Nodes[8].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1239));
            GroupView.Nodes[4].Nodes[0].Nodes[8].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1238));
            GroupView.Nodes[4].Nodes[0].Nodes[8].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1237));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Resource Processing Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[9].Nodes.AddRange(PopulateRigs("Resource Processing"));
            GroupView.Nodes[4].Nodes[0].Nodes[9].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1785));
            GroupView.Nodes[4].Nodes[0].Nodes[9].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1784));
            GroupView.Nodes[4].Nodes[0].Nodes[9].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1783));
            GroupView.Nodes[4].Nodes[0].Nodes[9].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1782));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Scanning Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[10].Nodes.AddRange(PopulateRigs("Scanning"));
            GroupView.Nodes[4].Nodes[0].Nodes[10].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1789));
            GroupView.Nodes[4].Nodes[0].Nodes[10].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1788));
            GroupView.Nodes[4].Nodes[0].Nodes[10].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1787));
            GroupView.Nodes[4].Nodes[0].Nodes[10].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1786));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Shield Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[11].Nodes.AddRange(PopulateRigs("Shield"));
            GroupView.Nodes[4].Nodes[0].Nodes[11].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1732));
            GroupView.Nodes[4].Nodes[0].Nodes[11].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1236));
            GroupView.Nodes[4].Nodes[0].Nodes[11].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1235));
            GroupView.Nodes[4].Nodes[0].Nodes[11].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1234));
            GroupView.Nodes[4].Nodes[0].Nodes.Add("Targeting Rigs");
            GroupView.Nodes[4].Nodes[0].Nodes[12].Nodes.AddRange(PopulateRigs("Targeting"));
            GroupView.Nodes[4].Nodes[0].Nodes[12].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1793));
            GroupView.Nodes[4].Nodes[0].Nodes[12].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1792));
            GroupView.Nodes[4].Nodes[0].Nodes[12].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1791));
            GroupView.Nodes[4].Nodes[0].Nodes[12].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1790));
            #endregion
            #region Subsystems
            GroupView.Nodes[4].Nodes.Add("Subsystems");
            GroupView.Nodes[4].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[4].Nodes[1].Nodes[0].Nodes.AddRange(PopulateSubsystem("Amarr"));
            GroupView.Nodes[4].Nodes[1].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1126));
            GroupView.Nodes[4].Nodes[1].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1611));
            GroupView.Nodes[4].Nodes[1].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1122));
            GroupView.Nodes[4].Nodes[1].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1130));
            GroupView.Nodes[4].Nodes[1].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1134));
            GroupView.Nodes[4].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[4].Nodes[1].Nodes[1].Nodes.AddRange(PopulateSubsystem("Caldari"));
            GroupView.Nodes[4].Nodes[1].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1127));
            GroupView.Nodes[4].Nodes[1].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1630));
            GroupView.Nodes[4].Nodes[1].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1123));
            GroupView.Nodes[4].Nodes[1].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1131));
            GroupView.Nodes[4].Nodes[1].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1135));
            GroupView.Nodes[4].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[4].Nodes[1].Nodes[2].Nodes.AddRange(PopulateSubsystem("Gallente"));
            GroupView.Nodes[4].Nodes[1].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1129));
            GroupView.Nodes[4].Nodes[1].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1628));
            GroupView.Nodes[4].Nodes[1].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1124));
            GroupView.Nodes[4].Nodes[1].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1132));
            GroupView.Nodes[4].Nodes[1].Nodes[2].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1136));
            GroupView.Nodes[4].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[4].Nodes[1].Nodes[3].Nodes.AddRange(PopulateSubsystem("Minmatar"));
            GroupView.Nodes[4].Nodes[1].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1128));
            GroupView.Nodes[4].Nodes[1].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1629));
            GroupView.Nodes[4].Nodes[1].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1125));
            GroupView.Nodes[4].Nodes[1].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1133));
            GroupView.Nodes[4].Nodes[1].Nodes[3].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1137));
            #endregion
            #endregion
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
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(471));
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(472));
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(473));

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
            GroupView.Nodes[5].Nodes[3].Nodes.Add("Advanced Cruisers");
            GroupView.Nodes[5].Nodes[3].Nodes.Add("Faction Cruisers");
            GroupView.Nodes[5].Nodes[3].Nodes.Add("Standard Cruisers");

            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(74));
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(75));
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(76));
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[3].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(73));

            GroupView.Nodes[5].Nodes[3].Nodes[1].Nodes.Add("Navy");
            GroupView.Nodes[5].Nodes[3].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(75));
            GroupView.Nodes[5].Nodes[3].Nodes[1].Nodes.Add("Pirate");
            GroupView.Nodes[5].Nodes[3].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1371));

            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes.Add("Heavy Assault Cruisers");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(449));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(450));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(451));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(452));

            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes.Add("Heavy Interdiction Cruisers");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1071));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1072));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1073));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1074));

            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes.Add("Logistics");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(438));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(439));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(440));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(441));

            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes.Add("Recon Ships");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(827));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(830));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(833));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(836));

            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes.Add("Strategic Cruisers");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1139));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1140));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1141));
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[3].Nodes[0].Nodes[4].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1142));
            #endregion
            #region Destroyers
            GroupView.Nodes[5].Nodes[4].Nodes.Add("Advanced Destroyers");
            GroupView.Nodes[5].Nodes[4].Nodes.Add("Standard Destroyers");

            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(465));
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(466));
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(467));
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[4].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(468));

            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes.Add("Interdictors");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(826));
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(829));
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(832));
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(835));

            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes.Add("Tacticle Destroyers");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1952));
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(2021));
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(2034));
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[4].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1953));
            #endregion
            #region Frigates
            GroupView.Nodes[5].Nodes[5].Nodes.Add("Advanced Battlecruisers");
            GroupView.Nodes[5].Nodes[5].Nodes.Add("Faction Battlecruisers");
            GroupView.Nodes[5].Nodes[5].Nodes.Add("Standard Battlecruisers");

            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(72));
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(61));
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(77));
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(64));
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes.Add("ORE");
            GroupView.Nodes[5].Nodes[5].Nodes[2].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1616));

            GroupView.Nodes[5].Nodes[5].Nodes[1].Nodes.Add("Navy");
            GroupView.Nodes[5].Nodes[5].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1366));
            GroupView.Nodes[5].Nodes[5].Nodes[1].Nodes.Add("Pirate");
            GroupView.Nodes[5].Nodes[5].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1365));

            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes.Add("Assult Frigates");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(433));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(434));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(435));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(436));

            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes.Add("Covert Ops");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(421));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(422));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(423));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(424));

            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes.Add("Electronic Attack Frigate");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1066));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1067));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1068));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1069));

            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes.Add("Interceptors");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes[0].Nodes.AddRange(PopulateTreeCategory(400));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes[1].Nodes.AddRange(PopulateTreeCategory(401));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes[2].Nodes.AddRange(PopulateTreeCategory(402));
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[3].Nodes[3].Nodes.AddRange(PopulateTreeCategory(103));

            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes.Add("Expedition Frigates");
            GroupView.Nodes[5].Nodes[5].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1924));
            #endregion
            #region IndustrualShips
            GroupView.Nodes[5].Nodes[6].Nodes.Add("Advanced Industrial Ships");
            GroupView.Nodes[5].Nodes[6].Nodes.Add("Standard Industrial Ships");

            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes[0].Nodes.AddRange(PopulateTreeCategory(85));
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(84));
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(83));
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(82));
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes.Add("ORE");
            GroupView.Nodes[5].Nodes[6].Nodes[1].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1390));

            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes.Add("Transport Ships");
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Amarr");
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(630));
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Caldari");
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(631));
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Gallente");
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(632));
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Minmatar");
            GroupView.Nodes[5].Nodes[6].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(633));
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
            #region Structures
            GroupView.Nodes[6].Nodes.Add("Deployable Structures");
            GroupView.Nodes[6].Nodes.Add("Sovereignty Structures");
            GroupView.Nodes[6].Nodes.Add("Starbase Structures");

            GroupView.Nodes[6].Nodes[0].Nodes.Add("Cargo Containers");
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Audit Log Containers");
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1652));
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Fright Containers");
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1653));
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Sercure Containers");
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1651));
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Standard Containers");
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1657));
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes.Add("Station Containers");
            GroupView.Nodes[6].Nodes[0].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1658));

            GroupView.Nodes[6].Nodes[0].Nodes.Add("Encounter Surveillance System");
            GroupView.Nodes[6].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1847));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Cynosural Inhibitors");
            GroupView.Nodes[6].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1832));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Depots");
            GroupView.Nodes[6].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1831));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Micro Jump Units");
            GroupView.Nodes[6].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1844));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Scan Inhibitors");
            GroupView.Nodes[6].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(1845));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Siphon Units");
            GroupView.Nodes[6].Nodes[0].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1835));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Tractor Units");
            GroupView.Nodes[6].Nodes[0].Nodes[6].Nodes.AddRange(PopulateTreeCategory(1835));
            GroupView.Nodes[6].Nodes[0].Nodes.Add("Mobile Disruption Fields");
            GroupView.Nodes[6].Nodes[0].Nodes[7].Nodes.AddRange(PopulateTreeCategory(405));

            GroupView.Nodes[6].Nodes[1].Nodes.Add("Infastructure Upgrades");
            GroupView.Nodes[6].Nodes[1].Nodes[0].Nodes.Add("Industrial Upgrades");
            GroupView.Nodes[6].Nodes[1].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(1283));
            GroupView.Nodes[6].Nodes[1].Nodes[0].Nodes.Add("Military Upgrades");
            GroupView.Nodes[6].Nodes[1].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1284));
            GroupView.Nodes[6].Nodes[1].Nodes[0].Nodes.Add("Strategic Upgrades");
            GroupView.Nodes[6].Nodes[1].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1282));
            GroupView.Nodes[6].Nodes[1].Nodes.Add("Infastructure Hubs");
            GroupView.Nodes[6].Nodes[1].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1275));
            GroupView.Nodes[6].Nodes[1].Nodes.Add("Sovereighty Blockade Unit");
            GroupView.Nodes[6].Nodes[1].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1274));
            GroupView.Nodes[6].Nodes[1].Nodes.Add("Territorial Claim Unit");
            GroupView.Nodes[6].Nodes[1].Nodes[3].Nodes.AddRange(PopulateTreeCategory(1273));

            GroupView.Nodes[6].Nodes[2].Nodes.Add("Weapon Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes.Add("Electronic Warefare Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes[0].Nodes.AddRange(PopulateTreeCategory(481));
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes.Add("Energy Neutralization Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes[1].Nodes.AddRange(PopulateTreeCategory(1009));
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes.Add("Hybrid Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes[0].Nodes[2].Nodes.AddRange(PopulateTreeCategory(595));
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes.Add("Laser Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes[3].Nodes.AddRange(PopulateTreeCategory(596));
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes.Add("Missile Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes[4].Nodes.AddRange(PopulateTreeCategory(479));
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes.Add("Projtile  Batteries");
            GroupView.Nodes[6].Nodes[2].Nodes[0].Nodes[5].Nodes.AddRange(PopulateTreeCategory(594));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Assembly Arrays");
            GroupView.Nodes[6].Nodes[2].Nodes[1].Nodes.AddRange(PopulateTreeCategory(932));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Compression Array");
            GroupView.Nodes[6].Nodes[2].Nodes[2].Nodes.AddRange(PopulateTreeCategory(1921));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Control Towers");
            GroupView.Nodes[6].Nodes[2].Nodes[3].Nodes.AddRange(PopulateTreeCategory(478));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Corporate Hanger Array");
            GroupView.Nodes[6].Nodes[2].Nodes[4].Nodes.AddRange(PopulateTreeCategory(506));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Cynosural Generator Array");
            GroupView.Nodes[6].Nodes[2].Nodes[5].Nodes.AddRange(PopulateTreeCategory(1013));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Cynosural System Jammer");
            GroupView.Nodes[6].Nodes[2].Nodes[6].Nodes.AddRange(PopulateTreeCategory(1012));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Jump Bridge");
            GroupView.Nodes[6].Nodes[2].Nodes[7].Nodes.AddRange(PopulateTreeCategory(1011));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Laboratory");
            GroupView.Nodes[6].Nodes[2].Nodes[8].Nodes.AddRange(PopulateTreeCategory(933));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Moon Harvesting Arrays");
            GroupView.Nodes[6].Nodes[2].Nodes[9].Nodes.AddRange(PopulateTreeCategory(488));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Personal Hanger Arrays");
            GroupView.Nodes[6].Nodes[2].Nodes[10].Nodes.AddRange(PopulateTreeCategory(1702));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Reactors");
            GroupView.Nodes[6].Nodes[2].Nodes[11].Nodes.AddRange(PopulateTreeCategory(490));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Reprocessing Arrays");
            GroupView.Nodes[6].Nodes[2].Nodes[12].Nodes.AddRange(PopulateTreeCategory(482));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Shield Hardening Arrays");
            GroupView.Nodes[6].Nodes[2].Nodes[13].Nodes.AddRange(PopulateTreeCategory(485));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Ship Maintenance Arrays");
            GroupView.Nodes[6].Nodes[2].Nodes[14].Nodes.AddRange(PopulateTreeCategory(484));
            GroupView.Nodes[6].Nodes[2].Nodes.Add("Silos");
            GroupView.Nodes[6].Nodes[2].Nodes[15].Nodes.AddRange(PopulateTreeCategory(483));
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

            if (output[0] == null)
            {
                output[0] = new TreeNode("Incorrect DATA");
                System.Diagnostics.Debug.WriteLine("Market Category Search FAIL: " + marketID);
            }

            return output;
        }

        TreeNode[] PopulateSub(int mode)
        {
            if (mode != 0 && mode != 1 &&
                mode != 2 && mode != 3 &&
                mode != 4 && mode != 4 &&
                mode != 5 && mode != 6 &&
                mode != 7)
            {
                TreeNode[] fail = new TreeNode[1];
                fail[0] = new TreeNode("Incorrect DATA");
                System.Diagnostics.Debug.WriteLine("Unexpected Sub No: " + mode);
                return fail;
            }

            TreeNode[] output = new TreeNode[1];
            output[0] = new TreeNode("Incorrect DATA");
            if (mode == 0) //large, medium, small
            {
                output = new TreeNode[3];
                output[0] = new TreeNode("Large");
                output[1] = new TreeNode("Medium");
                output[2] = new TreeNode("Small");
            }
            else if (mode == 1) //extra large, large, medium, small
            {
                output = new TreeNode[4];
                output[0] = new TreeNode("Extra Large");
                output[1] = new TreeNode("Large");
                output[2] = new TreeNode("Medium");
                output[3] = new TreeNode("Small");
            }
            else if (mode == 2) //large, medium, micro, small
            {
                output = new TreeNode[4];
                output[0] = new TreeNode("Large");
                output[1] = new TreeNode("Medium");
                output[2] = new TreeNode("Micro");
                output[3] = new TreeNode("Small");
            }
            else if (mode == 3) //heavy, medium, small
            {
                output = new TreeNode[3];
                output[0] = new TreeNode("Heavy");
                output[1] = new TreeNode("Medium");
                output[2] = new TreeNode("Small");
            }
            else if (mode == 4) //Capital, Large, Medium, small
            {
                output = new TreeNode[4];
                output[0] = new TreeNode("Capital");
                output[1] = new TreeNode("Large");
                output[2] = new TreeNode("Medium");
                output[3] = new TreeNode("Small");
            }
            else if (mode == 5) //heavy, medium, small
            {
                output = new TreeNode[4];
                output[0] = new TreeNode("Heavy");
                output[1] = new TreeNode("Medium");
                output[2] = new TreeNode("Micro");
                output[3] = new TreeNode("Small");
            }
            else if (mode == 6) //Capital, large, medium, micro, small
            {
                output = new TreeNode[5];
                output[0] = new TreeNode("Capital");
                output[1] = new TreeNode("Large");
                output[2] = new TreeNode("Medium");
                output[3] = new TreeNode("Micro");
                output[4] = new TreeNode("Small");
            }
            else if (mode == 7) //Capital, extra large, large, medium, small
            {
                output = new TreeNode[5];
                output[0] = new TreeNode("Capital");
                output[1] = new TreeNode("Extra Large");
                output[2] = new TreeNode("Large");
                output[3] = new TreeNode("Medium");
                output[4] = new TreeNode("Small");
            }

            return output;
        }

        TreeNode[] PopulateSubsystem(string text)
        {
            if(text == "" || text == null)
            {
                TreeNode[] fail = new TreeNode[1];
                fail[0] = new TreeNode("Incorrect DATA");
                return fail;
            }

            TreeNode[] output = new TreeNode[5];
            output[0] = new TreeNode(text + " Defensive Subsystem");
            output[1] = new TreeNode(text + " Electronic Subsystem");
            output[2] = new TreeNode(text + " Engineering Subsystem");
            output[3] = new TreeNode(text + " Offensive Subsystem");
            output[4] = new TreeNode(text + " Propulsion Subsystem");

            return output;
        }

        TreeNode[] PopulateRigs(string type)
        {
            if (type == "" || type == null)
            {
                TreeNode[] fail = new TreeNode[1];
                fail[0] = new TreeNode("Incorrect DATA");
                return fail;
            }

            TreeNode[] output = new TreeNode[4];
            output[0] = new TreeNode("Capital " + type + " Rigs");
            output[1] = new TreeNode("Large " + type + " Rigs");
            output[2] = new TreeNode("Medium " + type + " Rigs");
            output[3] = new TreeNode("Small " + type + " Rigs");

            return output;
        }

        public void populateShoppingCart(Item item)
        {
            //update shopping list
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

        public int[] GatherObjectsFromGroupView(TreeNode searchNode, int[] itemSearch, bool ignoreFaction)
        {
            //firgure out if there is a faction item
            if (searchNode.Text == "Pirate" && ignoreFaction == true ||
                searchNode.Text == "Navy" && ignoreFaction == true)
            {
                return itemSearch;
            }

            int[] output = itemSearch;
            if (searchNode.Nodes.Count == 0)
            {
                //TreeNode must be an item
                int[] temp = new int[itemSearch.Length + 1];
                for (int p = 0; p < temp.Length - 1; ++p)
                {
                    temp[p] = output[p];
                }
                output = temp;

                string name = searchNode.Text;
                output[output.Length - 1] = NametoItemIndex(name);

            }
            else
            {
                //TreeNode must have some more items inside them
                for (int i = 0; i < searchNode.Nodes.Count; ++i)
                {
                    output = GatherObjectsFromGroupView(searchNode.Nodes[i], output, ignoreFaction);
                }
            }
            return output;
        }

        public DataTable GroupComparison(int[] items, int ME)
        {
            string[] tableNames = new string[5];
            for (int i = 0; i < 5; ++i)
            {
                if (stationNames[i] != "" && stationNames[i] != null)
                {
                    tableNames[i] = stationNames[i] + " profit";
                }
                else
                {
                    tableNames[i] = "Set station " + i;
                }
            }

            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add(tableNames[0], typeof(Int64));
            table.Columns.Add(tableNames[1], typeof(Int64));
            table.Columns.Add(tableNames[2], typeof(Int64));
            table.Columns.Add(tableNames[3], typeof(Int64));
            table.Columns.Add(tableNames[4], typeof(Int64));
            table.Columns.Add("Best Isk/Hr", typeof(Int64));
            table.Columns.Add("Investment/Profit", typeof(float));

            //work out the data for each item
            Int64[] value = new Int64[5],
                profit = new Int64[5];
            Int64 bestIskHr = new Int64();
            float bestRatio = new float();
            foreach (int item in items)
            {
                //get value and profit from each item
                for (int i = 0; i < 5; ++i)
                {
                    if (stationIds[i] != 0)
                    {
                        value[i] = getItemValue(this.items[item], ME, i);
                        profit[i] = this.items[item].getSellPrice(i) - value[i];
                    }
                }

                //best Isk/Hr
                float buildTime = ((this.items[item].getProdTime() / 60f) / 60f) * (float)(1 - (0.01 * TESlider.Value));
                for (int i = 0; i < 5; ++i)
                {
                    if (stationIds[i] != 0)
                    {
                        if ((profit[i] / buildTime) > bestIskHr)
                        {
                            bestIskHr = (Int64)(profit[i] / buildTime);
                        }
                    }
                }

                //best Investment to Profit Ratio
                for (int i = 0; i < 5; ++i)
                {
                    if (stationIds[i] != 0)
                    {
                        if (((profit[i] * 1.0f) / value[i]) > bestRatio)
                        {
                            bestRatio = ((profit[i] * 1.0f) / value[i]);
                        }
                    }
                }

                table.Rows.Add(this.items[item].getName(),
                  profit[0],
                  profit[1],
                  profit[2],
                  profit[3],
                  profit[4],
                  bestIskHr,
                  bestRatio);

                bestIskHr = 0;
                bestRatio = 0.0f;
            }

            return table;
        }
    }
}
