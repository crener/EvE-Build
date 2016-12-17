using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using EvE_Build;
using System.Data;

namespace EvE_Build_UI
{
    public partial class MainWindow : Form
    {
        Calculation workhorse = new Calculation();

        Thread littleMinion;

        public MainWindow()
        {
            InitializeComponent();

            workhorse = new Calculation(itemSelectAll,
        RunsToPay,
        MaterialVolume,
        MESlider,
        TESlider,
        ToolProgress,
        ToolProgLbl,
        ToolError,
        BaseMaterials,
        sellorBuyCheck,
        ManufacturingTable,
        ProfitView,
        ShoppingCart,
        RunSelect,
        littleMinion);
        }

        void UpdateManufacturing()
        {
            //figure out what item the player want too see

            int itemIndex = -1;

            //all item tab
            if (ItemTabs.SelectedIndex == 0 && itemSelectAll.Items.Count != 0)
            {
                itemIndex = workhorse.NametoItemIndex(itemSelectAll.SelectedItem.ToString());
                if (itemIndex == -1)
                {
                    //item doesn't exist
                    return;
                }
            }
            //group item tab
            else if (ItemTabs.SelectedIndex == 1 && GroupView.SelectedNode != null)
            {
                if (GroupView.SelectedNode.Nodes.Count == 0)
                {
                    itemIndex = workhorse.NametoItemIndex(GroupView.SelectedNode.Text);
                }
                else if (GroupView.SelectedNode.Nodes.Count != 0 || GroupView.SelectedNode == null)
                {
                    //empty node selected
                    return;
                }
            }

            if (itemIndex == -1)
            {
                //item doesn't exist
                return;
            }

            //update labels
            MEL.Text = "ME Level: " + MESlider.Value;
            TEL.Text = "TE Level: " + TESlider.Value;
            ItemVolume.Text = (workhorse.Items[itemIndex].getVolume() *
                (Convert.ToInt32(RunSelect.Value) * workhorse.Items[itemIndex].getProdQty())) + " m3";
            maxRuns.Text = "Maximum runs: " + workhorse.Items[itemIndex].getProdLmt();
            BpoCost.Text = workhorse.Format(workhorse.Items[itemIndex].getBlueprintPrice().ToString()) + " isk";

            DisplayName.Text = workhorse.Items[itemIndex].getName();
            DisplayType.Text = "ID" + workhorse.Items[itemIndex].getTypeID().ToString();
            DisplayBType.Text = "B" + workhorse.Items[itemIndex].getBlueprintTypeID().ToString();

            workhorse.WorkOutData(itemIndex);
        }

        private void itemSelectAll_SelectedIndexChanged(object sender, EventArgs e)
        {
            RunSelect.Value = 1;
            UpdateManufacturing();
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            //if text is greater than 3 search for item which include those letters
            if (!(searchBox.Text.Length >= 3))
            {
                itemSelectAll.DataSource = workhorse.listItems;
                return;
            }

            if (workhorse.Items == null)
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
            foreach (var item in workhorse.Items)
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
            littleMinion = new Thread(workhorse.PopulateItemList);
            littleMinion.Name = "LittleMinion - Populate item list";
            littleMinion.Start();

            workhorse.LittleMinionStart(littleMinion);
        }

        private void MainWindow_Close(object sender, System.ComponentModel.CancelEventArgs e)
        {
            workhorse.CloseThreads();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options optionsForm = new Options(workhorse.stationNames, workhorse.stationIds, workhorse.updateOnStartup, workhorse.updateInterval);
            optionsForm.ShowDialog();

            //presumably there were changes made, so reloadd the settings
            workhorse.Settings();
        }

        private void MESlider_Scroll(object sender, EventArgs e)
        {
            UpdateManufacturing();
        }

        private void TESlider_Scroll(object sender, EventArgs e)
        {
            UpdateManufacturing();
        }

        private void OverviewStart_Click(object sender, EventArgs e)
        {
            string[] tableNames = new string[5];
            for (int i = 0; i < 5; ++i)
            {
                if (workhorse.stationNames[i] != "" && workhorse.stationNames[i] != null)
                {
                    tableNames[i] = workhorse.stationNames[i] + " profit";
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
            bool invalid = false;
            string[] factions = new string[47],
                rigs = new string[3];
            #region faction
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
            factions[46] = "CONCORD";

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
            factions[44] = "Digital";
            factions[45] = "Analog";
            #endregion
            #region rig
            //TODO figure out how to seperate rigs from all other items in overview,
            //posibly using market IDs or group IDs
            #endregion

            foreach (Item current in workhorse.Items)
            {
                //check for faction stuff
                if (faction && current.getName() != null)
                {
                    if (current.getFaction() != 0 &&// no faction
                        current.getFaction() != 500001 &&//caldari
                        current.getFaction() != 500003 &&//amarr
                        current.getFaction() != 500002 &&//minmatar
                        current.getFaction() != 500004)//gallente
                    {
                        invalid = true;
                    }

                    for (int i = 0; i < factions.Length && invalid == false; ++i)
                    {
                        if (current.getName().Contains(factions[i]))
                        {
                            invalid = true;
                        }
                    }
                }
                else if (OverviewRigs.Checked && current.getName() != null)
                {

                }

                if (invalid)
                {
                    invalid = false;
                    continue;
                }

                //ensure item isn't chinese
                if (current.getName() == "" || workhorse.CheckAlphanumeric(current.getName()) == false)
                {
                    continue;
                }

                for (int i = 0; i < 5; ++i)
                {
                    if (workhorse.stationIds[i] != 0)
                    {
                        value[i] = workhorse.getItemValue(current, (int)OverviewME.Value, i);
                        profit[i] = current.getSellPrice(i) - value[i];
                    }
                }

                //best Isk/Hr
                float buildTime = ((current.getProdTime() / 60f) / 60f) * (float)(1 - (0.01 * TESlider.Value));
                for (int i = 0; i < 5; ++i)
                {
                    if (workhorse.stationIds[i] != 0)
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
                    if (workhorse.stationIds[i] != 0)
                    {
                        if (((profit[i] * 1.0f) / value[i]) > bestRatio)
                        {
                            bestRatio = ((profit[i] * 1.0f) / value[i]);
                        }
                    }
                }

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
            if (GroupView.Nodes.Count == 0 && workhorse.Items != null)
            {
                GroupView = workhorse.SetupTreeView(ref GroupView);
                //GroupView.Refresh();
            }
        }

        private void sellorBuyCheck_CheckedChanged(object sender, EventArgs e)
        {
            UpdateManufacturing();
        }

        private void RunSelect_ValueChanged(object sender, EventArgs e)
        {
            UpdateManufacturing();
        }

        private void AddShoppingMaterials_Click(object sender, EventArgs e)
        {
            if (workhorse.Items == null || DisplayName.Text == "Loading items from file")
            {
                return;
            }
            workhorse.PopulateShoppingCart(workhorse.Items[workhorse.NametoItemIndex(DisplayName.Text)]);
        }

        private void ClearCart_Click(object sender, EventArgs e)
        {
            ShoppingCart.DataSource = new DataTable();
            workhorse.EmptyShoppingCart();
        }

        private void BaseMaterials_CheckedChanged(object sender, EventArgs e)
        {
            UpdateManufacturing();
        }

        private void GroupView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (GroupView.SelectedNode.Nodes.Count == 0)
            {
                RunSelect.Value = 1;
                UpdateManufacturing();
            }
            else
            {
                GridViewDataView.DataSource = workhorse.GroupComparison(
                    workhorse.GatherObjectsFromGroupView(GroupView.SelectedNode, new int[0], GroupViewFaction.Checked), 10);
            }
        }

        private void GroupViewFaction_CheckedChanged(object sender, EventArgs e)
        {
            if (GroupView.SelectedNode.Nodes.Count != 0 &&
                workhorse.Items != null)
            {
                GridViewDataView.DataSource = workhorse.GroupComparison(
                    workhorse.GatherObjectsFromGroupView(
                        GroupView.SelectedNode, new int[0], GroupViewFaction.Checked), 10);
            }
        }

        private void oreCalculatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            workhorse.CreateOre();
        }
    }
}