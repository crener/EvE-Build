using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvE_Build
{
    public class Material
    {
        //price[station index, 0 = buy]
        //                     1 = sell]
        public Int64[,] price { get; set; }
        public string name { get; set; }
        public int ID { get; set; }
        public float volume { get; set; }

        public Material()
        {
            price = new Int64[5,2];
            name = "unknown";
            ID = 0;
            volume = 0.0f;
        }

        public void updatePrice(int stationIndex, Int64 buy, Int64 sell) {
            price[stationIndex, 0] = buy;
            price[stationIndex, 0] = sell;
        }

    }
}
