using BozjaBuddy.Utils;
using System;
using System.Numerics;

namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    /// <summary>
    /// Represents a node that display info from a BozjaBuddy's Auxiliary tab.
    /// </summary>
    internal class AuxNode : BBNode
    {
        public override string mType { get; } = "aux";

        public AuxNode() : base()
        {
            mStyle.colorUnique = UtilsGUI.Colors.NormalBar_Grey;
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
