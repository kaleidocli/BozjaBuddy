using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BozjaBuddy.GUI.NodeGraphViewer.NodeContent;
using BozjaBuddy.GUI.NodeGraphViewer.utils;
using Dalamud.Logging;

namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    public class BBNode : Node
    {
        public static Plugin? kPlugin = null;       // this is a bad way to go with...
        public const string nodeType = "BBNode";
        public override string mType { get; } = BBNode.nodeType;
        protected Plugin? mPlugin = null;
        protected int? mGenId = null;

        public override void Init(string pNodeId, int pGraphId, NodeContent.NodeContent pContent, NodeStyle? _style = null)
        {
            base.Init(pNodeId, pGraphId, pContent, _style);
            if (pContent.GetType() == typeof(BBNodeContent))
            {
                BBNodeContent tContent = (BBNodeContent)pContent;
                this.mPlugin = tContent.GetPlugin();
                if (this.mPlugin == null && BBNode.kPlugin != null) this.mPlugin = BBNode.kPlugin;
                this.mGenId = tContent.GetGenObjId();
                if (this.mPlugin != null && this.mGenId.HasValue)
                {
                    if (this.mPlugin.mBBDataManager.mGeneralObjects.TryGetValue(this.mGenId.Value, out var pObj) && pObj != null)
                    {
                        this.mStyle.colorUnique = pObj.mTabColor ?? this.mStyle.colorUnique;
                    }

                }
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
