﻿using System;
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

namespace EvE_Build
{
    public partial class MainWindow : Form
    {
        Item[] items;
        Thread eveCentralBot,
            littleMinion;
        int[] prodMatIds,
            skills;
        Int64[,] prodMatPrices;
        string[] prodMatNames,
            skillNames;
        List<string> listItems;

        //application settings
        bool updateOnStartup;
        string[] stationNames;
        int[] stationIds;
        int updateInterval = 1;

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
            prodMatPrices = new Int64[prodMatIds.Length, 5];
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
                        string temp = line.Remove( line.IndexOf(" ") + 1);
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

        private void itemSelectAll_SelectedIndexChanged(object sender, EventArgs e)
        {
            Item current = new Item(0,0);

            //figure out what the type ID of the item is
            bool found = false;
            for (int i = 0; found == false; ++i)
            {
                if (items[i].getName() == itemSelectAll.Text.ToString())
                {
                    current = items[i];
                    found = true;
                }
            }

            //update data
            DataTable table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add(stationNames[0], typeof(Int64));
            table.Columns.Add(stationNames[1], typeof(Int64));
            table.Columns.Add(stationNames[2], typeof(Int64));
            table.Columns.Add(stationNames[3], typeof(Int64));
            table.Columns.Add(stationNames[4], typeof(Int64));

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
                    quantity = value;
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

        //private DataTable fillTable(Item search)
        //{
        //    DataTable output = new DataTable();
        //    bool second = false;
        //    int quantity = 0;
        //    foreach (int value in search.getProdMats())
        //    {
        //        if (value == null || value == 0)
        //        {
        //            continue;
        //        }

        //        if (second == false)
        //        {
        //            quantity = value;
        //            second = true;
        //        }
        //        else if (second == true)
        //        {
        //            //populate the next row
        //            string name = "";
        //            bool found2 = false;
        //            for (int i = 0; i < prodMatNames.Length - 1 && found2 == false; ++i)
        //            {
        //                if (prodMatIds[i] == value)
        //                {
        //                    name = prodMatNames[i];
        //                    table.Rows.Add(name, quantity, cost(i, 0, quantity),
        //                        cost(i, 1, quantity), cost(i, 2, quantity),
        //                        cost(i, 3, quantity), cost(i, 4, quantity));
        //                    found2 = true;
        //                    break;
        //                }
        //            }
        //            if (found2 == false)
        //            {
        //                for (int i = 0; i < items.Length - 1 && found2 == false; ++i)
        //                {

        //                }
        //            }


        //            second = false;
        //        }
        //    }
        //}

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

        private Int64 cost(int typeID, int stationNo, Int64 qty)
        {
            return prodMatPrices[typeID, stationNo] * qty;
        }
    }
}
