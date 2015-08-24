using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.IO;
using System.Windows.Forms;

namespace EvE_Build
{
    class WebInterface
    {
        public string getWebData(int stationID, int item)
        {
            if (stationID == 0)
            {
                //no point wasting time if there is no station to check
                return "";
            }

            string search = "http://api.eve-central.com/api/marketstat?&usesystem=" + stationID + "&typeid=" + item;

            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(search);
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
                MessageBox.Show("web query failed, if the station data \nhas been changed recently make sure the station ID id correct");
            }
            return "";
        }

        public string getWebData(int stationID, int[] item)
        {

            if (stationID == 0)
            {
                //no point wasting time if there is no station to check
                return "";
            }

            string items = "",
                newItem = "&typeid=";
            for (int i = 0; i < item.Length && item[i] != 0; ++i)
            {
                if (item[i] != 0)
                {
                    items += newItem + item[i];
                }
            }

            string search = "http://api.eve-central.com/api/marketstat?&usesystem=" + stationID + items;

            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(search);
            myRequest.Method = "GET";
            myRequest.Timeout = 8000;
            WebResponse myResponse;
            myResponse = myRequest.GetResponse();

            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            string result = sr.ReadToEnd();
            sr.Close();
            myResponse.Close();

            return result;
        }

        public Int64[] extractPrice(string search, int item)
        {
            if (search == "" || search == null)
            {
                //no point wasting time if there is no data to parse
                return new Int64[2];
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
                    return new Int64[2];
                }

                Int64[] output = new Int64[2];
                string temp;

                //the item matches the item that should be retrieved, continue
                reader.MoveToElement();
                reader.ReadToFollowing("buy");
                reader.ReadToFollowing("max");
                temp = reader.ReadElementContentAsString();
                temp = temp.Remove(temp.IndexOf("."), 1);
                output[0] = Int64.Parse(temp);

                reader.ReadToFollowing("sell");
                reader.ReadToFollowing("min");
                temp = reader.ReadElementContentAsString();
                temp = temp.Remove(temp.IndexOf("."), 1);
                output[1] = Int64.Parse(temp);

                return output;
            }
        }

        public Int64[,] extractPrice(string search, int[] item)
        {
            if (search == "" || search == null)
            {
                //no point wasting time if there is no data to parse
                return new Int64[item.Length - 1, 2];
            }

            using (XmlReader reader = XmlReader.Create(new StringReader(search)))
            {
                //reader.ReadToFollowing("type");

                Int64 buy, sell;
                int count = 0;
                Int64[,] output = new Int64[item.Length, 2];
                string temp = "";

                //while (reader.Read() && count < target)
                //while (count < target && reader.Name != "")
                for (int i = 0; i < item.Length; ++i)
                {

                    //check that the correct item is being read
                    reader.ReadToFollowing("type");
                    reader.MoveToFirstAttribute();
                    if (reader.NodeType == XmlNodeType.None || Int32.Parse(reader.Value.ToString()) != item[i])
                    {
                        continue;
                    }

                    reader.ReadToFollowing("buy");
                    reader.ReadToFollowing("max");
                    temp = reader.ReadElementContentAsString();
                    temp = temp.Remove(temp.IndexOf("."), 1);
                    buy = Int64.Parse(temp);

                    reader.ReadToFollowing("sell");
                    reader.ReadToFollowing("min");
                    temp = reader.ReadElementContentAsString();
                    temp = temp.Remove(temp.IndexOf("."), 1);
                    sell = Int64.Parse(temp);

                    output[i, 0] = buy;
                    output[i, 1] = sell;
                    ++count;
                }
                return output;
            }
        }

        public void extractPrice(string search, ref Item[] item, int station)
        {
            if (search == "" || search == null)
            {
                //no point wasting time if there is no data to parse
                return;
            }

            using (XmlReader reader = XmlReader.Create(new StringReader(search)))
            {
                //reader.ReadToFollowing("type");

                Int64 buy, sell;
                int count = 0;
                string temp = "";

                //while (reader.Read() && count < target)
                //while (count < target && reader.Name != "")
                for (int i = 0; i < item.Length - 1; ++i)
                {

                    //TODO remove the dot from the returned value to ensure that the point is carried (int64 doesn't support decimals)
                    //TODO make sure that every time the user is given the cost values a dot is added for 0.xx isk

                    //check that the correct item is being read
                    reader.ReadToFollowing("type");
                    reader.MoveToFirstAttribute();
                    if (reader.NodeType == XmlNodeType.None || Int32.Parse(reader.Value.ToString()) != item[i].getBlueprintTypeID())
                    {
                        continue;
                    }

                    reader.ReadToFollowing("buy");
                    reader.ReadToFollowing("max");
                    temp = reader.ReadElementContentAsString();
                    temp = temp.Remove(temp.IndexOf("."), 1);
                    buy = Int64.Parse(temp);

                    reader.ReadToFollowing("sell");
                    reader.ReadToFollowing("min");
                    temp = reader.ReadElementContentAsString();
                    temp = temp.Remove(temp.IndexOf("."), 1);
                    sell = Int64.Parse(temp);

                    item[i].setSellPrice(station, sell);
                    item[i].setBuyPrice(station, buy);
                    //output[i, 0] = buy;
                    //output[i, 1] = sell;
                    ++count;
                }
            }
        }
    }
}
