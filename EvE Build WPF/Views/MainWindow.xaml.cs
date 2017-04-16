using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Windows.Documents;
using EvE_Build_WPF.Code;
using EvE_Build_WPF.Code.Containers;
using YamlDotNet.Serialization.NodeDeserializers;

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
            Task<Dictionary<int, Item>> returnTask = Task<Dictionary<int, Item>>.Factory.StartNew(() => ItemDataAcquisition());

            Task[] jobs = new Task[3];
            jobs[0] = returnTask;
            jobs[1] = Task.Run(() => BuildMarketData());
            jobs[2] = Task.Run(() => LoadSettings());

            await Task.WhenAll(jobs);

            //move the results to a new dictionary (for thread safety)
            items = new ConcurrentDictionary<int, Item>();
            foreach (KeyValuePair<int, Item> item in returnTask.Result)
            {
                items.AddOrUpdate(item.Value.ProdId, item.Value, Item.Merdge);
            }

            BuildAllItems();
            ManName.Content = "Select an item from the right";

            await Task.Run(() => BuildMaterial());

            new CentralThread(ref materials, ref items);
            BuildTreeView();

            initDone = true;
        }

        private void BuildTreeView()
        {
            //create initial tree view items
            foreach (MarketItem mat in marketItems)
            {
                TreeViewItem viewItem = new TreeViewItem
                {
                    Header = mat.Name,
                    Tag = "market," + mat.MarketId
                };

                mat.TreeViewObject = viewItem;
                if (mat.ParentGroupId == -1) GroupView.Items.Add(viewItem);
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

                    TreeViewItem viewItem = new TreeViewItem
                    {
                        Tag = "item," + item.Key,
                        Header = item.Value.ProdName
                    };

                    group.TreeViewObject.Items.Add(viewItem);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
            }

            //clear out all the market groups that have no items
            /*foreach (MarketItem item in marketItems)
            {
                bool alive = item.TreeViewObject.Items.Count != 0;
                alive = alive && ((string) ((TreeViewItem) item.TreeViewObject.Items.GetItemAt(0)).Tag).Contains("market");

                if (!alive)
                {
                    if (item.ParentGroupId != -1) marketItems.First(x => x.MarketId == item.ParentGroupId).TreeViewObject.Items.Remove(item.TreeViewObject);
                    if (item.ParentGroupId == -1) GroupView.Items.Remove(item.TreeViewObject);

                    //clear out the reference so GC can grab it
                    item.TreeViewObject = null;
                }
            }*/
        }

        private Dictionary<int, Item> ItemDataAcquisition()
        {
            //ensure directory exists
            FileParser.CheckSaveDirectoryExists();

            //memory usage gets really big as the yaml data is parsed... GC the crap out of it
            Dictionary<int, Item> blue = FileParser.ParseBlueprintData();
            GC.Collect();
            FileParser.ParseItemDetails(ref blue);
            GC.Collect();

            return blue;
        }

        private void BuildMarketData()
        {
            marketItems = FileParser.ParseMarketGroupData();
            GC.Collect();
        }

        private void BuildMaterial()
        {
            materials = FileParser.GatherMaterials(items);
        }

        private void BuildAllItems()
        {
            List<Item> sortedItems = new List<Item>(items.Values);
            sortedItems.Sort();

            foreach (Item item in sortedItems)
            {
                ListBoxItem listItem = new ListBoxItem
                {
                    Content = item.ProdName,
                    Tag = item.ProdId
                };

                SearchAllList.Items.Add(listItem);
            }
        }

        private void LoadSettings()
        {
            Settings.Load();
        }

        private void SearchListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initDone) return;

            ChangeManufactureItem((int)((ListBoxItem)((ListBox)sender).SelectedItem).Tag);
        }

        private void SearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ResetDefaultState();

            string searchTerm = ((TextBox)sender).Text.ToLower();
            if (string.IsNullOrEmpty(searchTerm)) return;
            if (items.Count <= 0) return;

            List<KeyValuePair<int, Item>> results = new List<KeyValuePair<int, Item>>(
                items.Where(item => item.Value.ProdName.ToLower().Contains(searchTerm)));
            results.Sort(new Item());

            SearchAllList.Items.Clear();

            foreach (KeyValuePair<int, Item> item in results)
            {
                ListBoxItem listItem = new ListBoxItem
                {
                    Content = item.Value.ProdName,
                    Tag = item.Value.BlueprintId
                };

                SearchAllList.Items.Add(listItem);
            }
        }

        private void ChangeManufactureItem(int itemId)
        {
            Item item = items[itemId];

            ManName.Content = item.ProdName;
            ManTypeId.Content = item.ProdId;
            ManBlueType.Content = item.BlueprintId;

            ManBpoCost.Content = item.BlueprintBasePrice.ToString("N") + " isk";
            ManVolumeItem.Content = item.ProdVolume.ToString("N1") + " m3";

            CalculatePrices(item);
        }

        private void CalculatePrices(Item item)
        {
            int cheapestStation = CalculateTotals(item);
            CalculateMaterials(item, cheapestStation);
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
                    cost = cheapStation == 0 ? materials[material.Type].getPrice() : materials[material.Type].getPrice(cheapStation);
                }
                else if (items.ContainsKey(material.Type))
                {
                    name = items[material.Type].ProdName;
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
        /// <returns>id of the cheapest station to buy materials from</returns>
        private int CalculateTotals(Item item)
        {
            ManProfit.Items.Clear();
            int cheapestId = 0;
            decimal cheapestPrice = decimal.MaxValue;

            foreach (Station station in Settings.Stations)
            {
                decimal buildCost = CalculateItemPrice(station.StationId, item);
                decimal stationBuy = item.BuyPrice.Count > 0 ? item.BuyPrice[station.StationId] : 0m;
                decimal stationSell = item.SellPrice.Count > 0 ? item.SellPrice[station.StationId] : 0m;

                DataGridStation grid = new DataGridStation();
                grid.Name = station.StationName;
                grid.BuildCost = buildCost.ToString("N");
                grid.ItemCost = stationSell.ToString("N");
                grid.SellMargin = (buildCost - stationSell).ToString("N");
                grid.BuyMargin = (buildCost - stationBuy).ToString("N");
                grid.IskHr = stationSell > stationBuy ? stationSell.ToString("N") : stationBuy.ToString("N");//TODO fix once build time calculations are done
                grid.Investment = "0";//TODO fix once I know what it means...

                if (buildCost < cheapestPrice)
                {
                    cheapestId = station.StationId;
                    cheapestPrice = buildCost;
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
                if (materials.ContainsKey(material.Type))
                {
                    price += materials[material.Type].getPrice(stationId) * (int)Math.Ceiling(material.Quantity * me);
                }
                else if (items.ContainsKey(material.Type))
                {
                    price += CalculateItemPrice(stationId, items[material.Type]);
                }
            }
            return price;
        }

        private decimal CalculateItemPrice(Item item)
        {
            decimal price = decimal.MaxValue;

            foreach(Station station in Settings.Stations)
            {
                decimal stationCost = CalculateItemPrice(station.StationId, item);
                if(stationCost < price) price = stationCost;
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

        private void MeChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ListBoxItem box = (ListBoxItem)SearchAllList.SelectedItem;
            if (box == null) return;

            Item item = items[(int)box.Tag];

            CalculatePrices(item);
        }
    }
}
