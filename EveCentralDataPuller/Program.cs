using System;
using System.IO;
using System.Net;
using System.Xml;

namespace EveCentralDataPuller
{
    class Program
    {
        static void Main(string[] args)
        {
            Program thing = new Program();
            thing.doStuff();
        }

        void doStuff()
        {
            Console.WriteLine("type the TypeID of the item you want data on.");
            Console.WriteLine("if you want to search multipul item type \"multi\".");

            bool finish = false;
            int searchType;
            int[] multiSearch = new int[30];
            string line = "";

            while (finish == false)
            {
                line = Console.ReadLine();
                if (line == "multi" || line == "Multi")
                {
                    //search for multipul items
                    bool found = false;
                    line = Console.ReadLine();

                    while (line != "end" && line != "End")
                    {   
                        
                        for (int i = 0; i < multiSearch.Length && found == false; ++i)
                        {
                            if (multiSearch[i] == 0)
                            {
                                multiSearch[i] = Int32.Parse(line);
                                found = true;
                            } 
                        }
                        line = Console.ReadLine();
                        found = false;
                    }

                    Console.WriteLine("------AMARR------");
                    extractPrice(getWebData(30002187, multiSearch), multiSearch);
                    Console.WriteLine("------JITA------");
                    extractPrice(getWebData(30000142, multiSearch), multiSearch);
                    Console.WriteLine("------HEK-------");
                    extractPrice(getWebData(30002053, multiSearch), multiSearch);
                    Console.WriteLine("------RENS------");
                    extractPrice(getWebData(30004970, multiSearch), multiSearch);
                    Console.WriteLine("-----DODIXIE----");
                    extractPrice(getWebData(30002659, multiSearch), multiSearch);
                    Console.WriteLine("");
                }
                else if (Int32.Parse(line) > 0)
                {
                    searchType = Int32.Parse(line);

                    Console.WriteLine("------AMARR------");
                    extractPrice(getWebData(30002187, searchType), searchType);
                    Console.WriteLine("------JITA------");
                    extractPrice(getWebData(30000142, searchType), searchType);
                    Console.WriteLine("------HEK-------");
                    extractPrice(getWebData(30002053, searchType), searchType);
                    Console.WriteLine("------RENS------");
                    extractPrice(getWebData(30004970, searchType), searchType);
                    Console.WriteLine("-----DODIXIE----");
                    extractPrice(getWebData(30002659, searchType), searchType);
                    Console.WriteLine("");
                }
            }
        }

        string getWebData(int stationID, int item)
        {
            string search = "http://api.eve-central.com/api/marketstat?&usesystem=" + stationID + "&typeid=" + item;
            return Search(search);
        }

        string getWebData(int stationID, int[] item)
        {
            string items = "",
                newItem = "&typeid=";
            for (int i = 0; item[i] != 0; ++i)
            {
                if (item[i] != 0){
                    items +=  newItem + item[i];
                }
            }

            string search = "http://api.eve-central.com/api/marketstat?&usesystem=" + stationID + items;

            return Search(search);
        }

        private string Search(string term)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(term);
            myRequest.Method = "GET";
            myRequest.Timeout = 3000;
            WebResponse myResponse;
            try
            {
                myResponse = myRequest.GetResponse();

                StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                string result = sr.ReadToEnd();
                sr.Close();
                myResponse.Close();

                return result;
            }
            catch (WebException)
            {
                Console.WriteLine("WEB QUERY FAILED");
            }
            return "";

        }

        void extractPrice(string search, int item)
        {
            if (search == "" || search == null)
            {
                return;
            }

            using (XmlReader reader = XmlReader.Create(new StringReader(search)))
            {
                while (reader.Name != "type")
                {
                    reader.Read();
                }

                //check that the correct item has been retrieved (just in case)
                reader.MoveToFirstAttribute();
                if (Int32.Parse(reader.Value.ToString()) != item)
                {
                    return;
                }

                //the item matches the item that should be retrieved, continue
                reader.MoveToElement();
                reader.ReadToFollowing("buy");
                reader.ReadToFollowing("max");
                Console.WriteLine("best buy price = " + reader.ReadElementContentAsString());

                reader.ReadToFollowing("sell");
                reader.ReadToFollowing("min");
                Console.WriteLine("best sell price = " + reader.ReadElementContentAsString());
            }
        }

        void extractPrice(string search, int[] item)
        {
            if (search == "" || search == null)
            {
                return;
            }

            using (XmlReader reader = XmlReader.Create(new StringReader(search)))
            {
                reader.ReadToFollowing("type");

                string buy, sell;
                int count = 0,
                    target = 0;

                for(int i = 0; i < item.Length - 1 ; ++i){
                    if(item[i] != 0){
                        ++target;
                    }
                }

                while (reader.Read() && count < target)
                {
                    reader.ReadToFollowing("buy");
                    reader.ReadToFollowing("max");
                    buy = reader.ReadElementContentAsString();
                    
                    reader.ReadToFollowing("sell");
                    reader.ReadToFollowing("min");
                    sell = reader.ReadElementContentAsString();

                    Console.WriteLine("Item:" + item[count] + ", sell: " + sell + ", buy: " + buy);
                    ++count;
                }
            }
        }
    }
}
