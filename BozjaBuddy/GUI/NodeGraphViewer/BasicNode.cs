using System.Numerics;
using ImGuiNET;
using BozjaBuddy.GUI.NodeGraphViewer;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Logging;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Represents a basic info node with handle and description. 
    /// </summary>
    internal class BasicNode : Node
    {
        public override string mType { get; } = "link";

        public BasicNode() : base()
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
