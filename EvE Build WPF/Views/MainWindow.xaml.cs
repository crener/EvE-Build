using System;
using System.Collections;
using System.Collections.Generic;
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
            await Task.Run(() => ExecuteDataAquisition());

            BuildAllItems();
        }

        private void ExecuteDataAquisition()
        {
            FileParser parser = new FileParser();

            //memory usage gets really big as the yaml data is parsed... GC the crap out of it
            items = parser.ParseBlueprintData();
            GC.Collect();
            parser.ParseItemDetails(ref items);
            GC.Collect();
            marketItems = parser.ParseMarketGroupData();
            GC.Collect();
        }

        private void BuildAllItems()
        {
            List<Item> sortedItems = new List<Item>(items.Values);
            sortedItems.Sort();

            foreach(Item item in sortedItems)
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
    }
}
