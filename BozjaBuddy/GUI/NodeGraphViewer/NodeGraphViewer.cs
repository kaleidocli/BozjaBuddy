using System.Dynamic;
using System.Numerics;
using BozjaBuddy.Utils;
using Dalamud.Logging;
using ImGuiNET;
using static BozjaBuddy.Data.Location;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// ref material: https://git.anna.lgbt/ascclemens/QuestMap/src/branch/main/QuestMap/PluginUi.cs
    /// </summary>
    public class NodeGraphViewer
    {
        private const float kUnitGridSmall_Default = 10;
        private const float kUnitGridLarge_Default = 50;

        public Vector2? mSize = null;
        private NodeCanvas mNodeCanvas;
        private float mUnitGridSmall = 10;
        private float mUnitGridLarge = 50;

        private bool _isMouseHoldingViewer = false;

        public NodeGraphViewer()
        {
            this.mNodeCanvas = new();
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 1");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 22");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 333");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 4");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 5555555555");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 6");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 777777777777777");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 888");
        }

        public void Draw()
        {
            Draw(ImGui.GetCursorScreenPos());
        }
        public void Draw(Vector2 pPos, Vector2? pSize = null)
        {
            Area tGraphArea = new(pPos, pSize ?? ImGui.GetContentRegionAvail());
            this.DrawGraph(tGraphArea);
        }

        private void DrawGraph(Area pGraphArea)
        {
            var pDrawList = ImGui.GetWindowDrawList();
            ImGui.BeginChild(
                "nodegraphviewer",
                pGraphArea.size, 
                border: true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove);
            pDrawList.PushClipRect(pGraphArea.start, pGraphArea.end, true);

            DrawGraphBg(pGraphArea);
            DrawGraphNodes(pGraphArea);

            ImGui.EndChild();
            pDrawList.PopClipRect();
        }
        private void DrawGraphNodes(Area pGraphArea)
        {
            ImGui.SetCursorScreenPos(pGraphArea.start);

            // check if mouse within viewer, and if mouse is holding on viewer.
            UtilsGUI.InputPayload tInputPayload = new();
            tInputPayload.CaptureInput();
            bool tIsWithinViewer = pGraphArea.CheckPosIsWithin(tInputPayload.mMousePos);
            this._isMouseHoldingViewer = (tIsWithinViewer || this._isMouseHoldingViewer) && tInputPayload.mIsMouseLmbDown;
            //PluginLog.LogDebug($"> inView={tIsWithinViewer} holdView={this._isMouseHoldingViewer} lmbDown={tInputPayload.mIsMouseLmbDown}");
            if (tIsWithinViewer) { tInputPayload.CaptureMouseWheel(); }
            if (this._isMouseHoldingViewer) { tInputPayload.CaptureMouseDragDelta(); }

            this.mNodeCanvas.Draw(
                pGraphArea.start, 
                tInputPayload
                );
        }
        private void DrawGraphBg(Area pArea)
        {
            ImGui.SetCursorScreenPos(pArea.start);
            var pDrawList = ImGui.GetWindowDrawList();
            // backdrop
            pDrawList.AddRectFilled(pArea.start, pArea.end, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalBar_Grey));
            // grid
            uint tGridColor = ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalBar_Grey);
            for (var i = 0; i < pArea.size.X / mUnitGridSmall; i++)
            {
                pDrawList.AddLine(new Vector2(pArea.start.X + i * mUnitGridSmall, pArea.start.Y), new Vector2(pArea.start.X + i * mUnitGridSmall, pArea.end.Y), tGridColor, 1.0f);
            }

            for (var i = 0; i < pArea.size.Y / mUnitGridSmall; i++)
            {
                pDrawList.AddLine(new Vector2(pArea.start.X, pArea.start.Y + i * mUnitGridSmall), new Vector2(pArea.end.X, pArea.start.Y + i * mUnitGridSmall), tGridColor, 1.0f);
            }

            for (var i = 0; i < pArea.size.X / mUnitGridLarge; i++)
            {
                pDrawList.AddLine(new Vector2(pArea.start.X + i * mUnitGridLarge, pArea.start.Y), new Vector2(pArea.start.X + i * mUnitGridLarge, pArea.end.Y), tGridColor, 2.0f);
            }

            for (var i = 0; i < pArea.size.Y / mUnitGridLarge; i++)
            {
                pDrawList.AddLine(new Vector2(pArea.start.X, pArea.start.Y + i * mUnitGridLarge), new Vector2(pArea.end.X, pArea.start.Y + i * mUnitGridLarge), tGridColor, 2.0f);
            }
        }
     
    }
}
