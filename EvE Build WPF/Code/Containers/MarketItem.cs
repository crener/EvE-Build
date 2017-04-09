using System.Windows.Controls;

namespace EvE_Build_WPF.Code.Containers
{
    class MarketItem
    {
        public int MarketId { get; set; }
        public int ParentGroupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TreeViewItem TreeViewObject{ get; set; }
    }
}
