using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    public class BBNodeContent : Node.NodeContent
    {
        private int genObjId;
        private Plugin plugin;

        public BBNodeContent(Plugin plugin, int genObjId, string header, string description = "")
            : base(header, description)
        {
            this.plugin = plugin;
            this.genObjId = genObjId;
        }

        public int? GetGenObjId() => this.genObjId;
        public void SetGenObjId(int id) => this.genObjId = id;
        public Plugin? GetPlugin() => this.plugin;
    }
}
