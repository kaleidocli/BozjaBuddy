using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    public class BBNodeContent : NodeContent.NodeContent
    {
        public new const string nodeContentType = "nodeContentBB";
        public override string _contentType => BBNodeContent.nodeContentType;
        [JsonProperty]
        private int genObjId;
        private Plugin? plugin;

        /// <summary>FIXME: make param plugin not nullable. stuff was for debugging serialization.</summary>
        public BBNodeContent(Plugin? plugin, int genObjId, string header, string description = "")
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
