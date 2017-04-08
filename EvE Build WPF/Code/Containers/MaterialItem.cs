using System.Collections.Generic;

namespace EvE_Build_WPF.Code.Containers
{
    class MaterialItem
    {
        public string Name { get; set; }
        public int Id { get; set; }

        private Dictionary<int, decimal> prices = new Dictionary<int, decimal>();

        public MaterialItem(int id)
        {
            Id = id;
        }

        public decimal getPrice(int stationId)
        {
            if (prices.ContainsKey(stationId))
            {
                return prices[stationId];
            }

            return 0m;
        }


        public void addPrice(int stationId, decimal cost)
        {
            prices.Add(stationId, cost);
        }
    }
}
