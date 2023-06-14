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

        protected override InputFlag DrawInternal(Vector2 pNodeOSP, float pCanvasScaling)
        {
            ImGui.Text(this.GetHeader());

            return InputFlag.None;
        }

        public override void AdjustSizeToContent()
        {
            this.mStyle.size = ImGui.CalcTextSize(this.mContent.header) + Node.nodePadding * 2;
            //PluginLog.LogDebug($"> Size adjusted: {this.mStyle.size.X} {this.mStyle.size.Y} FontSize: {ImGui.GetFontSize()} == {ImGui.GetFont().FontSize}");
        }

        public override void Dispose()
        {

        }
    }
}
