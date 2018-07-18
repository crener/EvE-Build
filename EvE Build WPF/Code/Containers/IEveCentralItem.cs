namespace EvE_Build_WPF.Code.Containers
{
    interface IEveCentralItem
    {
        void UpdateBuyCost(int currentStation, decimal cost);
        void UpdateSellPrice(int currentStation, decimal cost);
    }
}
