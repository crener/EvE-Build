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
    }
}
