using System.Numerics;
using ImGuiNET;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Logging;
using BozjaBuddy.GUI.NodeGraphViewer.utils;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Represents a basic info node with handle and description. 
    /// </summary>
    internal class BasicNode : Node
    {
        public new const string nodeType = "BasicNode";
        public override string mType { get; } = BasicNode.nodeType;

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
