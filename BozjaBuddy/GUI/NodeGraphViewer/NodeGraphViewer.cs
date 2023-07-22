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
using Dalamud.Interface;
using System.Linq.Expressions;
using System.Reflection.Emit;
using BozjaBuddy.Data;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// ref material: https://git.anna.lgbt/ascclemens/QuestMap/src/branch/main/QuestMap/PluginUi.cs
    /// </summary>
    public class NodeGraphViewer : IDisposable
    {
        private const float kUnitGridSmall_Default = 10;
        private const float kUnitGridLarge_Default = 50;
        private const float kGridSnapProximity_Default = 3.5f;
        private const float kRulerTextFadePeriod_Default = 2500;
        private static Vector2 kRecommendedViewerSizeToSearch = new Vector2(200, 300);

        [JsonProperty]
        private Dictionary<int, NodeCanvas> _canvases = new();
        [JsonProperty]
        private List<int> _canvasOrder = new();
        private int _canvasCounter = 0;
        private bool _fistLoaded = true;
        private NodeCanvas mActiveCanvas;
        private ViewerNotificationManager mNotificationManager = new();
        [JsonProperty]
        private Dictionary<string, int> _tagAndCanvasId = new();
        [JsonProperty]
        private Dictionary<int, HashSet<string>> _canvasIdAndTags = new();       // a node can have multiple tags
        private bool _activeCanvasDiffSwitchReq = false;

        private bool _isMouseHoldingViewer = false;
        private bool _isShowingRulerText = false;
        private DateTime? _rulerTextLastAppear = null;
        private int _lastSelectedCount = 0;    // is shared between canvases of this viewer
        private bool _minimizeFuncState = false;
        private ViewerEventFlag _eventFlags = ViewerEventFlag.None;
        public Vector2? mSize = null;

        private string? _infield_CanvasName = null;
        private string _infield_NodeLookupVal = "";

        private Queue<Tuple<DateTime, string>> _saveData = new();
        private DateTime _lastTimeAutoSave = DateTime.MinValue;

        [JsonProperty]
        private NodeGraphViewerConfig mConfig = new()
        {
            unitGridSmall = NodeGraphViewer.kUnitGridSmall_Default,
            unitGridLarge = NodeGraphViewer.kUnitGridLarge_Default,
            gridSnapProximity = NodeGraphViewer.kGridSnapProximity_Default,
            timeForRulerTextFade = NodeGraphViewer.kRulerTextFadePeriod_Default,
            showRulerText = true
        };

        private NodeGraphViewer() { }
        /// <summary> Create a viewer with given data. If not given, create a fresh one. </summary>
        public NodeGraphViewer(string? dataJson)
        {
            if (dataJson != null || dataJson == string.Empty) this.LoadSaveData(dataJson);
            if (this.GetTopCanvas() == null)
            {
                this.AddBlankCanvas();
            }
            this.mActiveCanvas = this.GetTopCanvas()!;
        }

        private void AddBlankCanvas()
        {
            NodeCanvas t = new(this._canvasCounter + 1);
            this._canvases.Add(t.mId, t);
            this._canvasOrder.Add(t.mId);
            this._canvasCounter++;
        }
        /// <summary> 
        /// <para> tag:         add a canvas with tag. </para>
        /// <para> noInitOfs:   By default, canvas will offset itself to align (0, 0) at the center of viewer. </para>
        /// <para> Return null if there is already a canvas with the same tag. Otherwise, return the newly added canvas' id. </para>
        /// </summary>
        private int? AddCanvas(string pCanvasJson, string? pTag = null, bool noInitOfs = false)
        {
            var tCanvas = JsonConvert.DeserializeObject<NodeCanvas>(pCanvasJson, new utils.JsonConverters.NodeCanvasJsonConverter());
            if (tCanvas == null) return null;
            return this.AddCanvas(tCanvas, pTag, noInitOfs: noInitOfs);
        }
        /// <summary> 
        /// <para> tag:         add a canvas with tag. </para>
        /// <para> noInitOfs:   By default, canvas will offset itself to align (0, 0) at the center of viewer. </para>
        /// <para> Return null if there is already a canvas with the same tag. Otherwise, return the newly added canvas' id. </para>
        /// </summary>
        private int? AddCanvas(NodeCanvas pCanvas, string? pTag = null, bool noInitOfs = false)
        {
            // check if canvas with similar tag already exist
            if (pTag != null && this._tagAndCanvasId.ContainsKey(pTag)) return null;
            // reassign canvas IDs every first time viewer is loaded to cache
            if (this._fistLoaded)
            {
                this._canvasCounter = 0;
                List<int> tNewCanvasOrder = new();
                Dictionary<int, NodeCanvas> tNewCanvases = new();
                // assign new id to canvasses
                foreach (var id in this._canvasOrder)
                {
                    if (this._canvases.TryGetValue(id, out var tCanvas) && tCanvas != null)
                    {
                        tCanvas.mId = this._canvasCounter++;
                        // assign new canvas to the new colection
                        tNewCanvases.Add(tCanvas.mId, tCanvas);
                        // assign the new id into the new order
                        tNewCanvasOrder.Add(tCanvas.mId);
                    }
                }
                this._canvases = tNewCanvases;
                this._canvasOrder = tNewCanvasOrder;
                this._fistLoaded = false;
            }

            pCanvas.mId = this._canvasCounter + 1;
            this._canvases.Add(pCanvas.mId, pCanvas);
            this._canvasOrder.Add(pCanvas.mId);
            this._canvasCounter++;
            if (pTag != null) this.TagCanvas(pCanvas.mId, pTag);
            if (noInitOfs) pCanvas.MarkUnneedInitOfs();
            return pCanvas.mId;
        }
        private NodeCanvas? GetCanvas(int pId)
        {
            if (!this._canvases.TryGetValue(pId, out var tCanvas)) return null;
            return tCanvas;
        }
        private NodeCanvas? GetTopCanvas() => this._canvasOrder.Count == 0 ? null : this.GetCanvas(this._canvasOrder.First());
        private int? GetCanvasIdWithTag(string pTag) => this._tagAndCanvasId.TryGetValue(pTag, out var tCanvasId) ? tCanvasId : null;
        /// <summary>'Deep-copy' given canvas, and add it to the viewer with a new id. Return null if the deserialization fails, otherwise return the newly imported canvas' id.</summary>
        private int? ImportCanvas(NodeCanvas pCanvas, string? pTag = null)
        {
            return this.ImportCanvas(JsonConvert.SerializeObject(pCanvas), pTag);
        }
        /// <summary>
        /// <para> Add new canvas to viewer using JSON. Return null if the deserialization fails, otherwise return the newly imported canvas' id. </para>
        /// <para> If tag is given, return null if a canvas with the same tag already exists. </para>
        /// <para> noInitOfs:   By default, canvas will offset itself to align (0, 0) at the center of viewer. </para>
        /// </summary>
        public int? ImportCanvas(string pCanvasJson, string? pTag = null, bool noInitOfs = false)
        {
            return this.AddCanvas(pCanvasJson, pTag, noInitOfs: noInitOfs);
        }
        /// <summary> Returns false if canvasId is not found, otherwise true. </summary>
        private string? ExportCanvasAsJson(int pCanvasId)
        {
            var pCanvas = this.GetCanvas(pCanvasId);
            return pCanvas == null ? null : JsonConvert.SerializeObject(pCanvas);
        }
        private string ExportActiveCanvasAsJson()
        {
            return JsonConvert.SerializeObject(this.mActiveCanvas, Formatting.Indented);
        }
        /// <summary> forceSave: Save without interval check. Be advised when using this.</summary>
        private bool Save(bool _forceSave = false)
        {
            // Check interaction interval
            if (!_forceSave && (DateTime.Now - this._lastTimeAutoSave).TotalMilliseconds < this.mConfig.autoSaveInterval) return false;

            // Save
            var json = JsonConvert.SerializeObject(this);
            if (json == null) return false;
            this._saveData.Enqueue(new(DateTime.Now, json));
            while (this._saveData.Count > this.mConfig.saveCapacity) this._saveData.Dequeue();

            this._lastTimeAutoSave = DateTime.Now;
            this._eventFlags |= ViewerEventFlag.NewSaveDataAvailable;

            this.mNotificationManager.Push(new ViewerNotification("sysSaveNotiSucess", "Successfully saved!"));
            return true;
        }
        private Tuple<DateTime, string>? GetLatestSaveData() => this._saveData.Count == 0 ? null : this._saveData.Dequeue();
        // The difference between this and GetLatestSaveData() is that this method will only retrieve and return the save data if there is a flag.
        /// <summary> 
        /// Returns null if there is no new save data flagged by the viewer. Otherwise, return the latest save data AND remove the flag. 
        /// </summary>
        public Tuple<DateTime, string>? GetLatestSaveDataSinceLastChange()
        {
            if (!this._eventFlags.HasFlag(ViewerEventFlag.NewSaveDataAvailable)) return null;
            this._eventFlags &= ~ViewerEventFlag.NewSaveDataAvailable;
            return this.GetLatestSaveData();
        }
        public bool LoadSaveData(string dataJson)
        {
            var tRes = JsonConvert.DeserializeObject<NodeGraphViewer>(dataJson, new utils.JsonConverters.NodeCanvasJsonConverter());
            if (tRes == null) return false;

            HashSet<int> tLoadedIds = new();
            foreach (var oldId in tRes._canvasOrder)
            {
                if (tLoadedIds.Contains(oldId)) continue;      // prevent loading dupes

                var tCanvasIn = tRes.GetCanvas(oldId);
                if (tCanvasIn == null) continue;
                var tNewId = this.AddCanvas(tCanvasIn);
                if (tNewId != null && tRes._canvasIdAndTags.TryGetValue(oldId, out var tags))
                {
                    foreach (var tag in tags)
                    {
                        this._tagAndCanvasId.TryAdd(tag, tNewId.Value);
                    }
                    this._canvasIdAndTags.TryAdd(tNewId.Value, tags);
                }
            }
            this.mConfig = tRes.mConfig;
            if (this.GetTopCanvas() == null) this.AddBlankCanvas();
            this.mActiveCanvas = this.GetTopCanvas()!;
            return true;
        }
        /// <summary> Tag a canvas. Return false if tag already exists, or id not found. </summary>
        private bool TagCanvas(int pCanvasId, string pTag)
        {
            if (!this._canvasOrder.Contains(pCanvasId)) return false;
            if (this._tagAndCanvasId.ContainsKey(pTag)) return false;
            this._tagAndCanvasId.TryAdd(pTag, pCanvasId);
            if (this._canvasIdAndTags.ContainsKey(pCanvasId))
            {
                this._canvasIdAndTags[pCanvasId].Add(pTag);
            }
            else
            {
                this._canvasIdAndTags.TryAdd(pCanvasId, new() { pTag });
            }
            return true;
        }
        /// <summary> Untag a canvas. Returns false if canvas doeshn't have any tag. </summary>
        private bool UntagCanvas(int pCanvasId)
        {
            if (this._canvasIdAndTags.TryGetValue(pCanvasId, out var tTags) && tTags != null)
            {
                foreach (var tag in tTags)
                {
                    this._tagAndCanvasId.Remove(tag);
                }
                this._canvasIdAndTags.Remove(pCanvasId);
            }
            else return false;
            return true;
        }
        private bool RemoveCanvas(int pCanvasId)
        {
            if (this._canvasOrder.Count == 1) return false;
            if (!this._canvases.Remove(pCanvasId)) return false;
            this._canvasOrder.Remove(pCanvasId);
            var tTopCanvas = this.GetTopCanvas();
            if (tTopCanvas == null) this.AddBlankCanvas();
            if (this.mActiveCanvas.mId == pCanvasId) this.mActiveCanvas = tTopCanvas ?? this.GetTopCanvas()!;

            // removing canvas from tag's col if exist.
            this.UntagCanvas(pCanvasId);

            return true;
        }
        public ViewerEventFlag GetViewerEventFlags() => this._eventFlags;
        public void AddNodeToActiveCanvas<T>(NodeContent.NodeContent pNodeContent) where T : Node, new()
        {
            var tNodeId = this.mActiveCanvas.AddNodeWithinView<T>(pNodeContent, this.mConfig.sizeLastKnown ?? NodeGraphViewer.kRecommendedViewerSizeToSearch);
        }
        /// <summary> Set a canvas active (focus). Returns false if id not found. </summary>
        public bool SetCanvasActive(int pCanvasId)
        {
            if (this.mActiveCanvas.mId == pCanvasId) return true;
            var c = this.GetCanvas(pCanvasId);
            if (c == null) return false;
            this.SetCanvasActive(c);
            return true;
        }
        private void SetCanvasActive(NodeCanvas pCanvas)
        {
            if (this.mActiveCanvas.mId != pCanvas.mId)
            {
                this._activeCanvasDiffSwitchReq = true;
            }
            this.mActiveCanvas = pCanvas;
        }
        /// <summary> Set active (focus) on a canvas with given tag. Returns false if tag not found. </summary>
        public bool SetCanvasActive(string pTag)
        {
            var tCanvasId = this.GetCanvasIdWithTag(pTag);
            if (tCanvasId == null) return false;
            return this.SetCanvasActive(tCanvasId.Value);
        }

        /// <summary> Draw at current cursor, with size as ContentRegionAvail </summary>
        public void Draw(HashSet<ImGuiKey>? pExtraKeyboardInputs = null)
        {
            Draw(ImGui.GetCursorScreenPos(), pExtraKeyboardInputs: pExtraKeyboardInputs);
        }
        /// <summary> Draw at specified pos, with size as specified </summary>
        public void Draw(Vector2 pScreenPos, Vector2? pSize = null, HashSet<ImGuiKey>? pExtraKeyboardInputs = null)
        {
            this.mConfig.sizeLastKnown = pSize ?? ImGui.GetContentRegionAvail();
            Area tGraphArea = new(pScreenPos + new Vector2(0, 30), (this.mConfig.sizeLastKnown ?? ImGui.GetContentRegionAvail()) + new Vector2(0, -30));
            var tDrawList = ImGui.GetWindowDrawList();

            this.DrawUtilsBar();
            ImGui.SetCursorScreenPos(tGraphArea.start);
            this.DrawGraph(tGraphArea, tDrawList, pExtraKeyboardInputs: pExtraKeyboardInputs);
        }
        private void DrawUtilsBar()
        {
            HashSet<string> tPUReqs = new();
            // Split into 3 parts
            // [Canvasses tab bar and related] | [Viewer related] | [Active canvas related]

            // Canvas tab bar   ================================================
            float tTabBarW = ImGui.GetContentRegionMax().X / 10 * 4.4f - 10;
            if (ImGui.BeginTabBar("##ngvCanvasTabbar", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton))
            {
                List<int> tCanvasToRemove = new();
                foreach (var canvasId in this._canvasOrder)
                {
                    if (!this._canvases.TryGetValue(canvasId, out var c) || c == null) continue;
                    bool isOpened = true;
                    ImGui.SetNextItemWidth((tTabBarW - ImGui.GetStyle().ItemInnerSpacing.X * this._canvasOrder.Count) / this._canvasOrder.Count);
                    if (ImGui.BeginTabItem($"{c.mName}##{c.mId}", ref isOpened, (this._activeCanvasDiffSwitchReq && this.mActiveCanvas.mId == canvasId) ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                    {
                        if (!this._activeCanvasDiffSwitchReq) this.SetCanvasActive(c);
                        else if (this._activeCanvasDiffSwitchReq && this.mActiveCanvas.mId == canvasId) this._activeCanvasDiffSwitchReq = false;
                        ImGui.EndTabItem();
                    }
                    if (!isOpened) tCanvasToRemove.Add(c.mId);
                }
                foreach (var id in tCanvasToRemove) { this.RemoveCanvas(id); }
                if (ImGui.TabItemButton(" +"))
                {
                    this.AddBlankCanvas();
                }
                ImGui.EndTabBar();
            }

            // Extra options =======================================
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker(
                """
                Keybinds basic
                [LMB]                           Select/Drag nodes by the handle.
                [RMB]                           Extra options on the handle.
                [MiddleClick]              Delete a canvas by the handle.
                [MouseScroll]             Zoom in/out on canvas.
                [Crtl + LMB]                Multi-selecting nodes.
                [Shift + LMB drag]     Multi-selecting nodes within an area.
                [Ctrl + C]                      Copy selected nodes to clipboard.
                [Ctrl + V]                      Paste selected nodes to active canvas.

                Basics
                - Nodes can be added, deleted, edited, minimnized, and resized.
                - Nodes can be bundled and moved around, using its plug on top-left corner and LMB.
                - Nodes can be connected to each other, using its plug on top-left corner and LMB.
                - Nodes' connections can be modified in pre-defined path for organizing's sake, using a button in the middle of the connection's line.

                Saving and Auto-save
                - Viewer and all of its canvasses' data can be saved manually.
                - It can also be auto-saved, every X seconds (configurable in config).
                - Auto-save only runs when the viewer is visible.
                - Any adjustments that are not saved will be lost.
                """
                );
            ImGui.SameLine();
            if (ImGui.Button("…"))
            {
                ImGui.OpenPopup("##exgpu");
            }
            else UtilsGUI.SetTooltipForLastItem("Other options");
            if (ImGui.BeginPopup("##exgpu"))
            {
                this.DrawExtraOptions(tPUReqs);
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
            {
                this.Save(_forceSave: true);
            }
            else UtilsGUI.SetTooltipForLastItem("Save viewer's and canvasses' data");

            // Active canvas options ==========================================
            ImGui.SameLine();
            // Slider: Scaling
            int tScaling = (int)(this.mActiveCanvas.GetScaling() * 100);
            ImGui.SetNextItemWidth(35);
            if (ImGui.DragInt("##sldScaling", ref tScaling, NodeCanvas.stepScale * 100, (int)(NodeCanvas.minScale * 100), (int)(NodeCanvas.maxScale * 100), "%d%%"))
            {
                this.mActiveCanvas.SetScaling((float)tScaling / 100);
            }
            // Search bar
            ImGui.SameLine();
            this.DrawNodeSearchBox(pTextBoxWidth: 115);

            // Button: Minimize/Unminimize selected
            ImGui.SameLine();
            if (this.mActiveCanvas.GetSelectedCount() != this._lastSelectedCount)
            {
                this._minimizeFuncState = false;
                this._lastSelectedCount = this.mActiveCanvas.GetSelectedCount();
            }
            if (ImGuiComponents.IconButton(this._minimizeFuncState ? FontAwesomeIcon.WindowMaximize : FontAwesomeIcon.WindowMinimize))
            {
                if (this._minimizeFuncState) this.mActiveCanvas.UnminimizeSelectedNodes();
                else this.mActiveCanvas.MinimizeSelectedNodes();
                this._minimizeFuncState = !this._minimizeFuncState;
            }
            else UtilsGUI.SetTooltipForLastItem(this._minimizeFuncState ? "Unminimize selected nodes" : "Minimize selected nodes");
            // Button: Remove selected
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                this.mActiveCanvas.RemoveSelectedNodes();
            }
            else UtilsGUI.SetTooltipForLastItem("Delete ALL selected nodes [Del]");
            // Button: Add node (within view)
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            {
                this.AddNodeToActiveCanvas<BasicNode>(new NodeContent.NodeContent("New node"));
            }
            else UtilsGUI.SetTooltipForLastItem("Add a basic node");

            // PU Reqs
            if (tPUReqs.Contains("vwexpu_rename")) ImGui.OpenPopup("vwexpu_rename");

            this.DrawViewerExtraOptions_Rename("vwexpu_rename");
        }
        private void DrawGraph(Area pGraphArea, ImDrawListPtr pDrawList, HashSet<ImGuiKey>? pExtraKeyboardInputs = null)
        {
            List<ViewerNotification> tNotiListener = new();
            ImGui.BeginChild(
                "nodegraphviewer",
                pGraphArea.size, 
                border: true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove);
            pDrawList.PushClipRect(pGraphArea.start, pGraphArea.end, true);

            var tSnapData = DrawGraphBg(pGraphArea, this.mActiveCanvas.GetBaseOffset(), this.mActiveCanvas.GetScaling());
            DrawGraphNodes(pGraphArea, tSnapData, pDrawList, pNotiListener: tNotiListener, pExtraKeyboardInputs: pExtraKeyboardInputs);
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
        private void DrawGraphNodes(Area pGraphArea, GridSnapData pSnapData, ImDrawListPtr pDrawList, List<ViewerNotification>? pNotiListener = null, HashSet<ImGuiKey>? pExtraKeyboardInputs = null)
        {
            ImGui.SetCursorScreenPos(pGraphArea.start);
            
            // check if mouse within viewer, and if mouse is holding on viewer.
            UtilsGUI.InputPayload tInputPayload = new();
            tInputPayload.CaptureInput(pExtraKeyboardInputs: pExtraKeyboardInputs);
            bool tIsWithinViewer = pGraphArea.CheckPosIsWithin(tInputPayload.mMousePos);
            this._isMouseHoldingViewer = tInputPayload.mIsMouseLmbDown && (tIsWithinViewer || this._isMouseHoldingViewer);
            
            if (tIsWithinViewer) { tInputPayload.CaptureMouseWheel(); }
            if (this._isMouseHoldingViewer) { tInputPayload.CaptureMouseDragDelta(); }

            CanvasDrawFlags tRes = this.mActiveCanvas.Draw(
                                    pGraphArea.center,
                                    pGraphArea.start,
                                    pGraphArea.size,
                                    -1 * pGraphArea.size / 2,
                                    this.mConfig.gridSnapProximity,
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
            // Input process (viewer level)
            if (tInputPayload.mIsKeyCtrl && tInputPayload.mIsKeyC)
            {
                this.CopySelectedNodes();
            }
            if (tInputPayload.mIsKeyCtrl && tInputPayload.mIsKeyV)
            {
                this.PasteNodes();
            }
            // Snap lines
            if (!tRes.HasFlag(CanvasDrawFlags.NoNodeSnap)) this.DrawSnapLine(pGraphArea, pSnapData);
            // Auto save
            if (tRes == CanvasDrawFlags.None || tRes == CanvasDrawFlags.NoCanvasZooming)
            {
                this.Save();
            }
        }
        private GridSnapData DrawGraphBg(Area pArea, Vector2 pOffset, float pCanvasScale)
        {
            GridSnapData tGridSnap = new();
            float tUGSmall = this.mConfig.unitGridSmall * pCanvasScale;
            float tUGLarge = this.mConfig.unitGridLarge * pCanvasScale;
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
            pDrawList.AddRectFilled(pArea.start, pArea.end, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NormalBar_Grey, 0.13f)));

            // grid
            uint tGridColor = ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NormalBar_Grey, 0.05f));
            for (var i = 0; i < (pArea.end.X - tGridStart_S.X) / tUGSmall; i++)        // vertical S
            {
                pDrawList.AddLine(new Vector2(tGridStart_S.X + i * tUGSmall, pArea.start.Y), new Vector2(tGridStart_S.X + i * tUGSmall, pArea.end.Y), tGridColor, 1.0f);
            }
            for (var i = 0; i < (pArea.end.Y - tGridStart_S.Y) / tUGSmall; i++)        // horizontal S
            {
                pDrawList.AddLine(new Vector2(pArea.start.X, tGridStart_S.Y + i * tUGSmall), new Vector2(pArea.end.X, tGridStart_S.Y + i * tUGSmall), tGridColor, 1.0f);
            }

            int tXFirstNotation = (int)(-pOffset.X * pCanvasScale - pArea.size.X / 2) / (int)tUGLarge * (int)this.mConfig.unitGridLarge;
            int tYFirstNotation = (int)(-pOffset.Y * pCanvasScale - pArea.size.Y / 2) / (int)tUGLarge * (int)this.mConfig.unitGridLarge;
            float tTransMax = 0.2f;
            for (var i = 0; i < (pArea.end.X - tGridStart_L.X) / tUGLarge; i++)        // vertical L
            {
                pDrawList.AddLine(new Vector2(tGridStart_L.X + i * tUGLarge, pArea.start.Y), new Vector2(tGridStart_L.X + i * tUGLarge, pArea.end.Y), tGridColor, 2.0f);
                tGridSnap.X.Add(tGridStart_L.X + i * tUGLarge);
                if (this._isShowingRulerText)
                {
                    float tTrans = 1;
                    if (this._rulerTextLastAppear.HasValue)
                        tTrans = tTransMax - ((float)((DateTime.Now - this._rulerTextLastAppear.Value).TotalMilliseconds) / this.mConfig.timeForRulerTextFade) * tTransMax;
                    pDrawList.AddText(
                        new Vector2(tGridStart_L.X + i * tUGLarge, pArea.start.Y),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeText, tTrans)),
                        $"{(tXFirstNotation + (this.mConfig.unitGridLarge * i)) / 10}");
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
                        tTrans = tTransMax - ((float)((DateTime.Now - this._rulerTextLastAppear.Value).TotalMilliseconds) / this.mConfig.timeForRulerTextFade) * tTransMax;
                    pDrawList.AddText(
                        new Vector2(pArea.start.X + 6, tGridStart_L.Y + i * tUGLarge),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeText, tTrans)),
                        $"{(tYFirstNotation + (this.mConfig.unitGridLarge * i)) / 10}");
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
        private void DrawExtraOptions(HashSet<string> pPUReqs)
        {
            // Canvas options =================================
            UtilsGUI.TextDescriptionForWidget("Node");
            ImGui.Spacing();
            this.DrawCanvasExtraOptions();

            ImGui.Separator();

            // Viewer options =================================
            UtilsGUI.TextDescriptionForWidget("Canvas");
            ImGui.Spacing();
            this.DrawViewerExtraOptions(pPUReqs);

            ImGui.Separator();

            // Config =========================================
            UtilsGUI.TextDescriptionForWidget("Config");
            ImGui.Spacing();
            this.DrawViewerConfig("");
        }
        private void DrawViewerExtraOptions(HashSet<string> pPUReqs)
        {
            // Import
            if (ImGui.Selectable("Import", false, ImGuiSelectableFlags.DontClosePopups))
            {
                try
                {
                    if (this.ImportCanvas(ImGui.GetClipboardText()) != null)
                    {
                        this.mNotificationManager.Push(new ViewerNotification($"##cimpy", $"Canvas imported from clipboard!"));
                    }
                    else
                    {
                        this.mNotificationManager.Push(new ViewerNotification($"##cimpn", $"Failed to import canvas from clipboard.", ViewerNotificationType.Error));
                    }
                }
                catch (Newtonsoft.Json.JsonReaderException _)
                {
                    this.mNotificationManager.Push(new ViewerNotification($"##cimpn", $"Failed to import canvas from clipboard.", ViewerNotificationType.Error));
                }
            }
            else UtilsGUI.SetTooltipForLastItem("Import a canvas from clipboard.");
            // Export
            if (ImGui.Selectable("Export current", false, ImGuiSelectableFlags.DontClosePopups))
            {
                ImGui.SetClipboardText(this.ExportActiveCanvasAsJson());
                this.mNotificationManager.Push(new ViewerNotification($"##cexp{this.mActiveCanvas.mId}", $"Canvas copied to clipboard!"));
            }
            else UtilsGUI.SetTooltipForLastItem("Copy currently selected canvas to clipboard.");
            // Rename
            if (ImGui.Selectable("Rename current", false, ImGuiSelectableFlags.DontClosePopups))
            {
                pPUReqs.Add("vwexpu_rename");
            }
            else UtilsGUI.SetTooltipForLastItem("Rename currently selected canvas.");
        }
        private void DrawViewerExtraOptions_Rename(string pGuiId)
        {
            if (ImGui.BeginPopup(pGuiId))
            {
                if (this._infield_CanvasName == null) this._infield_CanvasName = this.mActiveCanvas.mName;
                ImGui.InputText($"##{pGuiId}_input", ref this._infield_CanvasName, 200);
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                {
                    this.mActiveCanvas.mName = this._infield_CanvasName;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            // reset fields
            else
            {
                this._infield_CanvasName = null;
            }
        }
        private void DrawCanvasExtraOptions()
        {
            if (ImGui.Selectable("Copy [Ctrl + C]"))
            {
                this.CopySelectedNodes();
            }
            else UtilsGUI.SetTooltipForLastItem("Copy selected nodes to clipboard.");
            if (ImGui.Selectable("Paste [Ctrl + V]"))
            {
                this.PasteNodes();
            }
            else UtilsGUI.SetTooltipForLastItem("Paste nodes from clipboard.");
        }
        private void DrawViewerConfig(string pGuiId)
        {

        }
        private void DrawNodeSearchBox(Vector2? pPUSize = null, float? pTextBoxWidth = null)
        {
            var tAnchor = ImGui.GetCursorScreenPos();

            if (pTextBoxWidth != null) ImGui.SetNextItemWidth(pTextBoxWidth.Value);
            ImGui.InputTextWithHint("", "Search nodes...", ref this._infield_NodeLookupVal, 200);
            bool tIsInputActive = ImGui.IsItemActive();
            bool tIsInputActivated = ImGui.IsItemActivated();
            bool tIsItemPUOpened = false;
            UtilsGUI.SetTooltipForLastItem("- Search by node's header.\nCapitalization insensitive. No regex.");

            if (this._infield_NodeLookupVal.Length != 0 && tIsInputActive)
                ImGui.OpenPopup("##searchNodePU");

            ImGui.SetNextWindowPos(tAnchor + new Vector2(0, 25));
            ImGui.SetNextWindowSizeConstraints(new Vector2(50, 25), pPUSize ?? new Vector2(300, 300));

            if (ImGui.BeginPopup("##searchNodePU", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.ChildWindow))
            {
                var tNodeIds = this.mActiveCanvas.LookUpNode(this._infield_NodeLookupVal);

                foreach (string nid in tNodeIds)
                {
                    Node? node = this.mActiveCanvas.GetNode(nid);
                    if (node == null) continue;

                    if (ImGui.Selectable(node.mContent.GetHeader(), false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        this.FocusOnNodeId_ActiveCanvas(node.mId);
                    }
                }
                if (!tIsInputActive && !ImGui.IsWindowFocused() && !tIsItemPUOpened)
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        private void FocusOnNodeId_ActiveCanvas(string pNodeId, Vector2? pExtraOfs = null) => this.mActiveCanvas.FocusOnNode(pNodeId, pExtraOfs);
        public void FocusOnNodeTag_ActiveCanvas(string pTag, Vector2? pExtraOfs = null)
        {
            var tRes = this.mActiveCanvas.LookUpNodeWithTag(pTag);
            if (tRes.Count == 0) return;
            this.mActiveCanvas.FocusOnNode(tRes.First(), pExtraOfs);
        }
        private void CopySelectedNodes()
        {
            var tJson = this.mActiveCanvas.ExportSelectedNodes();
            if (tJson != null)
            {
                ImGui.SetClipboardText(tJson);
                this.mNotificationManager.Push(new ViewerNotification($"##nexpo", $"Nodes copied to clipboard!"));
            }
        }
        private void PasteNodes()
        {
            if (this.mActiveCanvas.ImportNodes(ImGui.GetClipboardText()))
            {
                this.mNotificationManager.Push(new ViewerNotification($"##nimpo", $"Nodes pasted!"));
            }
            else
            {
                this.mNotificationManager.Push(new ViewerNotification($"##nimpof", $"Failed to paste nodes from clipboard.", ViewerNotificationType.Error));
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

        [Flags]
        public enum ViewerEventFlag
        {
            None = 0,
            NewSaveDataAvailable = 1
        }
    }
}
