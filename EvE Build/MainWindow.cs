using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Net;

namespace EvE_Build
{
    public partial class MainWindow : Form
    {
        Item[] items;
        Thread eveCentralBot,
            littleMinion;

        Material[] prodMats;
        int[]  skills;
        string[]    skillNames;

        List<string> listItems;

        WebInterface eveCentral = new WebInterface();

        //application settings
        bool updateOnStartup = false;
        string[] stationNames = new string[5];
        int[] stationIds = new int[5];
        int updateInterval = 5;

        public MainWindow()
        {
            InitializeComponent();
            Settings();
        }

        private bool checkAlphanumeric(string text)
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
        
        private void populateItemList()
        {
            try
            {
                //create and populate items for the itemselector
                YAML importer = new YAML();
                items = importer.ImportData("StaticData/blueprints.Yaml", "StaticData/typeIDs.yaml");

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

        void Settings()
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

        private void WorkOutData()
        {
            if (items == null || itemSelectAll.Items.Count == 0)
            {
                return;
            }

            int itemIndex = NametoItemIndex(itemSelectAll.SelectedItem.ToString());

            //update labels
            MEL.Text = "ME Level: " + MESlider.Value;
            TEL.Text = "TE Level: " + TESlider.Value;

            //update limit value
            RunSelect.Maximum = items[itemIndex].getProdLmt();
            maxRuns.Text = "Maximum runs: " + items[itemIndex].getProdLmt();

            //work out the material costs
            ItemProporties();

            //work out the profitability
            DisplayName.Text = itemSelectAll.SelectedItem.ToString();
            DisplayType.Text = "ID" + items[itemIndex].getTypeID().ToString();
            DisplayBType.Text = "B" + NametoBlueprintID(itemSelectAll.SelectedItem.ToString());

            Profit();
        }

        private void Profit()
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

            Item current = new Item(0, 0);

            //figure out what the type ID of the item is
            int index = NametoItemIndex(itemSelectAll.Text.ToString());
            if (index != -1)
            {
                //get the item
                current = items[index];
            }
            else
            {
                //index doesn't exist
                return;
            }

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
                                //stationPrice[k] += prodMatPrices[k, i, 1] * quantity;
                                stationPrice[k] += prodMats[i].price[k,1] * quantity;
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
                                    stationPrice[k] += getItemValue(items[i], 10, k) * quantity;
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

        private void ItemProporties()
        {
            if (items == null || prodMats == null)
            {
                return;
            }

            Item current = new Item(0, 0);

            //figure out what the type ID of the item is
            int index = NametoItemIndex(itemSelectAll.Text.ToString());
            if (index != -1)
            {
                //get the item
                current = items[index];
            }
            else
            {
                //index doesn't exist
                return;
            }

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
                    //populate the next row
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
                            break;
                        }
                    }
                    if (found2 == false)
                    {
                        //item is not normal, and must be an item that is in the item list
                        for (int i = 0; i < items.Length - 1 && found2 == false; ++i)
                        {
                            if (items[i].getTypeID() == value)
                            {
                                table.Rows.Add(items[i].getName(), quantity,
                                    format((getItemValue(items[i], 10, 0) * quantity).ToString()),
                                format((getItemValue(items[i], 10, 1) * quantity).ToString()),
                                format((getItemValue(items[i], 10, 2) * quantity).ToString()),
                                format((getItemValue(items[i], 10, 3) * quantity).ToString()),
                                format((getItemValue(items[i], 10, 4) * quantity).ToString()));
                            }
                        }
                    }
                    second = false;
                }
            }

            //put the data into the table so the user can see it
            ManufacturingTable.DataSource = new DataTable();
            ManufacturingTable.DataSource = table;
        }

        private Int64 getItemValue(Item search, int ME, int stationIndex)
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
                            //cost += prodMatPrices[stationIndex, m, 1] * qty;
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

        private int NametoItemIndex(string name)
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

        private void SetupTreeView()
        {
            //populate the gruop tree view with items




        }

#region formStuff
        private void itemSelectAll_SelectedIndexChanged(object sender, EventArgs e)
        {
            RunSelect.Value = 1;
            WorkOutData();
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            //if text is greater than 3 search for item which include those letters
            if (!(searchBox.Text.Length >= 3))
            {
                itemSelectAll.DataSource = listItems;
                return;
            }

            if (items == null)
            {
                return;
            }

            RunSelect.Value = 1;

            //start searching
            List<string> searchResults = new List<string>();
            string searchLow = "",
                searchHigh = "";

            if (searchBox.Text != null)
            {
                searchLow = searchBox.Text.First().ToString().ToLower() + String.Join("", searchBox.Text.Skip(1));
                searchHigh = searchBox.Text.First().ToString().ToUpper() + String.Join("", searchBox.Text.Skip(1));
            }
            foreach (var item in items)
            {
                if (item.getName() != null && item.getName().IndexOf(searchLow) >= 0)
                {
                    searchResults.Add(item.getName());
                }
                else if (item.getName() != null && item.getName().IndexOf(searchHigh) >= 0)
                {
                    searchResults.Add(item.getName());
                }
            }

            searchResults.Sort();
            itemSelectAll.DataSource = searchResults;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            //start going though the yaml files to get data
            littleMinion = new Thread(populateItemList);
            littleMinion.Name = "LittleMinion - Populate item list";
            littleMinion.Start();
            //populateItemList();
        }

        private void MainWindow_Close(object sender, System.ComponentModel.CancelEventArgs e)
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

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options optionsForm = new Options(stationNames, stationIds, updateOnStartup, updateInterval);
            optionsForm.ShowDialog();

            //presumably there were changes made, so reloadd the settings
            Settings();
        }

        private void MESlider_Scroll(object sender, EventArgs e)
        {
            WorkOutData();
        }

        private void TESlider_Scroll(object sender, EventArgs e)
        {
            WorkOutData();
        }

        private void OverviewStart_Click(object sender, EventArgs e)
        {
            string[] tableNames = new string[5];
            for(int i = 0; i < 5; ++i){
                if(stationNames[i] != "" && stationNames[i] != null){
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

            Int64[] value = new Int64[5],
                profit = new Int64[5];
            Int64 bestIskHr = new Int64();
            float bestRatio = new float();
            bool faction = OverviewFaction.Checked;
            string[] factions = new string[44];
            bool invalid = false;

            factions[0] = "Navy";
            factions[1] = "Shadow";
            factions[2] = "ORE";
            factions[3] = "Syndicate";
            factions[4] = "Ammatar";
            factions[5] = "Dark Blood";
            factions[6] = "Imperial";
            factions[7] = "Khniad";
            factions[8] = "Sansha";
            factions[9] = "Motte";
            factions[10] = "Republic";
            factions[11] = "Guristas";
            factions[12] = "Black Eagle";
            factions[13] = "Sentient";
            factions[41] = "Sisters";
            factions[40] = "Domination";
            factions[42] = "Angel";

            factions[43] = "Compressed";

            factions[14] = "Deuce";
            factions[15] = "Legion";
            factions[16] = "Thukker";

            factions[17] = "Ahremen";
            factions[18] = "Brokara";
            factions[19] = "Chelm";
            factions[20] = "Draclira";
            factions[21] = "Makur";
            factions[22] = "Raysere";
            factions[23] = "Selynne";
            factions[24] = "Tairei";
            factions[25] = "Vizan";
            factions[26] = "Gotan";
            factions[27] = "Hakim";
            factions[28] = "Mizuro";
            factions[29] = "Tobias";
            factions[30] = "Brynn";
            factions[31] = "Cormack";
            factions[32] = "Estamel";
            factions[33] = "Kaikka";
            factions[34] = "Setele";
            factions[35] = "Thon";
            factions[36] = "Tuvan";
            factions[37] = "Vepas";
            factions[38] = "Unit";
            factions[39] = "Shaqil";

            foreach (Item current in items)
            {
                //check for faction stuff
                if (faction && current.getName() != null)
                {

                    for (int i = 0; i <= factions.Length - 1 && invalid == false; ++i)
                    {
                        if (current.getName().Contains(factions[i]))
                        {
                            invalid = true;
                        }
                    }
                }
                
                if (invalid)
                {
                    invalid = false;
                    continue;
                }

                //ensure item isn't chinese
                if (current.getName() == "" || checkAlphanumeric(current.getName()) == false )
                {
                    continue;
                }

                for (int i = 0; i < 5; ++i)
                {
                    if (stationIds[i] != 0)
                    {
                        value[i] = getItemValue(current, (int)OverviewME.Value, i);
                        profit[i] = current.getSellPrice(i) - value[i];
                    }
                }

                //best Isk/Hr
                float buildTime = ((current.getProdTime() / 60f) / 60f) * (float)(1 - (0.01 * TESlider.Value));
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

                //table.Rows.Add(current.getName(),
                //  format(profit[0].ToString()),
                //  format(profit[1].ToString()),
                //  format(profit[2].ToString()),
                //  format(profit[3].ToString()),
                //  format(profit[4].ToString()),
                //  format(bestIskHr.ToString()),
                //  bestRatio);

                table.Rows.Add(current.getName(),
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

            OverviewTable.DataSource = table;
        }

        private void tabPage2_Open(object sender, EventArgs e)
        {
            //format the treeview if it is the first time opeining it since program start
            if (GroupView.Nodes.Count == 0 )
            {
                SetupTreeView();
            }
        }

        private void sellorBuyCheck_CheckedChanged(object sender, EventArgs e)
        {
            WorkOutData();
        }

        private void RunSelect_ValueChanged(object sender, EventArgs e)
        {
            WorkOutData();
        }
#endregion
    }
}