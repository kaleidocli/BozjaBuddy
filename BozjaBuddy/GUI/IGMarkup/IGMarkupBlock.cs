using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.IGMarkup
{
    abstract public class IGMarkupBlock
    {
        public virtual int mScope { get; set; } = 0;
        abstract protected void ProcessBlock(string pRawBlock);
        abstract public void DrawGUI();
    }
}
