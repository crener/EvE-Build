using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Data;
using EvE_Build_WPF.Code;
using EvE_Build_WPF.Code.Containers;

namespace EvE_Build_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<int, Item> items;
        private Dictionary<int, MaterialItem> materials;
        private List<MarketItem> marketItems;
        private bool initDone = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SetupData(object sender, RoutedEventArgs routedEventArgs)
        {
            Task[] jobs = new Task[3];
            jobs[0] = Task.Run(() => ExecuteDataAcquisition());
            jobs[1] = Task.Run(() => BuildMarketData());
            jobs[2] = Task.Run(() => LoadSettings());

            await Task.WhenAll(jobs);
            await Task.Run(() => BuildMaterial());

            BuildAllItems();
            BuildTreeView();
            ManName.Content = "Select an item from the right";

            initDone = true;
        }

        private void BuildTreeView()
        {
            //create initial tree view items
            foreach (MarketItem mat in marketItems)
            {
                TreeViewItem viewItem = new TreeViewItem();
                viewItem.Header = mat.Name;
                viewItem.Tag = "market," + mat.MarketId;

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

                    TreeViewItem viewItem = new TreeViewItem();
                    viewItem.Tag = "item," + item.Key;
                    viewItem.Header = item.Value.ProdName;

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

        private void ExecuteDataAcquisition()
        {
            //ensure directory exists
            FileParser.CheckSaveDirectoryExists();

            //memory usage gets really big as the yaml data is parsed... GC the crap out of it
            items = FileParser.ParseBlueprintData();
            GC.Collect();
            FileParser.ParseItemDetails(ref items);
            GC.Collect();
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
                ListBoxItem listItem = new ListBoxItem();
                listItem.Content = item.ProdName;
                listItem.Tag = item.BlueprintId;

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

            ManBpoCost.Content = item.BlueprintBasePrice + " isk";
            ManVolumeItem.Content = item.ProdVolume + " m3";

            CalculateMaterials(item);
        }


        private void CalculateMaterials(Item item)
        {
            ManRaw.Items.Clear();

            float me = 1f - (float)ManMe.Value / 100f;

            foreach (Material material in item.getProductMaterial())
            {
                long actualQty = (long)Math.Ceiling(material.Quantity * me);

                DataGridMaterial gridMaterial = new DataGridMaterial
                {
                    Name = materials[material.Type].Name,
                    Quantity = actualQty
                };

                ManRaw.Items.Add(gridMaterial);
            }
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
            ListBoxItem box = (ListBoxItem) SearchAllList.SelectedItem;
            if(box == null) return;
            
            Item item = items[(int)box.Tag];

            CalculateMaterials(item);
        }
    }
}
