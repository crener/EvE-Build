using System;
using System.Collections.Generic;
using EvE_Build_WPF.Code.Containers;
using Material = EvE_Build_WPF.Code.Containers.Material;

namespace EvE_Build_WPF.Code
{
    public class Item : IComparable<Item>, IComparer<KeyValuePair<int, Item>>, IEveCentralItem
    {
        public string BlueprintName { get; set; }
        public decimal BlueprintBasePrice { get; set; }
        public int BlueprintId { get; set; }

        public int TypeId { get; set; }
        public bool Released { get; set; }

        public string ProdName { get; set; }
        public int ProdLimit { get; set; }
        public int ProdQty { get; set; }
        public int ProdId { get; set; }
        public int ProdTime { get; set; }
        public decimal ProdBasePrice { get; set; }
        public decimal ProdVolume { get; set; }

        public int MatResearchTime { get; set; }
        public int TimeResearchTime { get; set; }
        public int CopyTime { get; set; }

        private Dictionary<int, long> prodMats = new Dictionary<int, long>();
        private Dictionary<int, long> copyMats = new Dictionary<int, long>();
        private Dictionary<int, int> prodSkills = new Dictionary<int, int>();
        private Dictionary<int, int> copySkills = new Dictionary<int, int>();

        private Dictionary<int, decimal> buyCost = new Dictionary<int, decimal>();
        private Dictionary<int, decimal> sellCost = new Dictionary<int, decimal>();

        public Faction FactionId { get; set; }
        public bool isFaction { get; set; }
        public string SofFaction { get; set; }
        public int GroudId { get; set; }
        public int MarketGroupId { get; set; }
        public Race Race { get; set; }

        public Item()
        {
            ProdName = "";
            Released = true;
        }

        public Item(int blueprintId, int typeId)
        {
            BlueprintId = blueprintId;
            TypeId = typeId;

            ProdName = "";
            Race = Race.Unknown;
            Released = true;
        }

        public bool CheckValididty()
        {
            if (prodMats.Count > 0) return true;
            return false;
        }

        public bool CheckItemViability()
        {
            bool result = CheckValididty();
            result = result && MarketGroupId != 0;
            result = result && ProdName.Length >= 1;
            result = result && BlueprintName != null;

            return result;
        }

        public void AddProductMaterial(int id, long quantity)
        {
            prodMats.Add(id, quantity);
        }

        public void AddCopyMaterial(int id, long quantity)
        {
            copyMats.Add(id, quantity);
        }

        public void AddProductSkill(int id, int level)
        {
            if (level > 6) throw new FormatException();
            if (prodSkills.ContainsKey(id) && prodSkills[id] == level) return;

            prodSkills.Add(id, level);
        }

        public void AddCopySkill(int id, int level)
        {
            if (level > 6) throw new FormatException();
            if (copySkills.ContainsKey(id) && copySkills[id] == level) return;

            copySkills.Add(id, level);
        }

        public Material[] ProductMaterial
        {
            get
            {
                List<Material> mats = new List<Material>(prodMats.Count + 1);
                foreach (KeyValuePair<int, long> pair in prodMats)
                {
                    mats.Add(new Material(pair.Key, pair.Value));
                }

                return mats.ToArray();
            }
        }

        public Skill[] ProductSkills
        {
            get
            {
                List<Skill> skills = new List<Skill>(prodSkills.Count + 1);
                foreach (KeyValuePair<int, int> pair in prodSkills)
                {
                    skills.Add(new Skill(pair.Key, pair.Value));
                }

                return skills.ToArray();
            }
        }

        public Material[] CopyMaterial
        {
            get
            {
                List<Material> mats = new List<Material>(copyMats.Count + 1);
                foreach (KeyValuePair<int, long> pair in copyMats)
                {
                    mats.Add(new Material(pair.Key, pair.Value));
                }

                return mats.ToArray();
            }
        }

        public Skill[] CopySkills
        {
            get
            {
                List<Skill> skills = new List<Skill>(copySkills.Count + 1);
                foreach (KeyValuePair<int, int> pair in copySkills)
                {
                    skills.Add(new Skill(pair.Key, pair.Value));
                }

                return skills.ToArray();
            }
        }

        public Dictionary<int, decimal> BuyPrice
        {
            get
            {
                return buyCost;
            }
        }

        public Dictionary<int, decimal> SellPrice
        {
            get
            {
                return sellCost;
            }
        }

        public void setBuyCost(int station, decimal isk)
        {
            if (buyCost.ContainsKey(station))
            {
                buyCost[station] = isk;
            }
            else { buyCost.Add(station, isk); }
        }

        public void setSellCost(int station, decimal isk)
        {
            if (sellCost.ContainsKey(station))
            {
                sellCost[station] = isk;
            }
            else { sellCost.Add(station, isk); }
        }

        public int CompareTo(Item other)
        {
            return string.Compare(ProdName, other.ProdName, StringComparison.Ordinal);
        }

        public int CompareTo(KeyValuePair<int, Item> other)
        {
            return CompareTo(other.Value);
        }

        public int Compare(KeyValuePair<int, Item> x, KeyValuePair<int, Item> y)
        {
            return x.Value.CompareTo(y.Value);
        }

        public static Item Merdge(int id, Item newObject)
        {
            return newObject;
        }
    }
}
