using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;
using EvE_Build_WPF.Code;
using EvE_Build_WPF.Code.Containers;

namespace EvE_Build_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ConcurrentDictionary<int, Item> items;
        private ConcurrentDictionary<int, MaterialItem> materials;
        private List<MarketItem> marketItems;
        private bool initDone;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SetupData(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!File.Exists(FileParser.MarketGroupFile))
            {
                //download the file and place it in the correct location
                try
                {
                    ManName.Content = "Downloading data (first time initialization)";
                    MessageBox.Show("EvE Build will perform a one time download of EvE online data to get the most up to date information. This is usually around 100MB in size.", "Downloading Content");

                    await Task.Run(() => UpdateChecker.DownloadEveFiles());
                    ManName.Content = "Loading items from file";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Updating", ex.Message);
                    return;
                }
            }

            FileParser fileParser = new FileParser();
            Task<Dictionary<int, Item>> blueprintData = Task<Dictionary<int, Item>>.Factory.StartNew(() => LoadItemBlueprintData(ref fileParser));
            Task<List<MarketItem>> marketData = Task<List<MarketItem>>.Factory.StartNew(FileParser.ParseMarketGroupData);

            Task[] jobs = new Task[3];
            jobs[0] = blueprintData;
            jobs[1] = marketData;
            jobs[2] = Task.Run(() => LoadSettings());

            await marketData;
            marketItems = marketData.Result;

            await Task.WhenAll(jobs);
            

            //move the results to a new dictionary (for thread safety)
            items = new ConcurrentDictionary<int, Item>();
            foreach (KeyValuePair<int, Item> item in blueprintData.Result)
                items.AddOrUpdate(item.Value.ProductId, item.Value, Item.Merdge);

            await Task.Run(() => materials = fileParser.GatherMaterials(items));

            new CentralThread(ref materials, ref items);
            CentralThread.stationDataUpdated += ThreadUpdateStationsInvoke;

            BuildTreeView();
            BuildAllItems();
            ManName.Content = "Select an item from the left";

            Task.Factory.StartNew(UpdateChecker.CheckForUpdates);
            initDone = true;
        }

        private void ThreadUpdateStationsInvoke(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(() => ThreadUpdateStations(sender, eventArgs));
        }

        private void ThreadUpdateStations(object sender, EventArgs eventArgs)
        {
            try
            {
                if (ManTypeId.Content.ToString() == "TypeID" || ManTypeId.Content.ToString() == "") return;
                int itemId = int.Parse(ManTypeId.Content.ToString());

                CalculatePrices(items[itemId]);
            }
            catch (InvalidOperationException) { }
        }

        private void BuildTreeView()
        {
            //create initial tree view items
            foreach (MarketItem mat in marketItems)
            {
                TreeViewItem viewItem = new TreeViewItem
                {
                    Header = mat.Name,
                    Tag = "market," + mat.MarketId,
                    ToolTip = mat.Description
                };

                mat.TreeViewObject = viewItem;
            }

            //link tree view items to get the correct hierarchy 
            foreach (MarketItem mat in marketItems)
            {
                if (mat.ParentGroupId != -1)
                {
                    MarketItem parent = marketItems.First(x => x.MarketId == mat.ParentGroupId);
                    parent.TreeViewObject.Items.Add(mat.TreeViewObject);
                }
            }

            //add Items to each market item
            foreach (KeyValuePair<int, Item> item in items)
            {
                try
                {
                    MarketItem group = marketItems.First(x => x.MarketId == item.Value.MarketGroupId);
                    if (group.Name.Contains("Faction")) item.Value.isFaction = true;
                    if (group.Name.Contains(" Rigs") || group.Name.Contains(" Modules")) item.Value.isRig = true;

                    TreeViewItem viewItem = new TreeViewItem
                    {
                        Tag = "item," + item.Value.ProductId,
                        Header = item.Value.ProductName
                    };

                    group.TreeViewObject.Items.Add(viewItem);
                    group.Used = true;

                    //Ensure that the market id and market category are not culled later
                    MarketItem search = marketItems.First(x => x.MarketId == group.ParentGroupId);
                    if (search != null && !search.Used)
                    {
                        while (!search.Used)
                        {
                            search.Used = true;
                            if (search.ParentGroupId == -1) break;

                            search = marketItems.First(x => x.MarketId == search.ParentGroupId);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
            }

            //Remove unused items
            for (int i = marketItems.Count - 1; i >= 0; i--)
            {
                if (marketItems[i].Used) continue;

                try
                {
                    MarketItem parent = marketItems.First(x => x.MarketId == marketItems[i].ParentGroupId);
                    if (parent != null) parent.TreeViewObject.Items.Remove(marketItems[i].TreeViewObject);
                }
                catch (InvalidOperationException)
                {
                    //ignore... the market item didn't have a parent
                }

                marketItems.RemoveAt(i);
            }

            //clear out all the market groups that have no items
            foreach (MarketItem item in marketItems)
            {
                if (item.ParentGroupId == -1 && item.Used) GroupView.Items.Add(item.TreeViewObject);
            }
        }

        private Dictionary<int, Item> LoadItemBlueprintData(ref FileParser fileParser)
        {
            //ensure directory exists
            string directory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "static";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            try
            {
                Dictionary<int, Item> blue = fileParser.ParseBlueprintData();
                fileParser.ParseItemDetails(ref blue);
                return blue;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("EvE Online data is Missing");
            }

            return null;
        }

        private void BuildAllItems()
        {
            SearchAllList.Items.Clear();
            List<Item> sortedItems = new List<Item>(items.Values);
            sortedItems.Sort();

            foreach (Item item in sortedItems)
            {
                if (item.ProductName.StartsWith("'") || item.ProductName.StartsWith("‘") || item.ProductName.StartsWith("R.A.M.") || item.ProductName.StartsWith("R.Db") || item.isFaction || item.isRig || item.isSubFaction || item.ProductName.StartsWith("Capital"))
                    continue;

                ListBoxItem listItem = new ListBoxItem
                {
                    Content = item.ProductName,
                    Tag = item.ProductId
                };

                SearchAllList.Items.Add(listItem);
            }
        }

        private void LoadSettings()
        {
            Settings.Load();
        }

        private void ChangeManufactureItem(int itemId)
        {
            Item item = items[itemId];

            ManName.Content = item.ProductName;
            ManTypeId.Content = item.ProductId;
            ManBlueType.Content = item.BlueprintId;

            ManBpoCost.Content = item.BlueprintBasePrice.ToString("N") + " isk";
            ManVolumeItem.Content = item.ProductVolume.ToString("N1") + " m3";

            CalculatePrices(item);
        }

        private void CalculatePrices(Item item)
        {
            int bestStation = CalculateTotals(item);
            CalculateMaterials(item, bestStation);

            //work out the bpo payback amount
            if (item.BuyPrice.ContainsKey(bestStation))
            {
                decimal buildCost = CalculateItemPrice(bestStation, item);

                decimal stationBuy = 0m;
                if (item.BuyPrice.Count > 0 && item.BuyPrice.ContainsKey(bestStation))
                    stationBuy = item.BuyPrice[bestStation];

                decimal stationSell = 0m;
                if (item.SellCost.Count > 0 && item.SellCost.ContainsKey(bestStation))
                    stationSell = item.SellCost[bestStation];

                decimal sellProfit = stationSell * item.ProductionQuantity - buildCost;
                decimal buyProfit = stationBuy * item.ProductionQuantity - buildCost;
                decimal bestPrice = sellProfit > buyProfit ? sellProfit : buyProfit;

                if (bestPrice <= 0) ManBpoRuns.Content = "Not Profitable";
                else
                {
                    int runs = (int)Math.Ceiling(item.BlueprintBasePrice / bestPrice);
                    ManBpoRuns.Content = runs + (runs == 1 ? " run in " : " runs in ") + Settings.SpecificStation(bestStation).StationName;
                }
            }
            else ManBpoRuns.Content = "Not enough data";
        }

        private void CalculateMaterials(Item item, int cheapStation)
        {
            ManRaw.Items.Clear();

            float me = 1f - (float)ManMe.Value / 100f;

            foreach (Material material in item.ProductMaterial)
            {
                long actualQty = (long)Math.Ceiling(material.Quantity * me);
                decimal cost = 0m;

                string name = "Not found!";
                bool advancedMaterial = false;
                if (materials.ContainsKey(material.Type))
                {
                    name = materials[material.Type].Name;
                    cost = cheapStation == 0 ? materials[material.Type].getPrice() * actualQty : materials[material.Type].getPrice(cheapStation) * actualQty;
                }
                else if (items.ContainsKey(material.Type))
                {
                    name = items[material.Type].ProductName;
                    cost = cheapStation == 0 ? CalculateItemPrice(item) : CalculateItemPrice(cheapStation, item);
                    advancedMaterial = true;
                }

                DataGridMaterial gridMaterial = new DataGridMaterial
                {
                    Name = name,
                    Quantity = actualQty,
                    Cost = cost.ToString("N")
                };

                if (advancedMaterial)
                {
                    //TODO figure out some recursion to get items to show up as children under this material
                }

                ManRaw.Items.Add(gridMaterial);
            }
        }

        /// <summary>
        /// calculates all values needed for update table
        /// </summary>
        /// <param name="item">the item that needs to be calculated</param>
        /// <returns>id of the highest margin system</returns>
        private int CalculateTotals(Item item)
        {
            ManProfit.Items.Clear();
            int cheapestId = 0;
            decimal bestMargin = decimal.MinValue;
            float te = 1f - (float)ManTe.Value / 100f;
            float buildTime = item.ProductionTime / 60f / 60f * te;

            foreach (Station station in Settings.Stations)
            {
                decimal buildCost = CalculateItemPrice(station.StationId, item);
                decimal stationBuy = item.BuyPrice.Count > 0 && item.BuyPrice.ContainsKey(station.StationId) ? item.BuyPrice[station.StationId] : 0m;
                decimal stationSell = item.SellCost.Count > 0 && item.SellCost.ContainsKey(station.StationId) ? item.SellCost[station.StationId] : 0m;

                decimal sellProfit = stationSell * item.ProductionQuantity - buildCost;
                decimal buyProfit = stationBuy * item.ProductionQuantity - buildCost;
                decimal bestPrice = sellProfit > buyProfit ? sellProfit : buyProfit;
                decimal hour = bestPrice / (decimal)buildTime;

                DataGridStation grid = new DataGridStation();
                grid.Name = station.StationName;
                grid.BuildCost = buildCost.ToString("N");
                grid.ItemCost = stationSell.ToString("N");
                grid.SellMargin = sellProfit.ToString("N");
                grid.BuyMargin = buyProfit.ToString("N");
                grid.IskHr = hour.ToString("N");

                if (bestPrice > bestMargin)
                {
                    cheapestId = station.StationId;
                    bestMargin = bestPrice;
                }

                ManProfit.Items.Add(grid);
            }

            return cheapestId;
        }

        private decimal CalculateItemPrice(int stationId, Item item)
        {
            decimal price = 0m;
            float me = 1f - (float)ManMe.Value / 100f;

            foreach (Material material in item.ProductMaterial)
            {
                int qty = (int)Math.Ceiling(material.Quantity * me);

                if (materials.ContainsKey(material.Type))
                {
                    price += materials[material.Type].getPrice(stationId) * qty;
                }
                else if (items.ContainsKey(material.Type))
                {
                    price += CalculateItemPrice(stationId, items[material.Type]) * qty;
                }
            }
            return price;
        }

        private decimal CalculateItemPrice(Item item)
        {
            decimal price = decimal.MaxValue;

            foreach (Station station in Settings.Stations)
            {
                decimal stationCost = CalculateItemPrice(station.StationId, item);
                if (stationCost < price) price = stationCost;
            }

            return price;
        }

        private void ResetDefaultState()
        {
            ManName.Content = "Select an item from the right";
            ManTypeId.Content = "TypeID";
            ManBlueType.Content = "BlueTypeID";

            ManBpoCost.Content = "0";
            ManBpoRuns.Content = "0";

            ManVolumeItem.Content = "0 m3";
            ManVolumeMaterial.Content = "0 m3";
        }

        private void SearchListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initDone) return;

            ListBox allItems = (ListBox)sender;
            if (allItems.SelectedItem == null) return;

            ListBoxItem selectedItem = (ListBoxItem)allItems.SelectedItem;
            if (selectedItem.Tag == null) return;

            int itemId = (int)selectedItem.Tag;
            if (!items.ContainsKey(itemId)) return;

            ChangeManufactureItem(itemId);
        }

        private void SearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ResetDefaultState();

            string searchTerm = ((TextBox)sender).Text.ToLower();
            if (string.IsNullOrEmpty(searchTerm))
            {
                BuildAllItems();
                return;
            }
            if (items.Count <= 0) return;

            List<KeyValuePair<int, Item>> results = new List<KeyValuePair<int, Item>>(
                items.Where(item => item.Value.ProductName.ToLower().Contains(searchTerm)));
            results.Sort(new Item());

            SearchAllList.Items.Clear();

            foreach (KeyValuePair<int, Item> item in results)
            {
                ListBoxItem listItem = new ListBoxItem
                {
                    Content = item.Value.ProductName,
                    Tag = item.Value.ProductId
                };

                SearchAllList.Items.Add(listItem);
            }
        }

        private void ParameterChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ManTypeId.Content.ToString() == "TypeID" || ManTypeId.Content.ToString() == "") return;
            int itemId = int.Parse(ManTypeId.Content.ToString());

            Item item = items[itemId];

            CalculatePrices(item);
        }

        private void GroupViewChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem groupItem = (TreeViewItem)GroupView.SelectedItem;
            if (groupItem == null || !groupItem.Tag.ToString().StartsWith("item,")) return;

            int itemId = int.Parse(groupItem.Tag.ToString().Substring(5));
            if (!items.ContainsKey(itemId)) return;

            ChangeManufactureItem(itemId);
        }
    }
}
