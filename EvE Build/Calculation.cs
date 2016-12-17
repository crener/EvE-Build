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
        public Item[] Items;

        Material[] prodMats;
        int[] skills;
        string[] skillNames;

        Int64[,] shoppingList;
        public List<string> listItems;

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
        Thread littleMinion)
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
            Settings();
        }

        public bool CheckAlphanumeric(string text)
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

        public void LittleMinionStart(Thread thread)
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

        public void PopulateItemList()
        {
            try
            {
                //create and populate items for the itemselector
                YAML importer = new YAML();
                Items = importer.ImportData();

                List<string> data = new List<string>();
                string[] tmpStorage = new string[Items.Length];
                int i = 0;
                foreach (var item in Items)
                {
                    if (item.getName() != "" && CheckAlphanumeric(item.getName()))
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

                prodMats = importer.YdnMatTypeMat(Items);
                importer.extractMaterialNames(ref prodMats, "StaticData/typeIDs.yaml", "en");

                skills = importer.YdnGetAllSkills(Items);
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
            //the amount of items that are checked every web request
            int loadChuck = 50;

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
                    if (updateInterval <= 1)
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
                    int division = (Items.Length - 1) / loadChuck;

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
                        division = (Items.Length - 1) / loadChuck;

                        while (upto != Items.Length - 1)
                        {
                            progress += (100.0f / stationCount) / division;
                            setProgress((int)progress);

                            for (int i = 0; i <= loadChuck - 1 && upto != Items.Length - 1; ++i)
                            {
                                search[i] = Items[upto].getTypeID();
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

                        int[] itemIDCollection = new int[Items.Length - 1];
                        for (int i = 0; i < itemIDCollection.Length; ++i)
                        {
                            itemIDCollection[i] = Items[i].getTypeID();
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
                                        Items[i].setBuyPrice(l, dataCheck[i, m]);
                                    }
                                    else if (m == 1)
                                    {
                                        Items[i].setSellPrice(l, dataCheck[i, m]);
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
            if (Items == null)
            {
                return;
            }

            //work out the material costs
            ItemProporties(itemIndex);

            //fill in runs needed until BPO pays for itself
            Int64 bestPrice = 0;
            int station = 0;
            Item item = Items[itemIndex];
            for (int i = 0; i < 5; ++i)
            {
                if (stationIds[i] != 0)
                {
                    Int64 itemCost = getItemValue(Items[itemIndex],
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

            Item current = Items[itemIndex];

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
                        for (int i = 0; i < Items.Length - 1 && found2 == false; ++i)
                        {
                            if (Items[i].getTypeID() == value)
                            {
                                for (int k = 0; k < 5; ++k)
                                {
                                    stationPrice[k] += getItemValue(Items[i], 10, k) * quantity;
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
                stationBuild[i] = Format(subPrice2.ToString());
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
                            Format(current.getSellPrice(s).ToString()),
                            Format((sellProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            Format((buyProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            Format(iskHr.ToString()),
                            iskInv);
                    }
                    else
                    {
                        //no station price data, don't bother with staion specific stuff
                        table.Rows.Add(stationNames[s],
                            stationBuild[s],
                            Format(current.getSellPrice(s).ToString()),
                            Format((sellProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
                            Format((buyProfit * Convert.ToInt32(RunSelect.Value)).ToString()),
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
            if (Items == null || prodMats == null)
            {
                return;
            }

            Item current = Items[itemIndex];
            float volume = 0f;
            
            //update data
            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add(stationNames[0], typeof(string));
            table.Columns.Add(stationNames[1], typeof(string));
            table.Columns.Add(stationNames[2], typeof(string));
            table.Columns.Add(stationNames[3], typeof(string));
            table.Columns.Add(stationNames[4], typeof(string));

            #region Component based price
            if (BaseMaterials.Checked == false)
            {
                //Get the values based on the components of the item being made
                bool second = false;
                int quantity = 0;
                
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
                                table.Rows.Add(name, quantity, Cost(i, 0, quantity),
                                    Cost(i, 1, quantity), Cost(i, 2, quantity),
                                    Cost(i, 3, quantity), Cost(i, 4, quantity));
                                found2 = true;

                                volume += prodMats[i].volume * quantity;
                            }
                        }
                        if (found2 == false)
                        {
                            //item is not normal, and must be an item that is in the item list
                            for (int i = 0; i < Items.Length - 1 && found2 == false; ++i)
                            {
                                if (Items[i].getTypeID() == value)
                                {
                                    table.Rows.Add(Items[i].getName(), quantity,
                                            Format((getItemValue(Items[i], 10, 0) * quantity).ToString()),
                                        Format((getItemValue(Items[i], 10, 1) * quantity).ToString()),
                                        Format((getItemValue(Items[i], 10, 2) * quantity).ToString()),
                                        Format((getItemValue(Items[i], 10, 3) * quantity).ToString()),
                                        Format((getItemValue(Items[i], 10, 4) * quantity).ToString()));

                                    volume += (Items[i].getVolume() * quantity);
                                }
                            }
                        }
                        second = false;
                    }

                    //material volume
                    MaterialVolume.Text = volume + " m3";
                }
            }
            #endregion
            #region Base level Mineral based price
            else
            {
                //get the values of the basic materials that make up the item being made
                //tritanium, pyritem, T2 stuff etc.

                long[,] materialList = new long[1, 2],
                    materials = current.getProdMats();

                materialList = getItemMaterials(current);

                for (int i = 0; i < (materialList.Length - 1) / 2; i++)
                {
                    //populate the next row of the table
                    string name = "";
                    Int64 quantity = CorrectRounding(materialList[i, 0] * (1 - (0.01f * MESlider.Value))) * Convert.ToInt32(RunSelect.Value);
                    bool found2 = false;
                    for (int u = 0; u < prodMats.Length - 1 && found2 == false; ++u)
                    {
                        if (prodMats[i].ID == materialList[i,1])
                        {
                            name = prodMats[i].name;
                            table.Rows.Add(name, quantity, Cost(i, 0, quantity),
                                Cost(i, 1, quantity), Cost(i, 2, quantity),
                                Cost(i, 3, quantity), Cost(i, 4, quantity));
                            found2 = true;

                            volume += prodMats[i].volume * quantity;
                        }
                    }
                    if (found2 == false)
                    {
                        //item is not normal, and must be an item that is in the item list
                        for (int r = 0; r < Items.Length - 1 && found2 == false; ++r)
                        {
                            if (Items[i].getTypeID() == materialList[i, 1])
                            {
                                table.Rows.Add(Items[i].getName(), quantity,
                                        Format((getItemValue(Items[i], 10, 0) * quantity).ToString()),
                                    Format((getItemValue(Items[i], 10, 1) * quantity).ToString()),
                                    Format((getItemValue(Items[i], 10, 2) * quantity).ToString()),
                                    Format((getItemValue(Items[i], 10, 3) * quantity).ToString()),
                                    Format((getItemValue(Items[i], 10, 4) * quantity).ToString()));

                                volume += (Items[i].getVolume() * quantity);
                            }
                        }
                    }
                }
            }
            #endregion

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

                        for (int m = 0; m < Items.Length - 1 && found == false; ++m)
                        {
                            if (Items[m].getTypeID() == id)
                            {
                                cost += getItemValue(Items[m], ME, stationIndex) * qty;
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
            if (Items == null || prodMats == null || typeID == 0)
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
                    cost = getItemValue(Items[TypeIDtoItemIndex(typeID)], 10, stationIndex) * qty;
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

        private string Cost(int typeIdIndex, int stationNo, Int64 qty)
        {
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

            return Format(output);
        }

        public string Format(string text)
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
            if (Items == null)
            {
                return 0;
            }

            bool found = false;
            for (int i = 0; found == false && i < Items.Length; ++i)
            {
                if (Items[i].getName() == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private int TypeIDtoItemIndex(int type)
        {
            if (Items == null)
            {
                return 0;
            }

            bool found = false;
            for (int i = 0; found == false && i < Items.Length; ++i)
            {
                if (Items[i].getTypeID() == type)
                {
                    return i;
                }
            }
            return -1;
        }

        public TreeView SetupTreeView(ref TreeView GroupView)
        {
            return GroupSetup.GenerateTreeView(ref Items, ref GroupView);
        }

        public void PopulateShoppingCart(Item item)
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
                    Format(price[0, i, 0].ToString()),
                    Format(price[1, i, 0].ToString()),
                    Format(price[2, i, 0].ToString()),
                    Format(price[3, i, 0].ToString()),
                    Format(price[4, i, 0].ToString()));
            }

            ShoppingCart.DataSource = shoppingCart;
        }

        public void EmptyShoppingCart()
        {
            shoppingList = null;
        }

        private string MaterialName(int typeID)
        {
            if (prodMats == null || Items == null)
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

            foreach (var name in Items)
            {
                if (name.getTypeID() == typeID)
                {
                    return name.getName();
                }
            }

            return " ";
        }

        private Int64[,] getItemMaterials(Item search)
        {
            //return the materials of a given item TypeID
            //This could be recuring 
            Int64[,] materials = search.getProdMats(),
                output = new Int64[materials.Length - 1, 2];
            bool found = false;

            for(int i = 0; i < (materials.Length / 2) - 1; ++i)
            {
                if (materials[i, 1] == 0) continue;
                Int64 searchItem = materials[i, 1];
                found = false;

                //search inside the materials
                for(int m = 0; m < (materials.Length / 2) - 1 && found == false; ++m)
                {
                    if(searchItem == prodMats[m].ID)
                    {
                        for(int y = 0; y < (output.Length / 2) - 1 && found == false; ++y)
                        {
                            if(output[y, 1] == 0)
                            {
                                output[y, 0] = materials[i, 0];
                                output[y, 1] = materials[i, 1];
                                found = true;
                            }
                        }
                    }
                }

                //break prematurly is the value has already been found
                if (found) continue;

                //search inside the items
                for (int m = 0; m < Items.Length - 1; ++m)
                {
                    if (searchItem == Items[m].getTypeID())
                    {
                        //call this method again to get the possible materials of that item,
                        //then merdge them together for 1 output
                        Item requirement = Items[TypeIDtoItemIndex((int)searchItem)];
                        Int64[,] temp = getItemMaterials(requirement);

                        output = MerdgeItemMaterials(output, temp);
                    }
                }
            }
            return output;
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
                        value[i] = getItemValue(this.Items[item], ME, i);
                        profit[i] = this.Items[item].getSellPrice(i) - value[i];
                    }
                }

                //best Isk/Hr
                float buildTime = ((this.Items[item].getProdTime() / 60f) / 60f) * (float)(1 - (0.01 * TESlider.Value));
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

                table.Rows.Add(this.Items[item].getName(),
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

        public void CreateOre()
        {
            Ore_Calculator window = new Ore_Calculator(stationIds, stationNames);
            window.Show();
        }
    }
}
