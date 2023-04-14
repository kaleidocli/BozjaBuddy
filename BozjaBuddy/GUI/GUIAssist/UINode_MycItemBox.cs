using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.GUIAssist
{
    public class UINode_MycItemBox : UINode
    {
        public int mRow = 0;
        public int mPosition = 0;
        public int mLastNode = 0;

        protected UINode_MycItemBox() { }
        public UINode_MycItemBox(string pAddonName, List<int> pNodePath, int pRow, int pPosition)
            : base(pAddonName, pNodePath)
        {
            this.mRow = pRow;
            this.mPosition = pPosition;
            this.mLastNode = this.mNodePath[^1];
        }
    }
}
