using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using QuickGraph.Serialization;
using static BozjaBuddy.Data.Location;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// ref material: https://git.anna.lgbt/ascclemens/QuestMap/src/branch/main/QuestMap/PluginUi.cs
    /// </summary>
    public class NodeGraphViewer : IDisposable
    {
        private const float kUnitGridSmall_Default = 10;
        private const float kUnitGridLarge_Default = 50;
        public const float kGridSnapProximity = 3.5f;
        private const float kRulerTextFadePeriod = 2500;

        private Plugin mPlugin;
        private readonly Dictionary<int, NodeCanvas> _canvases = new();
        private readonly List<int> _canvasOrder = new();
        private int _canvasCounter = 0;
        private NodeCanvas mActiveCanvas;
        private float mUnitGridSmall = NodeGraphViewer.kUnitGridSmall_Default;
        private float mUnitGridLarge = NodeGraphViewer.kUnitGridLarge_Default;

        private bool _isMouseHoldingViewer = false;
        private bool _isShowingRulerText = false;
        private DateTime? _rulerTextLastAppear = null;
        public Vector2? mSize = null;

        public NodeGraphViewer(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.AddCanvas();
            this.mActiveCanvas = this.GetTopCanvas();
        }

        private void AddCanvas()
        {
            NodeCanvas t = new(this.mPlugin, this._canvasCounter + 1);
            this._canvases.Add(t.mId, t);
            this._canvasOrder.Add(t.mId);
            this._canvasCounter++;
        }
        private NodeCanvas GetCanvas(int pId) => this._canvases[pId];
        private NodeCanvas GetTopCanvas() => this.GetCanvas(this._canvasOrder.First());
        private bool RemoveCanvas(int pCanvasId)
        {
            if (this._canvasOrder.Count == 1) return false;
            if (!this._canvases.Remove(pCanvasId)) return false;
            this._canvasOrder.Remove(pCanvasId);
            if (this.mActiveCanvas.mId == pCanvasId) this.mActiveCanvas = this.GetTopCanvas();
            return true;
        }



        public void Draw()
        {
            Draw(ImGui.GetCursorScreenPos());
        }
        public void Draw(Vector2 pScreenPos, Vector2? pSize = null)
        {
            Area tGraphArea = new(pScreenPos + new Vector2(0, 30), (pSize ?? ImGui.GetContentRegionAvail()) + new Vector2(0, -30));

            this.DrawUtilsBar(tGraphArea.size);
            ImGui.SetCursorScreenPos(tGraphArea.start);
            this.DrawGraph(tGraphArea);
        }
        private void DrawUtilsBar(Vector2 pViewerSize)
        {
            Utils.AlignRight(ImGui.GetWindowWidth() / 2 + 18 + ImGui.GetStyle().ItemSpacing.X);
            // Button scaling
            float tScaling = this.mActiveCanvas.GetScaling();
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2);
            if (ImGui.InputFloat("##sliderScale", ref tScaling, NodeCanvas.stepScale, NodeCanvas.stepScale * 2))
            {
                this.mActiveCanvas.SetScaling(tScaling);
            }
            // Button add node (within view)
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus))
            {
                Node.NodeContent tContent = new("New node");
                this.mActiveCanvas.AddNodeWithinView<NodeAuxiliary>(tContent, pViewerSize);
            }            
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

            var tSnapData = DrawGraphBg(pGraphArea, this.mActiveCanvas.GetBaseOffset(), this.mActiveCanvas.GetScaling());
            DrawGraphNodes(pGraphArea, tSnapData);

            ImGui.EndChild();
            pDrawList.PopClipRect();
        }
        private void DrawGraphNodes(Area pGraphArea, GridSnapData pSnapData)
        {
            ImGui.SetCursorScreenPos(pGraphArea.start);
            
            // check if mouse within viewer, and if mouse is holding on viewer.
            UtilsGUI.InputPayload tInputPayload = new();
            tInputPayload.CaptureInput();
            bool tIsWithinViewer = pGraphArea.CheckPosIsWithin(tInputPayload.mMousePos);
            this._isMouseHoldingViewer = tInputPayload.mIsMouseLmbDown && (tIsWithinViewer || this._isMouseHoldingViewer);
            
            if (tIsWithinViewer) { tInputPayload.CaptureMouseWheel(); }
            if (this._isMouseHoldingViewer) { tInputPayload.CaptureMouseDragDelta(); }

            CanvasDrawFlags tRes = this.mActiveCanvas.Draw(
                                    pGraphArea.center,
                                    -1 * pGraphArea.size / 2,
                                    tInputPayload,
                                    pSnapData,
                                    pCanvasDrawFlag: (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && (this._isMouseHoldingViewer || tIsWithinViewer))
                                                     ? CanvasDrawFlags.None
                                                     : CanvasDrawFlags.NoInteract
                                    );
            if (tRes.HasFlag(CanvasDrawFlags.StateNodeDrag) || tRes.HasFlag(CanvasDrawFlags.StateCanvasDrag))
            {
                this._isShowingRulerText = true;
                this._rulerTextLastAppear = DateTime.Now;
            }
            // Snap lines
            if (!tRes.HasFlag(CanvasDrawFlags.NoNodeSnap)) this.DrawSnapLine(pGraphArea, pSnapData);
        }
        private GridSnapData DrawGraphBg(Area pArea, Vector2 pOffset, float pCanvasScale)
        {
            GridSnapData tGridSnap = new();
            float tUGSmall = this.mUnitGridSmall * pCanvasScale;
            float tUGLarge = this.mUnitGridLarge * pCanvasScale;
            ImGui.SetCursorScreenPos(pArea.start);
            var pDrawList = ImGui.GetWindowDrawList();

            // Grid only adjusts to half of viewer size change,
            // When the viewer's size change, its midpoint only moves a distance of half the size change.
            // The canvas is anchored/offseted to the midpoint of viewer. Hence the canvas also moves half of size change.
            // And the grid should move along with the canvas (grid displays canvas's plane afterall, not the viewer),
            // honestly good luck with this.
            Vector2 tGridStart_S = pArea.start + new Vector2(
                        ((pOffset.X * pCanvasScale + (pArea.size.X * 0.5f)) % tUGSmall),      
                        ((pOffset.Y * pCanvasScale + (pArea.size.Y * 0.5f)) % tUGSmall)       
                    );
            Vector2 tGridStart_L = pArea.start + new Vector2(
                        ((pOffset.X * pCanvasScale + (pArea.size.X * 0.5f)) % tUGLarge),
                        ((pOffset.Y * pCanvasScale + (pArea.size.Y * 0.5f)) % tUGLarge)
                    );

            // backdrop
            pDrawList.AddRectFilled(pArea.start, pArea.end, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalBar_Grey));

            // grid
            uint tGridColor = ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NormalBar_Grey, 0.1f));
            for (var i = 0; i < (pArea.end.X - tGridStart_S.X) / tUGSmall; i++)        // vertical S
            {
                pDrawList.AddLine(new Vector2(tGridStart_S.X + i * tUGSmall, pArea.start.Y), new Vector2(tGridStart_S.X + i * tUGSmall, pArea.end.Y), tGridColor, 1.0f);
            }
            for (var i = 0; i < (pArea.end.Y - tGridStart_S.Y) / tUGSmall; i++)        // horizontal S
            {
                pDrawList.AddLine(new Vector2(pArea.start.X, tGridStart_S.Y + i * tUGSmall), new Vector2(pArea.end.X, tGridStart_S.Y + i * tUGSmall), tGridColor, 1.0f);
            }

            int tXFirstNotation = (int)(-pOffset.X * pCanvasScale - pArea.size.X / 2) / (int)tUGLarge * (int)this.mUnitGridLarge;
            int tYFirstNotation = (int)(-pOffset.Y * pCanvasScale - pArea.size.Y / 2) / (int)tUGLarge * (int)this.mUnitGridLarge;
            for (var i = 0; i < (pArea.end.X - tGridStart_L.X) / tUGLarge; i++)        // vertical L
            {
                pDrawList.AddLine(new Vector2(tGridStart_L.X + i * tUGLarge, pArea.start.Y), new Vector2(tGridStart_L.X + i * tUGLarge, pArea.end.Y), tGridColor, 2.0f);
                tGridSnap.X.Add(tGridStart_L.X + i * tUGLarge);
                if (this._isShowingRulerText)
                {
                    float tTrans = 1;
                    if (this._rulerTextLastAppear.HasValue)
                        tTrans = 1 - ((float)((DateTime.Now - this._rulerTextLastAppear.Value).TotalMilliseconds) / NodeGraphViewer.kRulerTextFadePeriod);
                    pDrawList.AddText(
                        new Vector2(tGridStart_L.X + i * tUGLarge, pArea.start.Y),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeText, tTrans)),
                        $"{(tXFirstNotation + (this.mUnitGridLarge * i)) / 10}");
                    // fade check
                    if (tTrans < 0.05f)
                    {
                        this._rulerTextLastAppear = null;
                        this._isShowingRulerText = false;
                    }
                }
            }
            for (var i = 0; i < (pArea.end.Y - tGridStart_L.Y) / tUGLarge; i++)        // horizontal L
            {
                pDrawList.AddLine(new Vector2(pArea.start.X, tGridStart_L.Y + i * tUGLarge), new Vector2(pArea.end.X, tGridStart_L.Y + i * tUGLarge), tGridColor, 2.0f);
                tGridSnap.Y.Add(tGridStart_L.Y + i * tUGLarge);
                if (this._isShowingRulerText)
                {
                    float tTrans = 1;
                    if (this._rulerTextLastAppear.HasValue)
                        tTrans = 1 - ((float)((DateTime.Now - this._rulerTextLastAppear.Value).TotalMilliseconds) / NodeGraphViewer.kRulerTextFadePeriod);
                    pDrawList.AddText(
                        new Vector2(pArea.start.X + 6, tGridStart_L.Y + i * tUGLarge),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeText, tTrans)),
                        $"{tYFirstNotation + (this.mUnitGridLarge * i)}");
                    // fade check
                    if (tTrans < 0.05f)
                    {
                        this._rulerTextLastAppear = null;
                        this._isShowingRulerText = false;
                    }
                }
            }

            return tGridSnap;
        }
        private void DrawSnapLine(Area pGraphArea, GridSnapData pSnapData)
        {
            var pDrawList = ImGui.GetWindowDrawList();
            // X
            if (pSnapData.lastClosestSnapX != null)
            {
                pDrawList.AddLine(
                    new Vector2(pSnapData.lastClosestSnapX.Value, pGraphArea.start.Y), 
                    new Vector2(pSnapData.lastClosestSnapX.Value, pGraphArea.end.Y), 
                    ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeGraphViewer_SnaplineGold, 0.5f)),
                    1.0f);
            }
            // Y
            if (pSnapData.lastClosestSnapY != null)
            {
                pDrawList.AddLine(
                    new Vector2(pGraphArea.start.X, pSnapData.lastClosestSnapY.Value),
                    new Vector2(pGraphArea.end.X, pSnapData.lastClosestSnapY.Value),
                    ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeGraphViewer_SnaplineGold, 0.5f)),
                    1.0f);
            }
        }

        public void Dispose()
        {
            this.mActiveCanvas.Dispose();
        }


        public class GridSnapData
        {
            public List<float> X = new();
            public List<float> Y = new();
            public float? lastClosestSnapX = null;
            public float? lastClosestSnapY = null;

            public void AddUsingPos(Vector2 pos)
            {
                this.X.Add(pos.X);
                this.Y.Add(pos.Y);
            }
            public Vector2 GetClosestSnapPos(Vector2 currPos, float proximity)
            {
                var tXClosest = Utils.GetClosestItem(currPos.X, this.X);
                var tYClosest = Utils.GetClosestItem(currPos.Y, this.Y);
                var x = tXClosest ?? currPos.X;
                if (Math.Abs(x - currPos.X) > proximity)
                {
                    x = currPos.X;
                    this.lastClosestSnapX = null;
                }
                else if (tXClosest.HasValue) this.lastClosestSnapX = x;
                var y = tYClosest ?? currPos.Y;
                if (Math.Abs(y - currPos.Y) > proximity)
                {
                    y = currPos.Y;
                    this.lastClosestSnapY = null;
                }
                else if (tYClosest.HasValue) this.lastClosestSnapY = y;
                return new(x, y);
            }
        }
    }
}
