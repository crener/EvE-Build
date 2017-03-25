using System;
using System.Collections.Generic;
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

            FileParser parser = new FileParser();

            //memory usage gets really big as the yaml data is parsed... GC the crap out of it
            items = parser.ParseBlueprintData();
            GC.Collect();
            parser.ParseItemDetails(ref items);
            GC.Collect();
            marketItems = parser.ParseMarketGroupData();
            GC.Collect();

        }

        private void SearchTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ManBaseMaterial_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
