using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;
using System.Xml.XPath;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using BozjaBuddy.GUI.NodeGraphViewer.utils;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
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

        [JsonProperty]
        private readonly Dictionary<int, NodeCanvas> _canvases = new();
        [JsonProperty]
        private readonly List<int> _canvasOrder = new();
        [JsonProperty]
        private int _canvasCounter = 0;
        [JsonProperty]
        private NodeCanvas mActiveCanvas;
        private float mUnitGridSmall = NodeGraphViewer.kUnitGridSmall_Default;
        private float mUnitGridLarge = NodeGraphViewer.kUnitGridLarge_Default;
        private ViewerNotificationManager mNotificationManager = new();

        private bool _isMouseHoldingViewer = false;
        private bool _isShowingRulerText = false;
        private DateTime? _rulerTextLastAppear = null;
        public Vector2? mSize = null;

        private string? _debugViewerJson = null;
        private Plugin? _plugin = null;

        public NodeGraphViewer()
        {
            this.AddBlankCanvas();
            this.mActiveCanvas = this.GetTopCanvas()!;
        }
        /// <summary>For debugging Bozja buddy related stuff only</summary>
        public NodeGraphViewer(Plugin pPlugin) : this()
        {
            this._plugin = pPlugin;
        }

        private void AddBlankCanvas()
        {
            NodeCanvas t = new(this._canvasCounter + 1);
            this._canvases.Add(t.mId, t);
            this._canvasOrder.Add(t.mId);
            this._canvasCounter++;
        }
        private bool AddCanvas(string pCanvasJson)
        {
            var tCanvas = JsonConvert.DeserializeObject<NodeCanvas>(pCanvasJson, new utils.JsonConverters.NodeJsonConverter());
            if (tCanvas == null) return false;
            return this.AddCanvas(tCanvas);
        }
        private bool AddCanvas(NodeCanvas pCanvas)
        {
            pCanvas.mId = this._canvasCounter + 1;
            this._canvases.Add(pCanvas.mId, pCanvas);
            this._canvasOrder.Add(pCanvas.mId);
            this._canvasCounter++;
            return true;
        }
        private NodeCanvas? GetCanvas(int pId)
        {
            if (!this._canvases.TryGetValue(pId, out var tCanvas)) return null;
            return tCanvas;
        }
        private NodeCanvas? GetTopCanvas() => this._canvasOrder.Count == 0 ? null : this.GetCanvas(this._canvasOrder.First());
        private bool RemoveCanvas(int pCanvasId)
        {
            if (this._canvasOrder.Count == 1) return false;
            if (!this._canvases.Remove(pCanvasId)) return false;
            this._canvasOrder.Remove(pCanvasId);
            var tTopCanvas = this.GetTopCanvas();
            if (tTopCanvas == null) this.AddBlankCanvas();
            if (this.mActiveCanvas.mId == pCanvasId) this.mActiveCanvas = tTopCanvas ?? this.GetTopCanvas()!;
            return true;
        }
        /// <summary>'Deep-copy' given canvas, and add it to the viewer with a new id.</summary>
        public bool ImportCanvas(NodeCanvas pCanvas)
        {
            return this.ImportCanvas(JsonConvert.SerializeObject(pCanvas));
        }
        /// <summary>Add new canvas to viewer using JSON. Return false if the deserialization fails, otherwise true.</summary>
        public bool ImportCanvas(string pCanvasJson)
        {
            return this.AddCanvas(pCanvasJson);
        }
        public string? ExportCanvasAsJson(int pCanvasId)
        {
            var pCanvas = this.GetCanvas(pCanvasId);
            return pCanvas == null ? null : JsonConvert.SerializeObject(pCanvas);
        }
        public string ExportActiveCanvasAsJson()
        {
            return JsonConvert.SerializeObject(this.mActiveCanvas);
        }



        public void Draw()
        {
            Draw(ImGui.GetCursorScreenPos());
        }
        public void Draw(Vector2 pScreenPos, Vector2? pSize = null)
        {
            Area tGraphArea = new(pScreenPos + new Vector2(0, 30), (pSize ?? ImGui.GetContentRegionAvail()) + new Vector2(0, -30));
            var tDrawList = ImGui.GetWindowDrawList();

            this.DrawUtilsBar(tGraphArea.size);
            ImGui.SetCursorScreenPos(tGraphArea.start);
            this.DrawGraph(tGraphArea, tDrawList);
        }
        private void DrawUtilsBar(Vector2 pViewerSize)
        {
            // =======================================================
            // DEBUG =================================================
            if (ImGui.Button("Cache viewer"))
            {
                var tRes = this.ExportActiveCanvasAsJson();
                this._debugViewerJson = tRes;
            }
            ImGui.SameLine();
            if (this._debugViewerJson == null) ImGui.BeginDisabled();
            if (ImGui.Button("Load json from cache") && this._debugViewerJson != null)
            {
                var tRes = JsonConvert.DeserializeObject<NodeCanvas>(this._debugViewerJson, new utils.JsonConverters.NodeJsonConverter());
            }
            if (this._debugViewerJson == null) ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Notify!"))
            {
                this.mNotificationManager.Push(new ViewerNotification("viewerNoti", "INFO! viewer's notification button\n... or is it?"));
                this.mNotificationManager.Push(new ViewerNotification("viewerNoti2", "WARNING! viewer's notification button pressed!aaaaaaaaaa\n... or is it?", ViewerNotificationType.Warning));
                this.mNotificationManager.Push(new ViewerNotification("viewerNoti3", "ERROR! viewer's notification button\n... or is it?", ViewerNotificationType.Error));
            }
            ImGui.SameLine();
            // DEBUG =================================================
            // =======================================================

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
                //NodeContent.NodeContent tContent = new("New node");
                //this.mActiveCanvas.AddNodeWithinView<AuxNode>(tContent, pViewerSize);
                BBNodeContent tContent = new(this._plugin, 400005, "Lost Banner of Xyz");
                this.mActiveCanvas.AddNodeWithinView<AuxNode>(tContent, pViewerSize);
            }            
        }
        private void DrawGraph(Area pGraphArea, ImDrawListPtr pDrawList)
        {
            List<ViewerNotification> tNotiListener = new();
            ImGui.BeginChild(
                "nodegraphviewer",
                pGraphArea.size, 
                border: true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove);
            pDrawList.PushClipRect(pGraphArea.start, pGraphArea.end, true);

            var tSnapData = DrawGraphBg(pGraphArea, this.mActiveCanvas.GetBaseOffset(), this.mActiveCanvas.GetScaling());
            DrawGraphNodes(pGraphArea, tSnapData, pDrawList, pNotiListener: tNotiListener);
            this.mNotificationManager.Push(tNotiListener);
            DrawNotifications(pGraphArea);

            ImGui.EndChild();
            pDrawList.PopClipRect();
        }
        private void DrawNotifications(Area pGraphArea)
        {
            int tCounter = 1;
            Vector2 tNotifBoxPadding = new(5, 5);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, tNotifBoxPadding);

            foreach (var n in this.mNotificationManager.GetNotifications())
            {
                if (!n.contentImguiSize.HasValue) n.contentImguiSize = ImGui.CalcTextSize(n.content);
                float tTransparency = (float)n.GetTimeLeft() / n.duration;

                ImGui.SetCursorScreenPos(
                    new Vector2(
                        pGraphArea.end.X - (n.contentImguiSize.Value.X + tNotifBoxPadding.X), 
                        pGraphArea.end.Y - (n.contentImguiSize.Value.Y + tNotifBoxPadding.Y * 1.25f) * tCounter
                        )
                    + new Vector2(-10, -5 * tCounter));

                ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_Black, tTransparency)));
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(n.type switch
                                                                                                    {
                                                                                                        ViewerNotificationType.Warning => UtilsGUI.Colors.NodeNotifWarning,
                                                                                                        ViewerNotificationType.Error => UtilsGUI.Colors.NodeNotifError,
                                                                                                        _ => UtilsGUI.Colors.NodeNotifInfo,
                                                                                                    },
                                                                                                    tTransparency)));
                ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(
                                                                                                    n.type switch { 
                                                                                                        ViewerNotificationType.Warning => UtilsGUI.Colors.NodeNotifWarning,
                                                                                                        ViewerNotificationType.Error => UtilsGUI.Colors.NodeNotifError,
                                                                                                        _ => UtilsGUI.Colors.NodeNotifInfo
                                                                                                    },
                                                                                                    tTransparency)));
                ImGui.BeginChild(n.id, n.contentImguiSize.Value + tNotifBoxPadding * 2, border: true, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

                ImGui.Text(n.content);
                // extend expiration on hover
                if (ImGui.IsItemHovered()) n.Renew();

                ImGui.EndChild();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                tCounter++;
            }
            ImGui.PopStyleVar();
        }
        private void DrawGraphNodes(Area pGraphArea, GridSnapData pSnapData, ImDrawListPtr pDrawList, List<ViewerNotification>? pNotiListener = null)
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
                                    pDrawList,
                                    pSnapData: pSnapData,
                                    pCanvasDrawFlag: (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && (this._isMouseHoldingViewer || tIsWithinViewer))
                                                     ? CanvasDrawFlags.None
                                                     : CanvasDrawFlags.NoInteract,
                                    pNotiListener: pNotiListener
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
            float tTransMax = 0.3f;
            for (var i = 0; i < (pArea.end.X - tGridStart_L.X) / tUGLarge; i++)        // vertical L
            {
                pDrawList.AddLine(new Vector2(tGridStart_L.X + i * tUGLarge, pArea.start.Y), new Vector2(tGridStart_L.X + i * tUGLarge, pArea.end.Y), tGridColor, 2.0f);
                tGridSnap.X.Add(tGridStart_L.X + i * tUGLarge);
                if (this._isShowingRulerText)
                {
                    float tTrans = 1;
                    if (this._rulerTextLastAppear.HasValue)
                        tTrans = tTransMax - ((float)((DateTime.Now - this._rulerTextLastAppear.Value).TotalMilliseconds) / NodeGraphViewer.kRulerTextFadePeriod) * tTransMax;
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
                        tTrans = tTransMax - ((float)((DateTime.Now - this._rulerTextLastAppear.Value).TotalMilliseconds) / NodeGraphViewer.kRulerTextFadePeriod) * tTransMax;
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
