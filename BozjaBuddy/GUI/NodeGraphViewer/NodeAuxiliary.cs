using BozjaBuddy.Utils;
using System;
using System.Numerics;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Represents a node that display info from a BozjaBuddy's Auxiliary tab.
    /// </summary>
    internal class NodeAuxiliary : Node
    {
        public override string mType { get; } = "aux";

        public NodeAuxiliary() : base()
        {
            this.mStyle.colorUnique = UtilsGUI.Colors.NormalBar_Grey;
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
