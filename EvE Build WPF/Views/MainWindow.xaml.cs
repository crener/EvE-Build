using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private List<MarketItem> marketItems;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SetupData(object sender, RoutedEventArgs routedEventArgs)
        {
            Task[] jobs = new Task[2];
            jobs[0] = Task.Run(() => ExecuteDataAquisition());
            jobs[1] = Task.Run(() => buildMarketData());

            await Task.WhenAll(jobs);

            BuildAllItems();
            ManName.Content = "Select an item from the right";
        }

        private void ExecuteDataAquisition()
        {
            //ensure directory exists
            FileParser.CheckSaveDirectoryExists();

            //memory usage gets really big as the yaml data is parsed... GC the crap out of it
            items = FileParser.ParseBlueprintData();
            GC.Collect();
            FileParser.ParseItemDetails(ref items);
            GC.Collect();
        }

        private void buildMarketData()
        {
            marketItems = FileParser.ParseMarketGroupData();
            GC.Collect();
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

        private void SearchTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ManBaseMaterial_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SearchChanged(object sender, SelectionChangedEventArgs e)
        {
            Item item = items[(int)((ListBoxItem)((ListBox)sender).SelectedItem).Tag];

            ManName.Content = item.ProdName;
            ManTypeId.Content = item.ProdId;
            ManBlueType.Content = item.BlueprintId;

            ManBpoCost.Content = item.BlueprintBasePrice + " isk";
            ManVolumeItem.Content = item.ProdVolume + " m3";
        }

        private void SearchAllBox_TextChanged(object sender, TextChangedEventArgs e)
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
                ListBoxItem listItem = new ListBoxItem();
                listItem.Content = item.Value.ProdName;
                listItem.Tag = item.Value.BlueprintId;

                SearchAllList.Items.Add(listItem);
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
    }
}
