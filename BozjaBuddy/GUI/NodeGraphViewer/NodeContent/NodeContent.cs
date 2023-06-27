using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer.NodeContent
{
    public class NodeContent
    {
        public const string nodeContentType = "nodeContent";
        [JsonProperty]
        public virtual string _contentType { get; } = NodeContent.nodeContentType;
        [JsonProperty]
        private string header = "";
        [JsonProperty]
        private string description = "";

        public NodeContent() { }
        public NodeContent(string header) { this.header = header; }
        public NodeContent(string header, string description) { this.header = header; this.description = description; }

        public string GetHeader() => this.header;
        public void _setHeader(string header) => this.header = header;
        public string GetDescription() => this.description;
        public void SetDescription(string description) => this.description = description;
    }
}
