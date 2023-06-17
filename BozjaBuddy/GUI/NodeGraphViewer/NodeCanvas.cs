using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;
using BozjaBuddy.Utils;
using System.Linq;
using static BozjaBuddy.Utils.UtilsGUI;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// <para>Represents a layer of node graph.</para>
    /// <para>Contains info about nodes and its position.</para>
    /// </summary>
    public class NodeCanvas
    {
        public static float minScale = 0.25f;
        public static float maxScale = 2f;
        public static float stepScale = 0.1f;

        private int _counter { get; set; } = -1;

        private readonly NodeMap mMap = new();
        private readonly Dictionary<string, Node> mNodes = new();
        private readonly HashSet<string> _nodeIds = new();
        private readonly Dictionary<string, HashSet<Node>> mEdges = new();
        private readonly Dictionary<string, HashSet<Node>> mEdgesReversed = new();      // readonly is required for OccupiedRegion to update
        private readonly OccupiedRegion mOccuppiedRegion;
        private CanvasConfig mConfig { get; set; } = new();

        private bool _isNodeBeingDragged = false;
        private HashSet<string> _selectedNode = new();
        private Node? _snappingNode = null;
        private Vector2? _lastSnapDelta = null;
        private FirstClickType _firstClickInDrag = FirstClickType.None;
        private bool _isFirstFrameAfterLmbDown = true;      // specifically for Draw()
        private Vector2? _selectAreaOSP = null;

        public NodeCanvas()
        {
            this.mOccuppiedRegion = new(this.mNodes, this.mMap);
        }

        /// <summary>
        /// Add node at pos relative to the canvas's origin.
        /// </summary>
        public string? AddNode<T>(
                string pHeader,
                Vector2 pDrawRelaPos
                          ) where T : Node, new()
        {
            int tNewId = this._counter + 1;
            // create node
            T tNode = new();
            tNode.Init(tNewId.ToString(), pHeader);
            // add node
            try
            {
                if (!this.mNodes.TryAdd(tNode.mId, tNode)) return null;
                if (!this._nodeIds.Add(tNode.mId)) return null;
                this.mMap.AddNode(tNode.mId, pDrawRelaPos);
                PluginLog.LogDebug($"> Import mapping for nodeId={tNode.mId} at ({pDrawRelaPos.X}, {pDrawRelaPos.Y})");
            }
            catch (Exception e) { PluginLog.LogDebug(e.Message); }

            this._counter++;
            return tNode.mId;
        }
        /// <summary>
        /// <para>Add node to one of the 4 corners of the occupied area, with preferred direction.</para>
        /// <para>Returns added node's ID if succeed, otherwise null.</para>
        /// </summary>
        public string? AddNodeToAvailableCorner<T>(
                string pHeader,
                Direction pCorner = Direction.NE,
                Direction pDirection = Direction.E,
                Vector2? pPadding = null
                                  ) where T : Node, new()
        {
            return this.AddNode<T>(
                    pHeader,
                    this.mOccuppiedRegion.GetAvailableRelaPos(pCorner, pDirection, pPadding ?? this.mConfig.nodeGap)
                );
        }
        /// <summary>
        /// <para>Add node within the view area (master layer)</para>
        /// <para>Returns added node's ID if succeed, otherwise null.</para>
        /// </summary>
        public string? AddNodeWithinView<T>(
                string pHeader,
                Vector2 pMasterScreenSize
                                  ) where T : Node, new()
        {
            var tOffset = this.mMap.GetBaseOffset();
            Area pRelaAreaToScanForAvailableRegion = new(
                    new(0, tOffset.Y),
                    pMasterScreenSize - tOffset
                );
            return this.AddNode<T>(
                    pHeader,
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
            if (pReadClicks && !tIsNodeHandleClicked && pInputPayload.mIsKeyShift && pInputPayload.mIsMouseMid)
            {
                if (pNode.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                    this.RemoveNode(pNode.mId);
            }
            // Process node select
            if (pReadClicks && !tIsNodeHandleClicked && pInputPayload.mIsMouseLmb)
            {
                if (pNode.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
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
                else if (pNode.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
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
                    if (pNode.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                    {
                        if (pNode.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
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
            if (pInputPayload.mLmbDragDelta.HasValue) { this.mMap.AddBaseOffset(pInputPayload.mLmbDragDelta.Value); }
            // Mouse wheel zooming
            switch (pInputPayload.mMouseWheelValue)
            {
                case 1:
                    if (this.mConfig.scaling >= NodeCanvas.maxScale)
                        this.mConfig.scaling = NodeCanvas.maxScale;
                    else
                        this.mConfig.scaling += NodeCanvas.stepScale;
                    break;
                case -1:
                    if (this.mConfig.scaling <= NodeCanvas.minScale)
                        this.mConfig.scaling = NodeCanvas.minScale;
                    else
                        this.mConfig.scaling -= NodeCanvas.stepScale;
                    break;
            };
        }
        public InputFlag Draw(Vector2 pBaseOriginScreenPos, UtilsGUI.InputPayload pInputPayload, NodeGraphViewer.GridSnapData? pSnapData = null, bool pInteractable = true)
        {
            bool tIsAnyNodeHandleClicked = false;
            bool tIsReadingClicksOnNode = true;
            bool tIsAnyNodeClicked = false;
            Vector2? tSnapDelta = null;
            // Get this canvas' origin' screenPos
            Vector2 tCanvasOSP = pBaseOriginScreenPos + this.mMap.GetBaseOffset();

            if (!pInteractable)     // clean up stuff in case viewer is involuntarily lose focus, to avoid potential accidents.
            {
                this._lastSnapDelta = null;
                this._snappingNode = null;
                this._isNodeBeingDragged = false;
            }

            // Capture selectArea
            if (pInteractable)
            {
                
            }

            // Populate snap data
            if (pInteractable)
            {
                foreach (var node in this.mNodes.Values)
                {
                    Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(node.mId, tCanvasOSP, this.mConfig.scaling);
                    if (tNodeOSP == null) continue;
                    if (this._snappingNode != null && node.mId != this._snappingNode.mId)
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
            foreach (var id in this._nodeIds)
            {
                // Get NodeOSP
                Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(id, tCanvasOSP, this.mConfig.scaling);
                if (tNodeOSP == null) continue;
                if (!this.mNodes.TryGetValue(id, out var tNode) || tNode == null) continue;

                if (pInteractable)
                {
                    // Process input on node
                    var t = this.ProcessInputOnNode(tNode, tNodeOSP.Value, pInputPayload, tIsReadingClicksOnNode);
                    if (t.Item1) tIsAnyNodeHandleClicked = t.Item1;
                    tIsReadingClicksOnNode = t.Item2;
                    if (t.Item3) tIsAnyNodeClicked = true;
                    if (t.Item4 != FirstClickType.None) tFirstClickScanRes = t.Item4;
                }

                // Draw using NodeOSP
                tNode.Draw(
                    tSnapDelta != null && this._selectedNode.Contains(id)
                        ? tNodeOSP.Value + tSnapDelta.Value
                        : tNodeOSP.Value, 
                    this.mConfig.scaling,
                    this._selectedNode.Contains(id),
                    pInputPayload);
                if (tNode._isBusy) tIsAnyNodeBusy = true;
            }
            // Capture drag's first click. State Body or Handle can only be accessed from state None.
            if (pInputPayload.mIsMouseLmb) this._firstClickInDrag = FirstClickType.None;
            else if (this._firstClickInDrag == FirstClickType.None && tFirstClickScanRes != FirstClickType.None)
                this._firstClickInDrag = tFirstClickScanRes;

            if (pInteractable 
                && pInputPayload.mIsMouseLmb 
                && !tIsAnyNodeBusy
                && (!tIsAnyNodeHandleClicked && (pInputPayload.mLmbDragDelta == null))
                && !pInputPayload.mIsALmbDragRelease) this._selectedNode.Clear();

            if (pInteractable && !tIsAnyNodeBusy)
            {
                // Drag selected node
                if (this._isNodeBeingDragged && pInputPayload.mLmbDragDelta.HasValue)
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
                else if (!this._isNodeBeingDragged && this._lastSnapDelta != null)
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
                if (!this._isNodeBeingDragged && !tIsAnyNodeClicked && this._firstClickInDrag == FirstClickType.None) 
                    this.ProcessInputOnCanvas(pInputPayload);
            }

            // First frame after lmb down. Leave this at the bottom (end of frame drawing).
            if (pInputPayload.mIsMouseLmb) this._isFirstFrameAfterLmbDown = true;
            else if (pInputPayload.mIsMouseLmbDown) this._isFirstFrameAfterLmbDown = false;

            return InputFlag.None;
        }


        public class CanvasConfig
        {
            public float scaling;
            public Vector2 nodeGap;

            public CanvasConfig()
            {
                this.scaling = 1f;
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
