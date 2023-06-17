using System.Numerics;
using ImGuiNET;
using BozjaBuddy.GUI.NodeGraphViewer;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Logging;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    internal class NodeLink : Node
    {
        public override string mType { get; } = "link";

        public NodeLink() { }
        protected override InputFlag DrawBody(Vector2 pNodeOSP, float pCanvasScaling)
        {
            return InputFlag.None;
        }

        public override void Dispose()
        {

        }
    }
}
