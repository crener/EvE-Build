using System;

namespace EvE_Build
{
    public class Material
    {
        //price[station index, 0 = buy]
        //                     1 = sell]
        public Int64[,] price { get; set; }
        public string name { get; set; }
        public int ID { get; set; }
        public int groupID { get; set; }
        public int marketGroupID { get; set; }
        public int race { get; set; }
        public int faction { get; set; }
        public float volume { get; set; }
        public float mass { get; set; }

        public Material()
        {
            price = new Int64[5,2];
            name = "unknown";
            ID = 0;
            volume = 0.0f;
            mass = 0.0f;
            groupID = 0;
            marketGroupID = 0;
            race = 0;
            faction = 0;
        }

        public void updatePrice(int stationIndex, Int64 buy, Int64 sell) {
            price[stationIndex, 0] = buy;
            price[stationIndex, 0] = sell;
        }

    }
}
