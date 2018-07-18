using System.Collections.Generic;

namespace EvE_Build_WPF.Code.Containers
{
    class MaterialItem : IEveCentralItem
    {
        public string Name { get; set; }
        public int Id { get; private set; }

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

        /// <summary>
        /// returns cheapest price from collection
        /// </summary>
        /// <returns></returns>
        public decimal getPrice()
        {
            if (prices.Count == 0) return 0m;

            decimal cheapest = decimal.MaxValue;

            foreach (decimal current in prices.Values)
            {
                if (current < cheapest) cheapest = current;
            }

            return cheapest;
        }

        public void addPrice(int stationId, decimal cost)
        {
            prices.Add(stationId, cost);
        }

        public static MaterialItem merdge(int id, MaterialItem newObject)
        {
            return newObject;
        }

        public void UpdateBuyCost(int currentStation, decimal cost)
        {
            //ignore as this isn't needed (yet)
        }

        public void UpdateSellPrice(int currentStation, decimal cost)
        {
            if (prices.ContainsKey(currentStation))
            {
                prices[currentStation] = cost;
            }
            else
            {
                prices.Add(currentStation, cost);
            }
        }
    }
}
