using BozjaBuddy.GUI.NodeGraphViewer.ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer.NodeContent
{
    public class NodeContentError : NodeContent
    {
        public new const string nodeContentType = "nodeContentError";
        public override string _contentType => NodeContentError.nodeContentType;
        public NodeContentError(string header, string description) : base(header, description) { }
    }
}
