using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace EvE_Build
{
    static class GroupSetup
    {
        private const int KillCount = 40;

        public static TreeView GenerateTreeView(ref Item[] items)
        {
            TreeView view = new TreeView();

            List<MenuItem> menuItems = ParseFile();
            Dictionary<int, TreeNode> mappings = RootNodes(ref view, menuItems);
            CheckChildren(ref view, menuItems, mappings);
            AddItems(ref view, ref items, mappings);
            for (int i = 0; i < KillCount; i++) KillUnused(ref mappings);

            return view;
        }

        public static TreeView GenerateTreeView(ref Item[] items, ref TreeView old)
        {
            List<MenuItem> menuItems = ParseFile();
            Dictionary<int, TreeNode> mappings = RootNodes(ref old, menuItems);
            CheckChildren(ref old, menuItems, mappings);
            AddItems(ref old, ref items, mappings);
            for (int i = 0; i < KillCount; i++) KillUnused(ref mappings);

            return old;
        }

        private static List<MenuItem> ParseFile()
        {
            List<MenuItem> menuItems = new List<MenuItem>();
            string line;

            using (StreamReader file = new StreamReader("StaticData/invMarketGroups.csv"))
            {
                line = file.ReadLine();

                while ((line = file.ReadLine()) != null)
                {
                    try
                    {
                        string[] fields = CheckArray(line.Split(','));
                        //marketGroupID,parentGroupID,marketGroupName,description,iconID,hasTypes
                        int market, parent, icon;
                        bool type = false;

                        try
                        {
                            if (fields[0] == "None") market = -1;
                            else market = int.Parse(fields[0]);
                        }
                        catch (Exception)
                        {
                            market = -1;
                        }

                        try
                        {
                            if (fields[1] == "None") parent = -1;
                            else parent = int.Parse(fields[1]);
                        }
                        catch (Exception)
                        {
                            parent = -1;
                        }

                        try
                        {
                            if (fields[4] == "None") icon = -1;
                            else icon = int.Parse(fields[4]);
                        }
                        catch (Exception)
                        {
                            icon = -1;
                        }

                        if (fields[5] == "1") type = true;
                        else type = false;

                        MenuItem current = new MenuItem(market, fields[2], fields[3], icon, parent, type);
                        menuItems.Add(current);
                    }
                    catch (Exception e) { };
                }
            }
            return menuItems;
        }

        private static string[] CheckArray(string[] test)
        {
            List<string> output = new List<string>();

            for (int i = 0; i < test.Length; i++)
            {
                if (test[i].StartsWith("\""))
                {
                    string concat = "";

                    while (!test[i].EndsWith("\""))
                    {
                        concat += test[i] + ",";
                        ++i;
                    }

                    concat += test[i];
                    output.Add(concat);
                }
                else
                {
                    output.Add(test[i]);
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// Find all the root nodes 
        /// </summary>
        private static Dictionary<int, TreeNode> RootNodes(ref TreeView view, List<MenuItem> menuItems)
        {
            Dictionary<int, TreeNode> mappings = new Dictionary<int, TreeNode>();
            foreach (MenuItem item in menuItems)
            {
                if (item.ParentGroup < 0)
                {
                    //add to root nodes
                    TreeNode node = new TreeNode(item.Name);
                    mappings.Add(item.MarketGroup, node);
                    view.Nodes.Add(node);
                }
            }
            return mappings;
        }

        private static void CheckChildren(ref TreeView view, List<MenuItem> menuItems, Dictionary<int, TreeNode> mappings)
        {
            Dictionary<int, TreeNode> roots = new Dictionary<int, TreeNode>(mappings);

            foreach (KeyValuePair<int, TreeNode> root in roots)
            {
                foreach (MenuItem sub in menuItems)
                {
                    if (sub.ParentGroup == root.Key)
                    {
                        TreeNode subNode = new TreeNode(sub.Name);
                        root.Value.Nodes.Add(subNode);
                        mappings.Add(sub.MarketGroup, subNode);
                        DiscoverSubs(ref subNode, sub.MarketGroup, menuItems, mappings);
                    }
                }
            }
        }

        private static void DiscoverSubs(ref TreeNode parent, int marketId, List<MenuItem> menuItems, Dictionary<int, TreeNode> mappings)
        {
            //find and add all subs in this chain
            foreach (MenuItem sub in menuItems)
            {
                if (sub.ParentGroup == marketId)
                {
                    TreeNode subNode = new TreeNode(sub.Name);
                    parent.Nodes.Add(subNode);
                    mappings.Add(sub.MarketGroup, subNode);
                    DiscoverSubs(ref subNode, sub.MarketGroup, menuItems, mappings);
                }
            }
        }

        private static void AddItems(ref TreeView view, ref Item[] items, Dictionary<int, TreeNode> mappings)
        {
            foreach (Item item in items)
            {
                TreeNode val;
                if (mappings.TryGetValue(item.getMarketGroupID(), out val))
                {
                    val.Nodes.Add(new TreeNode(item.getName()));
                }
            }
        }

        private static void KillUnused(ref Dictionary<int, TreeNode> mappings)
        {
            List<int> remove = new List<int>();
            foreach (KeyValuePair<int, TreeNode> everything in mappings)
                if (everything.Value.Nodes.Count <= 0) remove.Add(everything.Key);

            foreach (int i in remove)
            {
                TreeNode node;
                if (mappings.TryGetValue(i, out node))
                {
                    mappings.Remove(i);
                    node.Remove();
                }
            }
        }

        private class MenuItem
        {
            int marketGroup;
            int parent = -1;
            string name = "";
            string description = "";
            int iconID = -1;
            bool hasTypes = false;

            public MenuItem(int marketGroup, string name, string desc = "", int icon = -1, int parent = -1, bool hasTypes = false)
            {
                this.marketGroup = marketGroup;
                this.name = name;
                this.description = desc;
                this.iconID = icon;
                this.parent = parent;
                this.hasTypes = hasTypes;
            }

            #region Auto getters and setters
            public int MarketGroup
            {
                get { return marketGroup; }
            }

            public int ParentGroup
            {
                get { return parent; }
            }

            public int IconID
            {
                get { return iconID; }
            }

            public string Name
            {
                get { return name; }
            }

            public string Description
            {
                get { return description; }
            }

            public bool hasType
            {
                get { return hasTypes; }
            }
            #endregion
        }
    }
}
