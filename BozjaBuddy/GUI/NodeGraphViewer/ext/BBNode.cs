using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BozjaBuddy.GUI.NodeGraphViewer.NodeContent;
using BozjaBuddy.GUI.NodeGraphViewer.utils;

namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    public class BBNode : Node
    {
        public const string nodeType = "BBNode";
        public override string mType { get; } = BBNode.nodeType;
        private Plugin? mPlugin = null;
        private int? mGenId = null;

        public override void Init(string pNodeId, int pGraphId, NodeContent.NodeContent pContent, NodeStyle? _style = null)
        {
            base.Init(pNodeId, pGraphId, pContent);
            if (pContent.GetType() == typeof(BBNodeContent))
            {
                BBNodeContent tContent = (BBNodeContent)pContent;
                this.mPlugin = tContent.GetPlugin();
                this.mGenId = tContent.GetGenObjId();
            }
        }
        protected override NodeInteractionFlags DrawBody(Vector2 pNodeOSP, float pCanvasScaling)
        {
            return NodeInteractionFlags.None;
        }
        public override void Dispose()
        {
            
        }
    }
}
