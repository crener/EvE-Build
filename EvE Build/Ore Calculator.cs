using System;
using System.Data;
using System.Threading;
using System.Windows.Forms;

struct RefineMins
{
    public int Tritainium,
        Pyerite,
        Mexallon,
        Isogen,
        Nocxium,
        Megacyte,
        Zydrine;
}

struct Minerals
{
    public long[] price;
    public int ID;
    public string name;
}

struct Ore
{
    public string name;
    public int ID;
    public RefineMins minerals;
    public int[] StationPrice;
}

struct Station
{
    public string name;
    public int ID;
}

namespace EvE_Build
{
    public partial class Ore_Calculator : Form
    {
        Ore[] ore = new Ore[45];
        Station[] stations = new Station[5];
        Thread oreGettingThread;
        Minerals[] mins = new Minerals[7];

        public Ore_Calculator(int[] stationID, string[] stationName)
        {
            InitializeComponent();

            for (int i = 0; i < 5; i++)
            {
                stations[i].name = stationName[i];
                stations[i].ID = stationID[i];
            }
        }

        private void SetupOre()
        {
            ore[0].name = "Prime Arkonor";
            ore[0].ID = 17426;
            ore[0].minerals = new RefineMins { Tritainium = 7596, Megacyte = 253, Zydrine = 127 };

            ore[1].name = "Crimson Arkonor";
            ore[1].ID = 17425;
            ore[1].minerals = new RefineMins { Tritainium = 7251, Megacyte = 242, Zydrine = 121 };

            ore[2].name = "Arkonor";
            ore[2].ID = 22;
            ore[2].minerals = new RefineMins { Tritainium = 2905, Megacyte = 230, Zydrine = 155 };

            ore[3].name = "Bistot";
            ore[3].ID = 1223;
            ore[3].minerals = new RefineMins { Pyerite = 8286, Megacyte = 118, Zydrine = 236 };

            ore[4].name = "Triclinic Bistot";
            ore[4].ID = 17428;
            ore[4].minerals = new RefineMins { Pyerite = 8701, Megacyte = 124, Zydrine = 248 };

            ore[5].name = "Monoclinic Bistot";
            ore[5].ID = 17429;
            ore[5].minerals = new RefineMins { Pyerite = 9115, Megacyte = 130, Zydrine = 259 };

            ore[6].name = "Crokite";
            ore[6].ID = 1225;
            ore[6].minerals = new RefineMins { Tritainium = 20992, Nocxium = 183, Zydrine = 367 };

            ore[7].name = "Sharp Crokite";
            ore[7].ID = 17432;
            ore[7].minerals = new RefineMins { Tritainium = 22041, Nocxium = 193, Zydrine = 385 };

            ore[8].name = "Crystalline Crokite";
            ore[8].ID = 17433;
            ore[8].minerals = new RefineMins { Tritainium = 23091, Nocxium = 202, Zydrine = 403 };

            ore[9].name = "Dark Ochre";
            ore[9].ID = 1232;
            ore[9].minerals = new RefineMins { Tritainium = 8804, Nocxium = 173, Zydrine = 87 };

            ore[10].name = "Onyx Ochre";
            ore[10].ID = 17436;
            ore[10].minerals = new RefineMins { Tritainium = 9245, Nocxium = 182, Zydrine = 91 };

            ore[11].name = "Obsidian Ochre";
            ore[11].ID = 17437;
            ore[11].minerals = new RefineMins { Tritainium = 9685, Nocxium = 190, Zydrine = 95 };

            ore[12].name = "Iridescent Gneiss";
            ore[12].ID = 17865;
            ore[12].minerals = new RefineMins { Tritainium = 1342, Mexallon = 1342, Isogen = 254, Zydrine = 63 };

            ore[13].name = "Prismatic Gneiss";
            ore[13].ID = 17866;
            ore[13].minerals = new RefineMins { Tritainium = 1406, Mexallon = 1406, Isogen = 266, Zydrine = 65 };

            ore[14].name = "Gneiss";
            ore[14].ID = 1229;
            ore[14].minerals = new RefineMins { Tritainium = 1278, Mexallon = 1278, Isogen = 242, Zydrine = 60 };

            ore[15].name = "Vitric Hedbergite";
            ore[15].ID = 17440;
            ore[15].minerals = new RefineMins { Pyerite = 85, Isogen = 206, Nocxium = 103, Zydrine = 10 };

            ore[16].name = "Glazed Hedbergite";
            ore[16].ID = 17441;
            ore[16].minerals = new RefineMins { Pyerite = 89, Isogen = 216, Nocxium = 108, Zydrine = 10 };

            ore[17].name = "Hedbergite";
            ore[17].ID = 21;
            ore[17].minerals = new RefineMins { Pyerite = 81, Isogen = 196, Nocxium = 98, Zydrine = 9 };

            ore[18].name = "Vitric Hemorphite";
            ore[18].ID = 17444;
            ore[18].minerals = new RefineMins { Tritainium = 189, Pyerite = 76, Mexallon = 18, Isogen = 62, Nocxium = 123, Zydrine = 9 };

            ore[19].name = "Radiant Hemorphite";
            ore[19].ID = 17445;
            ore[19].minerals = new RefineMins { Tritainium = 198, Pyerite = 79, Mexallon = 19, Isogen = 65, Nocxium = 129, Zydrine = 9 };

            ore[20].name = "Hemorphite";
            ore[20].ID = 1231;
            ore[20].minerals = new RefineMins { Tritainium = 180, Pyerite = 72, Mexallon = 17, Isogen = 59, Nocxium = 118, Zydrine = 8 };

            ore[21].name = "Pure Jaspet";
            ore[21].ID = 17448;
            ore[21].minerals = new RefineMins { Tritainium = 76, Pyerite = 127, Mexallon = 151, Nocxium = 76, Zydrine = 3 };

            ore[22].name = "Pristine Jaspet";
            ore[22].ID = 17449;
            ore[22].minerals = new RefineMins { Tritainium = 79, Pyerite = 133, Mexallon = 158, Nocxium = 79, Zydrine = 3 };

            ore[23].name = "Jaspet";
            ore[23].ID = 1226;
            ore[23].minerals = new RefineMins { Tritainium = 72, Pyerite = 121, Mexallon = 144, Nocxium = 72, Zydrine = 3 };

            ore[24].name = "Luminous kernite";
            ore[24].ID = 17452;
            ore[24].minerals = new RefineMins { Tritainium = 140, Mexallon = 281, Isogen = 140 };

            ore[25].name = "Fiery Kernite";
            ore[25].ID = 17453;
            ore[25].minerals = new RefineMins { Tritainium = 147, Mexallon = 294, Isogen = 147 };

            ore[26].name = "Kernite";
            ore[26].ID = 20;
            ore[26].minerals = new RefineMins { Tritainium = 134, Mexallon = 267, Isogen = 137 };

            ore[27].name = "Silvery Omber";
            ore[27].ID = 17867;
            ore[27].minerals = new RefineMins { Tritainium = 89, Pyerite = 36, Isogen = 89 };

            ore[28].name = "Omber";
            ore[28].ID = 1227;
            ore[28].minerals = new RefineMins { Tritainium = 85, Pyerite = 34, Isogen = 85 };

            ore[29].name = "Golden Omber";
            ore[29].ID = 17868;
            ore[29].minerals = new RefineMins { Tritainium = 94, Pyerite = 38, Isogen = 94 };

            ore[30].name = "Azure Plagioclase";
            ore[30].ID = 17455;
            ore[30].minerals = new RefineMins { Tritainium = 112, Pyerite = 224, Mexallon = 112 };

            ore[31].name = "Rich Plagioclase";
            ore[31].ID = 17456;
            ore[31].minerals = new RefineMins { Tritainium = 117, Pyerite = 234, Mexallon = 117 };

            ore[32].name = "Plagioclase";
            ore[32].ID = 18;
            ore[32].minerals = new RefineMins { Tritainium = 107, Pyerite = 213, Mexallon = 107 };

            ore[33].name = "Pyroxeres";
            ore[33].ID = 1224;
            ore[33].minerals = new RefineMins { Tritainium = 351, Pyerite = 25, Mexallon = 50, Nocxium = 5 };

            ore[34].name = "Solid Pyroxeres";
            ore[34].ID = 17459;
            ore[34].minerals = new RefineMins { Tritainium = 368, Pyerite = 26, Mexallon = 53, Nocxium = 5 };

            ore[35].name = "Viscous Pyoxeres";
            ore[35].ID = 17460;
            ore[35].minerals = new RefineMins { Tritainium = 385, Pyerite = 27, Mexallon = 55, Nocxium = 5 };

            ore[36].name = "Scordite";
            ore[36].ID = 1228;
            ore[36].minerals = new RefineMins { Tritainium = 346, Pyerite = 173 };

            ore[37].name = "Condensed Scordite";
            ore[37].ID = 17463;
            ore[37].minerals = new RefineMins { Tritainium = 363, Pyerite = 182 };

            ore[38].name = "Massive Scordite";
            ore[38].ID = 17464;
            ore[38].minerals = new RefineMins { Tritainium = 380, Pyerite = 190 };

            ore[39].name = "Spudumain";
            ore[39].ID = 19;
            ore[39].minerals = new RefineMins { Tritainium = 39221, Pyerite = 4972 };

            ore[40].name = "Bright Spudumain";
            ore[40].ID = 17466;
            ore[40].minerals = new RefineMins { Tritainium = 41182, Pyerite = 5221 };

            ore[41].name = "Gleaming Spudumain";
            ore[41].ID = 17467;
            ore[41].minerals = new RefineMins { Tritainium = 43143, Pyerite = 5469 };

            ore[42].name = "Veldspar";
            ore[42].ID = 1230;
            ore[42].minerals = new RefineMins { Tritainium = 415 };

            ore[43].name = "Concentrated Veldspar";
            ore[43].ID = 17470;
            ore[43].minerals = new RefineMins { Tritainium = 436 };

            ore[44].name = "Dense Veldspar";
            ore[44].ID = 17471;
            ore[44].minerals = new RefineMins { Tritainium = 467 };
        }

        private void SetupMins()
        {
            mins[0].name = "Tritanium";
            mins[0].ID = 34;

            mins[1].name = "Pyerite";
            mins[1].ID = 35;

            mins[2].name = "Mexallon";
            mins[2].ID = 36;
        
            mins[3].name = "Isogen";
            mins[3].ID = 37;

            mins[4].name = "Nocxium";
            mins[4].ID = 38;

            mins[5].name = "Megacyte";
            mins[5].ID = 39;

            mins[6].name = "Zydrine";
            mins[6].ID = 40;
        }

        private void WebThread()
        {
            WebInterface getter = new WebInterface();
            try
            {
                while (true)
                {
                    //Ore Price
                    for (int i = 0; i < 5; i++)
                    {
                        int[] items = new int[(ore.Length - 1) + (7)];

                        for (int u = 0; u < ore.Length - 1; u++)
                        {
                            items[u] = ore[u].ID;
                        }

                        for (int t = 0; t < 7; t++)
                        {
                            items[(ore.Length - 1) + t] = mins[t].ID;
                        }

                        long[,] returnData = getter.extractPrice(getter.getWebData(i, items), items);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                oreGettingThread.Abort();
            }  
        }

        private void Ore_Calculator_Load(object sender, EventArgs e)
        {
            SetupOre();

            //add all ore types to the box
            foreach (Ore item in ore)
            {
                OreList.Items.Add(item.name);
            }

            //set the minerals to the output
            DataTable minerals = new DataTable();
            minerals.Columns.Add("Mineral", typeof(string));
            minerals.Columns.Add("Quantity", typeof(long));
            minerals.Columns.Add(stations[0].name, typeof(long));
            minerals.Columns.Add(stations[1].name, typeof(long));
            minerals.Columns.Add(stations[2].name, typeof(long));
            minerals.Columns.Add(stations[3].name, typeof(long));
            minerals.Columns.Add(stations[4].name, typeof(long));

            minerals.Rows.Add("Tritanium", 0, 0, 0, 0, 0, 0);
            minerals.Rows.Add("Pyerite", 0, 0, 0, 0, 0, 0);
            minerals.Rows.Add("Mexallon", 0, 0, 0, 0, 0, 0);
            minerals.Rows.Add("Isogen", 0, 0, 0, 0, 0, 0);
            minerals.Rows.Add("Nocxium", 0, 0, 0, 0, 0, 0);
            minerals.Rows.Add("Megacyte", 0, 0, 0, 0, 0, 0);
            minerals.Rows.Add("Zydrine", 0, 0, 0, 0, 0, 0);

            oreGettingThread = new Thread(WebThread);
            oreGettingThread.Name = "Ore Fetcher";
            oreGettingThread.Start();
        }

        private void OreList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //generate a list of ores
            DataTable ores = new DataTable();
            ores.Columns.Add("Ore Type", typeof(string));
            ores.Columns.Add("Quantity", typeof(string));
            ores.Columns["Quantity"].ReadOnly = false;
            ores.Columns.Add(stations[0].name, typeof(string));
            ores.Columns.Add(stations[1].name, typeof(string));
            ores.Columns.Add(stations[2].name, typeof(string));
            ores.Columns.Add(stations[3].name, typeof(string));
            ores.Columns.Add(stations[4].name, typeof(string));

            for (int i = 0; i < OreList.CheckedItems.Count; i++)
            {
                string name = OreList.CheckedItems[i].ToString();
                ores.Rows.Add(name, 0);
            }


            Input.DataSource = ores;
        }

        private void Input_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
