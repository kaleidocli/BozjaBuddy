using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;
using BozjaBuddy.Utils;
using System.Linq;
using static BozjaBuddy.Utils.UtilsGUI;
using QuickGraph;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// <para>Represents a layer of node graph.</para>
    /// <para>Contains info about nodes and its position.</para>
    /// </summary>
    public class NodeCanvas : IDisposable
    {
        public static float minScale = 0.4f;
        public static float maxScale = 2f;
        public static float stepScale = 0.1f;

        private Plugin mPlugin;
        public int mId;
        public string mName;
        private int _nodeCounter { get; set; } = -1;

        private readonly NodeMap mMap = new();
        private readonly Dictionary<string, Node> mNodes = new();
        private readonly HashSet<string> _nodeIds = new();
        private readonly OccupiedRegion mOccuppiedRegion;
        private readonly AdjacencyGraph<int, SEdge<int>> mGraph;
        private CanvasConfig mConfig { get; set; } = new();

        private bool _isNodeBeingDragged = false;
        private HashSet<string> _selectedNode = new();
        private Node? _snappingNode = null;
        private Vector2? _lastSnapDelta = null;
        private FirstClickType _firstClickInDrag = FirstClickType.None;
        private bool _isFirstFrameAfterLmbDown = true;      // specifically for Draw()
        private Vector2? _selectAreaOSP = null;
        private bool _isNodeSelectionLocked = false;

        public NodeCanvas(Plugin pPlugin, int pId, string pName = "new canvas")
        {
            this.mPlugin = pPlugin;
            this.mId = pId;
            this.mName = pName;
            this.mOccuppiedRegion = new(this.mNodes, this.mMap);
            this.mGraph = new();
        }
        public float GetScaling() => this.mConfig.scaling;
        public void SetScaling(float pScale) => this.mConfig.scaling = pScale;
        public Vector2 GetBaseOffset() => this.mMap.GetBaseOffset();

        /// <summary>
        /// Add node at pos relative to the canvas's origin.
        /// </summary>
        public string? AddNode<T>(
                Node.NodeContent pNodeContent,
                Vector2 pDrawRelaPos
                          ) where T : Node, new()
        {
            int tNewId = this._nodeCounter + 1;
            // create node
            T tNode = new();
            tNode.Init(this.mPlugin, tNewId.ToString(), tNewId, pNodeContent);
            // add node
            try
            {
                if (!this.mNodes.TryAdd(tNode.mId, tNode)) return null;
                if (!this._nodeIds.Add(tNode.mId)) return null;
                this.mMap.AddNode(tNode.mId, pDrawRelaPos);
                PluginLog.LogDebug($"> Import mapping for nodeId={tNode.mId} at ({pDrawRelaPos.X}, {pDrawRelaPos.Y})");
            }
            catch (Exception e) { PluginLog.LogDebug(e.Message); }

            this._nodeCounter++;
            return tNode.mId;
        }
        /// <summary>
        /// <para>Add node to one of the 4 corners of the occupied area, with preferred direction.</para>
        /// <para>Returns added node's ID if succeed, otherwise null.</para>
        /// </summary>
        public string? AddNodeToAvailableCorner<T>(
                Node.NodeContent pNodeContent,
                Direction pCorner = Direction.NE,
                Direction pDirection = Direction.E,
                Vector2? pPadding = null
                                  ) where T : Node, new()
        {
            return this.AddNode<T>(
                    pNodeContent,
                    this.mOccuppiedRegion.GetAvailableRelaPos(pCorner, pDirection, pPadding ?? this.mConfig.nodeGap)
                );
        }
        /// <summary>
        /// <para>Add node within the view area (master layer)</para>
        /// <para>Returns added node's ID if succeed, otherwise null.</para>
        /// </summary>
        public string? AddNodeWithinView<T>(
                Node.NodeContent pNodeContent,
                Vector2 pMasterScreenSize
                                  ) where T : Node, new()
        {
            var tOffset = this.mMap.GetBaseOffset();
            Area pRelaAreaToScanForAvailableRegion = new(
                    new(0, tOffset.Y),
                    pMasterScreenSize - tOffset
                );
            return this.AddNode<T>(
                    pNodeContent,
                    this.mOccuppiedRegion.GetAvailableRelaPos(pRelaAreaToScanForAvailableRegion)
                ); ;
        }
        /// <summary>
        /// Return false if the process partially/fully fails.
        /// </summary>
        public bool RemoveNode(string pNodeId)
        {
            bool tRes = true;
            if (!this.mMap.RemoveNode(pNodeId)) tRes = false;
            if (this.mNodes.ContainsKey(pNodeId)) { this.mNodes[pNodeId].Dispose(); }
            if (!this.mNodes.Remove(pNodeId)) tRes = false;
            if (!_nodeIds.Remove(pNodeId)) tRes = false;
            this.mOccuppiedRegion.Update();
            return tRes;
        }
        /// <summary>
        /// <para>Create a directional edge connecting two nodes.</para>
        /// <para>Return false if one of the nodes DNE, otherwise true.</para>
        /// </summary>
        public bool ConnectNodes(string pNodeIdStart, string pNodeIdEnd)
        {
            if (!(this.HasNode(pNodeIdStart) && this.HasNode(pNodeIdEnd))) return false;

            return true;
        }
        /// <summary>
        /// <para>Check if node exists in this canvas' collection and map.</para>
        /// </summary>
        public bool HasNode(string pNodeId) => this._nodeIds.Contains(pNodeId) && this.mMap.CheckNodeExist(pNodeId);
        public void MoveCanvas(Vector2 pDelta)
        {
            this.mMap.AddBaseOffset(pDelta);
        }
        public Tuple<bool, bool, bool, FirstClickType> ProcessInputOnNode(Node pNode, Vector2 pNodeOSP, UtilsGUI.InputPayload pInputPayload, bool pReadClicks)
        {
            bool tIsNodeHandleClicked = false;
            bool tIsNodeClicked = false;
            FirstClickType tFirstClick = FirstClickType.None;
            // Process node delete
            if (pReadClicks && !tIsNodeHandleClicked && pInputPayload.mIsMouseMid)
            {
                if (pNode.mStyle.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                    this.RemoveNode(pNode.mId);
            }
            else if (pNode._isMarkedDeleted)
            {
                this.RemoveNode(pNode.mId);
            }
            // Process node select
            if (pReadClicks && !tIsNodeHandleClicked && pInputPayload.mIsMouseLmb)
            {
                if (pNode.mStyle.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                {
                    // multi-selecting
                    if (pInputPayload.mIsKeyCtrl)
                    {
                        // select
                        if (!this._selectedNode.Contains(pNode.mId))
                            this._selectedNode.Add(pNode.mId);
                        // remove
                        else
                            this._selectedNode.Remove(pNode.mId);
                    }
                    // single-selecting node
                    else if (!pInputPayload.mIsALmbDragRelease)
                    {
                        pReadClicks = false;
                        this._selectedNode.Clear();
                        this._selectedNode.Add(pNode.mId);
                    }
                    tIsNodeHandleClicked = true;
                }
                else if (pNode.mStyle.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                {
                    tIsNodeClicked = true;
                }
            }
            // Process node holding and dragging, except for when multiselecting
            if (pInputPayload.mIsMouseLmbDown)          // if mouse is hold, and the holding's first pos is within a selected node
            {                                           // then mark state as being dragged
                                                        // as long as the mouse is hold, even if mouse then moving out of node zone
                                                        // First click in drag
                if (!this._isNodeBeingDragged && this._isFirstFrameAfterLmbDown)
                {
                    if (pNode.mStyle.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                    {
                        if (pNode.mStyle.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                            tFirstClick = FirstClickType.Handle;
                        else
                            tFirstClick = FirstClickType.Body;
                    }
                }

                if (!this._isNodeBeingDragged
                    && tFirstClick != FirstClickType.None
                    && !tIsNodeHandleClicked
                    && !pInputPayload.mIsKeyCtrl)
                    
                {
                    if (tFirstClick == FirstClickType.Handle)
                    {
                        this._isNodeBeingDragged = true;
                        this._snappingNode = pNode;
                        // single-selecting new node
                        if (!pInputPayload.mIsKeyCtrl && !this._selectedNode.Contains(pNode.mId))
                        {
                            tIsNodeHandleClicked = true;
                            this._selectedNode.Clear();
                            this._selectedNode.Add(pNode.mId);
                        }
                    }
                    else if (tFirstClick == FirstClickType.Body)
                    {
                        tIsNodeClicked = true;
                    }
                }
            }
            else
            {
                this._isNodeBeingDragged = false;
                this._snappingNode = null;
            }

            return new Tuple<bool, bool, bool, FirstClickType>(tIsNodeHandleClicked, pReadClicks, tIsNodeClicked, tFirstClick);
        }
        public void ProcessInputOnCanvas(UtilsGUI.InputPayload pInputPayload)
        {
            // Mouse drag
            if (pInputPayload.mLmbDragDelta.HasValue) { this.mMap.AddBaseOffset(pInputPayload.mLmbDragDelta.Value / this.mConfig.scaling); }
            // Mouse wheel zooming
            switch (pInputPayload.mMouseWheelValue)
            {
                case 1:
                    this.mConfig.scaling += NodeCanvas.stepScale;
                    break;
                case -1:
                    this.mConfig.scaling -= NodeCanvas.stepScale;
                    break;
            };
        }
        /// <summary>
        /// Interactable:   Window active. Either cursor within viewer, 
        ///                 or cursor can be outside of viewer while holding the viewer.
        /// </summary>
        public CanvasDrawFlags Draw(
            Vector2 pBaseOriginScreenPos,
            Vector2 pInitBaseOffset,
            UtilsGUI.InputPayload pInputPayload,
            NodeGraphViewer.GridSnapData? pSnapData = null, 
            CanvasDrawFlags pCanvasDrawFlag = CanvasDrawFlags.None)
        {
            bool tIsAnyNodeHandleClicked = false;
            bool tIsReadingClicksOnNode = true;
            bool tIsAnyNodeClicked = false;
            Area? tSelectScreenArea = null;
            Vector2? tSnapDelta = null;
            // Get this canvas' origin' screenPos   (only scaling for zooming)
            if (this.mMap.CheckNeedInitOfs())
            {
                this.mMap.AddBaseOffset(pInitBaseOffset);
                this.mMap.MarkUnneedInitOfs();
            }
            Vector2 tCanvasOSP = pBaseOriginScreenPos + this.mMap.GetBaseOffset() * this.mConfig.scaling;

            if (pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract))     // clean up stuff in case viewer is involuntarily lose focus, to avoid potential accidents.
            {
                this._lastSnapDelta = null;
                this._snappingNode = null;
                this._isNodeBeingDragged = false;
            }

            // Capture selectArea
            if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract))
            {
                // Capture selectAreaOSP
                if (!this._isNodeBeingDragged && pInputPayload.mIsKeyShift && pInputPayload.mIsMouseLmbDown)
                {
                    if (!this._selectAreaOSP.HasValue) this._selectAreaOSP = pInputPayload.mMousePos;
                }
                else this._selectAreaOSP = null;

                // Capture selectArea
                if (this._selectAreaOSP != null)
                {
                    tSelectScreenArea = new(this._selectAreaOSP.Value, pInputPayload.mMousePos, true);
                    //PluginLog.LogDebug($"> m={pInputPayload.mMousePos} s={tSelectScreenArea.start} e={tSelectScreenArea.end} sz={tSelectScreenArea.size}");
                }
            }

            // Populate snap data
            if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract))
            {
                foreach (var node in this.mNodes.Values)
                {
                    Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(node.mId, tCanvasOSP, this.mConfig.scaling);
                    if (tNodeOSP == null) continue;
                    if (this._snappingNode != null && node.mId != this._snappingNode.mId && !this._selectedNode.Contains(node.mId))  // avoid snapping itself & selected nodess
                    {
                        pSnapData?.AddUsingPos(tNodeOSP.Value);
                    }
                }
                // Get snap delta
                if (this._snappingNode != null)
                {
                    Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(this._snappingNode.mId, tCanvasOSP, this.mConfig.scaling);
                    Vector2? tSnapOSP = null;
                    if (tNodeOSP.HasValue)
                        tSnapOSP = pSnapData?.GetClosestSnapPos(tNodeOSP.Value, NodeGraphViewer.kGridSnapProximity);
                    if (tSnapOSP.HasValue)
                        tSnapDelta = tSnapOSP.Value - tNodeOSP;
                    this._lastSnapDelta = tSnapDelta;
                }
            }

            // Draw
            FirstClickType tFirstClickScanRes = FirstClickType.None;
            bool tIsAnyNodeBusy = false;
            bool tIsLockingSelection = false;
            foreach (var id in this._nodeIds)
            {
                // Get NodeOSP
                Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(id, tCanvasOSP, this.mConfig.scaling);
                if (tNodeOSP == null) continue;
                if (!this.mNodes.TryGetValue(id, out var tNode) || tNode == null) continue;

                // Process input on node
                if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract) && !this._isNodeSelectionLocked)
                {
                    // Process input on node
                    var t = this.ProcessInputOnNode(tNode, tNodeOSP.Value, pInputPayload, tIsReadingClicksOnNode);
                    if (t.Item1) tIsAnyNodeHandleClicked = t.Item1;
                    tIsReadingClicksOnNode = t.Item2;
                    if (t.Item3) tIsAnyNodeClicked = true;
                    if (t.Item4 != FirstClickType.None) tFirstClickScanRes = t.Item4;
                    // Select using selectArea
                    if (tSelectScreenArea != null && !this._isNodeBeingDragged && this._firstClickInDrag == FirstClickType.None)
                    {
                        if (tNode.mStyle.CheckAreaIntersect(tNodeOSP.Value, this.mConfig.scaling, tSelectScreenArea))
                        {
                            this._selectedNode.Add(id);
                        }
                    }
                }

                // Draw using NodeOSP
                NodeInteractionFlags tNodeRes = tNode.Draw(
                                                    tSnapDelta != null && this._selectedNode.Contains(id)
                                                        ? tNodeOSP.Value + tSnapDelta.Value
                                                        : tNodeOSP.Value, 
                                                    this.mConfig.scaling,
                                                    this._selectedNode.Contains(id),
                                                    pInputPayload);
                // Draw node's coord display
                var tNodeRelaPos = this.mMap.GetNodeRelaPos(id);
                if (tNodeRelaPos.HasValue) 
                    ImGui.GetWindowDrawList().AddText(tNodeOSP.Value + new Vector2(0, -30), ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeText), $"({tNodeRelaPos.Value.X}, {tNodeRelaPos.Value.Y})");

                if (tNode._isBusy) tIsAnyNodeBusy = true;
                // Process node's content interaction
                if (tNodeRes.HasFlag(NodeInteractionFlags.Internal)) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasInteraction | CanvasDrawFlags.NoNodeDrag | CanvasDrawFlags.NoNodeSnap;
                if (tNodeRes.HasFlag(NodeInteractionFlags.LockSelection))
                {
                    tIsLockingSelection = true;
                }
            }
            if (tIsLockingSelection) this._isNodeSelectionLocked = true;
            else this._isNodeSelectionLocked = false;
            // Capture drag's first click. State Body or Handle can only be accessed from state None.
            if (pInputPayload.mIsMouseLmb) this._firstClickInDrag = FirstClickType.None;
            else if (this._firstClickInDrag == FirstClickType.None && tFirstClickScanRes != FirstClickType.None)
                this._firstClickInDrag = tFirstClickScanRes;

            if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract)
                && pInputPayload.mIsMouseLmb
                && !tIsAnyNodeBusy
                && (!tIsAnyNodeHandleClicked && (pInputPayload.mLmbDragDelta == null))
                && !pInputPayload.mIsALmbDragRelease)
            {
                this._selectedNode.Clear();
            }


            // Draw selectArea
            if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract) && tSelectScreenArea != null)
            {
                ImGui.GetWindowDrawList().AddRectFilled(tSelectScreenArea.start, tSelectScreenArea.end, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, 0.5f)));
            }

            if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract) && !tIsAnyNodeBusy)
            {
                // Drag selected node
                if (this._isNodeBeingDragged 
                    && pInputPayload.mLmbDragDelta.HasValue
                    && !pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoNodeDrag))
                {
                    foreach (var id in this._selectedNode)
                    {
                        this.mMap.MoveNodeRelaPos(
                            id,
                            pInputPayload.mLmbDragDelta.Value,
                            this.mConfig.scaling);
                    }
                }
                // Snap if available
                else if (!this._isNodeBeingDragged 
                         && this._lastSnapDelta != null 
                         && (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoNodeDrag) || !pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoNodeSnap)))
                {
                    foreach (var id in this._selectedNode)
                    {
                        this.mMap.MoveNodeRelaPos(
                            id,
                            this._lastSnapDelta.Value,
                            this.mConfig.scaling);
                    }
                    this._lastSnapDelta = null;
                }
                // Process input on canvas
                if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoCanvasInteraction)
                    && !this._isNodeBeingDragged 
                    && !tIsAnyNodeClicked
                    && (this._firstClickInDrag == FirstClickType.None || this._firstClickInDrag == FirstClickType.Body)
                    && this._selectAreaOSP == null)
                {
                    this.ProcessInputOnCanvas(pInputPayload);
                }
                    
            }

            // First frame after lmb down. Leave this at the bottom (end of frame drawing).
            if (pInputPayload.mIsMouseLmb) this._isFirstFrameAfterLmbDown = true;
            else if (pInputPayload.mIsMouseLmbDown) this._isFirstFrameAfterLmbDown = false;

            return pCanvasDrawFlag;
        }


        public void Dispose()
        {
            var tNodeIds = this.mNodes.Keys.ToList();
            foreach (var id in tNodeIds)
            {
                this.RemoveNode(id);
            }
        }

        public class CanvasConfig
        {
            private float _scaling;
            public float scaling
            {
                get { return this._scaling; }
                set
                {
                    if (value > NodeCanvas.maxScale)
                        this._scaling = NodeCanvas.maxScale;
                    else if (value < NodeCanvas.minScale)
                        this._scaling = NodeCanvas.minScale;
                    else
                        this._scaling = value;
                }
            }
            public Vector2 nodeGap;

            public CanvasConfig()
            {
                this.scaling = 1f;
                this._scaling = 1f;
                this.nodeGap = Vector2.One;
            }
        }
        public enum FirstClickType
        {
            None = 0,
            Handle = 1,
            Body = 2
        }
    }
    public enum Direction
    {
        N = 0,
        NE = 1,
        E = 2,
        SE = 3,
        S = 4,
        SW = 5,
        W = 6,
        NW = 7,
        None = 8
    }
}
