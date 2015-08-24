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
        int[] prodMatIds,
            skills;
        Int64[, ,] prodMatPrices;
        string[] prodMatNames,
            skillNames;
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

        /// <summary>
        /// Populate the ListAllItems list in GUI and find out that the names of items are
        /// </summary>
        private void populateItemList()
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

            prodMatIds = importer.YdnMatType(items);
            prodMatNames = importer.YdnNameFromID("StaticData/typeIDs.yaml", prodMatIds, "en");
            prodMatPrices = new Int64[5, prodMatIds.Length, 2];
            skills = importer.YdnGetAllSkills(items);
            skillNames = importer.YdnNameFromID("StaticData/typeIDs.yaml", skills, "en");

            itemSelectAll.DataSource = data;
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

                while (true)
                {
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt") + " Update cycle started");

                    ToolError.Text = "";
                    ToolProgLbl.Text = "Updating Material Data";
                    setProgress((int)progress);

                    //update station data
                    for (int l = 0; l < 5; ++l)
                    {
                        int upto = 0;
                        int[] search = new int[50];
                        string data = "";

                        try
                        {
                            progress = stationCount / (l * 100);
                        }
                        catch (DivideByZeroException)
                        {
                            progress = 0;
                        }
                        setProgress((int)progress);

                        while (upto != prodMatIds.Length - 1)
                        {
                            for (int i = 0; i <= 49 && upto != prodMatIds.Length - 1; ++i)
                            {
                                search[i] = prodMatIds[upto];
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
                        Int64[,] dataCheck = eveBotInterface.extractPrice(data, prodMatIds);
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
                                    prodMatPrices[l, i, m] = dataCheck[i, m];
                                }
                            }
                        }
                    }

                    //update item data
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt") + " Starting Item Update");
                    ToolProgLbl.Text = "Updating Item Data";
                    progress = 0;
                    setProgress((int)progress);

                    for (int l = 0; l < 5 && stationIds[l] != 0; ++l)
                    {
                        int upto = 0;
                        int[] search = new int[50];
                        string data = "";
                        int loadChuck = 50;

                        if (l != 0)
                        {
                            progress = stationCount / (l * 100);
                        }
                        else
                        {
                            progress = 0;
                        }

                        setProgress((int)progress);
                        int division = (items.Length - 1) / loadChuck;

                        while (upto != items.Length - 1)
                        {
                            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt") +
                            " Item Update Station:" + l + " of 4, itemCount: " + upto);

                            progress += ( 100.0f / stationCount) / division;
                            setProgress((int) progress);

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
                                data = eveBotInterface.getWebData(stationIds[l], search);
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
                                    items[i].setSellPrice(l, dataCheck[i, m]);
                                    //prodMatPrices[l, i, m] = dataCheck[i, m];
                                }
                            }
                        }


                    }
                    ToolProgLbl.Text = "Data Updated";
                    setProgress(0);
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss tt") + " Update cycle completed");
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
            if ( ToolProgress.GetCurrentParent().InvokeRequired ){
                ToolProgress.GetCurrentParent().Invoke(new MethodInvoker(delegate {
                    ToolProgress.Value = value; 
                }));
            }
        }

        private void itemSelectAll_SelectedIndexChanged(object sender, EventArgs e)
        {
            WorkOutData();
        }

        private void WorkOutData()
        {
            //update labels
            MEL.Text = "TE Level: " + MESlider.Value;
            TEL.Text = "TE Level: " + TESlider.Value;

            //work out the material costs
            ItemProporties();

            //work out the profitability
            DisplayName.Text = itemSelectAll.SelectedItem.ToString();
            DisplayType.Text = (NametoItemIndex(itemSelectAll.SelectedItem.ToString())).ToString();
            DisplayBType.Text = NametoBlueprintID(itemSelectAll.SelectedItem.ToString());

            Profit();
        }

        private void Profit()
        {
            //create a datatable and setup
            DataTable table = new DataTable();
            table.Columns.Add("Station", typeof(string));
            table.Columns.Add("Build Cost", typeof(string));
            table.Columns.Add("Item Cost", typeof(string));
            table.Columns.Add("Proft?", typeof(string));
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
                    quantity = (int)((value * (1 - (0.01 * MESlider.Value))) + 0.5f);
                    second = true;
                }
                else if (second == true)
                {
                    //populate the next row
                    //item in productMats
                    string name = "";
                    bool found2 = false;
                    for (int i = 0; i < prodMatNames.Length - 1 && found2 == false; ++i)
                    {
                        if (prodMatIds[i] == value)
                        {
                            name = prodMatNames[i];
                            for (int k = 0; k < 5; ++k)
                            {
                                stationPrice[k] += prodMatPrices[k, i, 1] * quantity;
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
                            if (items[i].getName() == name)
                            {

                            }
                        }
                    }
                    second = false;
                }
            }
            //populate the table
            string[] stationBuild = new string[5];
            for (int i = 0; i < 5; ++i)
            {
                stationBuild[i] = format(stationPrice[i].ToString());
            }
            for (int s = 0; s < 5; ++s)
            {
                if (stationIds[s] != 0)
                {
                    //Int64[] itemCost = eveCentral.extractPrice(eveCentral.getWebData(stationIds[s], current.getTypeID()), current.getTypeID());
                    //Int64 profit = itemCost[1] - stationPrice[s];

                    Int64 itemCost = current.getSellPrice(s);
                    Int64 profit = (itemCost - stationPrice[s]) / current.getProdQty();
                    float buildTime = ((current.getProdTime() / 60f) / 60f) * (float)(1 - (0.01 * TESlider.Value));
                    Int64 iskHr = (Int64)(profit / buildTime);

                    if (stationPrice[s] != 0)
                    {
                        table.Rows.Add(stationNames[s],
                            stationBuild[s],
                            format(itemCost.ToString()),
                            format(profit.ToString()),
                            format(iskHr.ToString()),
                            (profit * 1.0f) / stationPrice[s]);
                    }
                    else
                    {
                        //no station price data, don't bother with staion specific stuff
                        table.Rows.Add(stationNames[s],
                            stationBuild[s],
                            format((itemCost).ToString()),
                            format((profit).ToString()),
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
                    quantity = (int)((value * (1 - (0.01 * MESlider.Value))) + 0.5f);
                    second = true;
                }
                else if (second == true)
                {
                    //populate the next row
                    string name = "";
                    bool found2 = false;
                    for (int i = 0; i < prodMatNames.Length - 1 && found2 == false; ++i)
                    {
                        if (prodMatIds[i] == value)
                        {
                            name = prodMatNames[i];
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

                        }
                    }


                    second = false;
                }
            }

            //put the data into the table so the user can see it
            ManufacturingTable.DataSource = new DataTable();
            ManufacturingTable.DataSource = table;
        }

        private void AdvanceMaterialsStart()
        {
            //reset all controls to standared level
            int defaultMELevel = 10,
                defaultTELevel = 20;

            Name1.Visible = false;
            Name2.Visible = false;
            Name3.Visible = false;
            Name4.Visible = false;
            Name5.Visible = false;
            Name6.Visible = false;
            Name7.Visible = false;
            Name8.Visible = false;
            Name9.Visible = false;
            Name10.Visible = false;
            Name11.Visible = false;
            Name12.Visible = false;

            ME1.Value = defaultMELevel;
            ME2.Value = defaultMELevel;
            ME3.Value = defaultMELevel;
            ME4.Value = defaultMELevel;
            ME5.Value = defaultMELevel;
            ME6.Value = defaultMELevel;
            ME7.Value = defaultMELevel;
            ME8.Value = defaultMELevel;
            ME9.Value = defaultMELevel;
            ME10.Value = defaultMELevel;
            ME11.Value = defaultMELevel;
            ME12.Value = defaultMELevel;

            TE1.Value = defaultTELevel;
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            //if text is greater than 3 search for item which include those letters
            if (!(searchBox.Text.Length >= 3))
            {
                itemSelectAll.DataSource = listItems;
                return;
            }

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
            populateItemList();

            //Start eve central thread for grabbing eve data periodialy
            eveCentralBot = new Thread(EveThread);
            eveCentralBot.Name = "eveCentralBot";
            eveCentralBot.Start();
        }

        private void MainWindow_Close(object sender, System.ComponentModel.CancelEventArgs e)
        {
            eveCentralBot.Abort();
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

            updateOnStartup = false;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options optionsForm = new Options(stationNames, stationIds, updateOnStartup, updateInterval);
            optionsForm.ShowDialog();
        }

        private string cost(int typeID, int stationNo, Int64 qty)
        {
            Int64 numValue = prodMatPrices[stationNo, typeID, 1] * qty;
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

        private void MESlider_Scroll(object sender, EventArgs e)
        {
            WorkOutData();
        }

        private int NametoItemIndex(string name)
        {
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

        private void TESlider_Scroll(object sender, EventArgs e)
        {
            WorkOutData();
        }
    }
}