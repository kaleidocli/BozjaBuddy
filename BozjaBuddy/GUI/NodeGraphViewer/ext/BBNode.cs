using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    public class BBNode : Node
    {
        public override string mType { get; } = "bb";
        Plugin? mPlugin = null;
        int? mGenId = null;

        public override void Init(string pNodeId, int pGraphId, NodeContent pContent)
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
