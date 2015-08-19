using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvE_Build
{
    public class Item
    {
        string displayName,
            blueprintName;
        int typeID,
            blueprintID,
            prodLmt = -1,
            productionQty = 1,
            productionTime = 0,
            MEtime = -1,
            TEtime = -1,
            copyTime = -1;
        //int invID = 0,
        //    invQty = 1,
        //    invTime = 0;
        Int64[,] prodMats;
        int[,] //invMats,
            copyMats,
            prodskills,
            //MEskills,
            //TEskills,
            copyskills;
        //float invProb = 0f;

        Int64[] buyCost = new Int64[5],
            sellCost = new Int64[5];

        public Item(int blueprintID,int typeID)
        {
            //all items MUST have a type ID to be created
            this.typeID = typeID;
            this.blueprintID = blueprintID;
        }

        public void setName(string name) { displayName = name; blueprintName = displayName + " Blueprint"; }
        public string getName()
        {
            if (displayName == null)
            {
                return null;
            }
            return displayName;
        }
        public string getBlueprintName()
        {
            if (displayName == null)
            {
                return null;
            }
            return displayName;
        }
        public void setProdLimit(int limit) { prodLmt = limit; }
        public void setProdTime(int time) { productionTime = time; }
        public void setProdQty(int qty) { productionQty = qty; }
        public void setCopyTime(int time) { copyTime = time; }
        public void setMEtime(int time) { MEtime = time; }
        public void setTEtime(int time) { TEtime = time; }
        public void setProdMats(int[,] mats)
        {
            prodMats = new Int64[mats.Length / 2, 2];

            //move values from one array to another
            for (int i = 0; i < (mats.Length / 2); ++i)
            {
                if (mats[i, 1] != 0)
                {
                    prodMats[i, 0] = mats[i, 0];
                    prodMats[i, 1] = mats[i, 1];
                }
            }
        }
        public void setProdskills(int[,] skill)
        {
            prodskills = new int[skill.Length / 2, 2];

            //move values from one array to another
            for (int i = 0; i < (skill.Length / 2); ++i)
            {
                if (skill[i, 1] != 0)
                {
                    prodskills[i, 0] = skill[i, 0];
                    prodskills[i, 1] = skill[i, 1];
                }
            }
        }
        public void setCopySkills(int[,] skill)
        {
            copyskills = new int[skill.Length / 2, 2];

            //move values from one array to another
            for (int i = 0; i < (skill.Length / 2); ++i)
            {
                if (skill[i, 1] != 0)
                {
                    copyskills[i, 0] = skill[i, 0];
                    copyskills[i, 1] = skill[i, 1];
                }
            }
        }
        public void setCopyMats(int[,] mats)
        {
            copyMats = new int[mats.Length / 2, 2];

            //move values from one array to another
            for (int i = 0; i < (mats.Length / 2); ++i)
            {
                if (mats[i, 1] != 0)
                {
                    copyMats[i, 0] = mats[i, 0];
                    copyMats[i, 1] = mats[i, 1];
                }
            }
        }
        public int getTypeID() { return typeID; }
        public int getProdQty() { return productionQty; }
        public int[,] getCopySkill() { return copyskills; }
        public int[,] getCopyMats() { return copyMats; }
        public int[,] getProdSkill() { return prodskills; }
        public Int64[,] getProdMats() { return prodMats; }
        public Int64 getBuyPrice(int station) {

            if (station > buyCost.Length - 1)
            {
                return 0;
            }

            return buyCost[station];
        }
        public Int64 getSellPrice(int station)
        {

            if (station > sellCost.Length - 1)
            {
                return 0;
            }

            return sellCost[station];
        }
        public void setBuyPrice(int station, Int64 cost)
        {
            if (station > buyCost.Length - 1)
            {
                return;
            }
            buyCost[station] = cost;
        }
        public void setSellPrice(int station, Int64 cost)
        {
            if (station > sellCost.Length - 1)
            {
                return;
            }
            sellCost[station] = cost;
        }
    }
}
