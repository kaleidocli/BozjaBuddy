using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.GUIAssist
{
    public class UINode
    {
        public string mAddonName;
        public List<int> mNodePath;

        protected UINode() { }
        public UINode(string pAddonName, List<int> pNodePath)
        {
            this.mAddonName = pAddonName;
            this.mNodePath = pNodePath;
        }
    }
}
