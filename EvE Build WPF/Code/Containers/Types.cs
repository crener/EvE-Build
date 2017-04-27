namespace EvE_Build_WPF.Code.Containers
{
    public enum Race
    {
        Unknown = 0,
        Amarr = 4,
        Caldari = 1,
        Gallente = 8,
        Minmatar = 2,
        Jove = 16,
        Ore = 128
    }

    public enum Faction
    {
        Unknown,
        Amarr = 500003,
        Caldari = 500001,
        Gallente = 500004,
        Minmatar = 500002,
        Jove = 500017,
        Ore = 500014,
        Sanshas_Nation = 500019,
        Guristas_Pirates = 500010,
        Angel_Cartel = 500011,
        Serpentis = 500020,
        Blood_Raiders = 500012,
        Sisters_of_EVE = 500016,
        Mordus_Legion = 500018,
    }

    public class Material
    {
        public int Type { get; private set; }
        public long Quantity { get; private set; }

        public Material(int type, long quantity)
        {
            Type = type;
            Quantity = quantity;
        }
    }

    public class Skill
    {
        public int Type { get; private set; }
        public int Level { get; private set; }

        public Skill(int type, int level)
        {
            Type = type;
            Level = level;
        }
    }

    public class Cost
    {
        public int StationId { get; private set; }
        public decimal Price { get; private set; }

        public Cost(int stationId, decimal price)
        {
            StationId = stationId;
            Price = price;
        }
    }

    public class Station
    {
        public int StationId { get; private set; }
        public string StationName { get; private set; }

        public Station(int stationId, string stationName)
        {
            StationId = stationId;
            StationName = stationName;
        }
    }

    public class DataGridMaterial
    {
        public string Name { get; set; }
        public long Quantity { get; set; }
        public string Cost { get; set; }
    }

    public class DataGridStation
    {
        public string Name { get; set; }
        public string BuildCost { get; set; }
        public string ItemCost { get; set; }
        public string SellMargin { get; set; }
        public string BuyMargin { get; set; }
        public string IskHr { get; set; }
    }
}
