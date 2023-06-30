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
using System.Diagnostics.Tracing;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using QuickGraph.Algorithms;
using Newtonsoft.Json;
using BozjaBuddy.GUI.NodeGraphViewer.utils;

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

        public int mId;
        public string mName;
        [JsonProperty]
        private int _nodeCounter { get; set; } = -1;

        [JsonProperty]
        private readonly NodeMap mMap = new();
        [JsonProperty]
        private readonly Dictionary<string, Node> mNodes = new();
        [JsonProperty]
        private readonly HashSet<string> _nodeIds = new();
        [JsonProperty]
        private readonly OccupiedRegion mOccuppiedRegion;       // this shouldn't be serialized, and only initiated using node list and maps.
        [JsonProperty]
        private readonly AdjacencyGraph<int, SEdge<int>> mGraph;        // whatever this is
        [JsonProperty]
        private readonly List<Edge> mEdges = new();

        [JsonProperty]
        private CanvasConfig mConfig { get; set; } = new();

        private bool _isNodeBeingDragged = false;
        private HashSet<string> _selectedNodes = new();
        private Node? _snappingNode = null;
        private Vector2? _lastSnapDelta = null;
        private FirstClickType _firstClickInDrag = FirstClickType.None;
        private bool _isFirstFrameAfterLmbDown = true;      // specifically for Draw()
        private Vector2? _selectAreaOSP = null;
        private bool _isNodeSelectionLocked = false;
        private EdgeConn? _nodeConnTemp = null;

        public NodeCanvas(int pId, string pName = "new canvas")
        {
            this.mId = pId;
            this.mName = pName;
            this.mOccuppiedRegion = new();
            this.mGraph = new();

            var nid1 = this.AddNodeToAvailableCorner<BasicNode>(new NodeContent.NodeContent("1"));
            var nid2 = this.AddNodeToAvailableCorner<AuxNode>(new BBNodeContent(null, 0, "2"));
            var nid3 = this.AddNodeToAvailableCorner<BasicNode>(new NodeContent.NodeContent("3"));
            var nid4 = this.AddNodeToAvailableCorner<BBNode>(new BBNodeContent(null, 0, "4"));
            if (nid1 != null && nid2 != null && nid3 != null && nid4 != null)
            {
                this.AddEdge(nid1, nid2);
                this.AddEdge(nid1, nid3);
                this.AddEdge(nid2, nid4);
                this.AddEdge(nid3, nid4);
            }
        }
        public float GetScaling() => this.mConfig.scaling;
        public void SetScaling(float pScale) => this.mConfig.scaling = pScale;
        public Vector2 GetBaseOffset() => this.mMap.GetBaseOffset();

        /// <summary>
        /// Add node at pos relative to the canvas's origin.
        /// </summary>
        protected string? AddNode<T>(
                NodeContent.NodeContent pNodeContent,
                Vector2 pDrawRelaPos
                          ) where T : Node, new()
        {
            int tNewId = this._nodeCounter + 1;
            // create node
            T tNode = new();
            tNode.Init(tNewId.ToString(), tNewId, pNodeContent);
            // add node
            try
            {
                if (!this.mNodes.TryAdd(tNode.mId, tNode)) return null;
                if (!this._nodeIds.Add(tNode.mId)) return null;
                if (!this.mOccuppiedRegion.IsUpdatedOnce()) this.mOccuppiedRegion.Update(this.mNodes, this.mMap);
                this.mMap.AddNode(tNode.mId, pDrawRelaPos);
            }
            catch (Exception e) { PluginLog.LogDebug(e.Message); }

            this.mOccuppiedRegion.Update(this.mNodes, this.mMap);
            this._nodeCounter++;
            // add node vertex to graph
            this.mGraph.AddVertex(tNode.mGraphId);
            return tNode.mId;
        }
        /// <summary>
        /// <para>Add node to one of the 4 corners of the occupied area, with preferred direction.</para>
        /// <para>Returns added node's ID if succeed, otherwise null.</para>
        /// </summary>
        public string? AddNodeToAvailableCorner<T>(
                NodeContent.NodeContent pNodeContent,
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
                NodeContent.NodeContent pNodeContent,
                Vector2 pViewerSize
                                  ) where T : Node, new()
        {
            var tOffset = this.mMap.GetBaseOffset();
            Area pRelaAreaToScanForAvailableRegion = new(
                    -tOffset - pViewerSize * 0.5f,
                    pViewerSize * 0.95f              // only get up until 0.8 of the screen to avoid the new node going out of viewer
                );
            PluginLog.LogDebug($"> Scanning: {pRelaAreaToScanForAvailableRegion.start} ---> {pRelaAreaToScanForAvailableRegion.end}");
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
            int? tNodeGraphId = null;
            if (!this.mMap.RemoveNode(pNodeId)) tRes = false;
            if (this.mNodes.ContainsKey(pNodeId)) 
            {
                tNodeGraphId = this.mNodes[pNodeId].mGraphId;
                this.mNodes[pNodeId].Dispose();
            }
            if (!this.mNodes.Remove(pNodeId)) tRes = false;
            if (!_nodeIds.Remove(pNodeId)) tRes = false;
            this.mOccuppiedRegion.Update(this.mNodes, this.mMap);
            // Graph stuff
            this.RemoveEdgesWithNodeId(pNodeId);
            if (tNodeGraphId.HasValue) this.mGraph.RemoveVertex(tNodeGraphId.Value);
            return tRes;
        }
        /// <summary>
        /// <para>Create a directional edge connecting two nodes.</para>
        /// <para>Return false if any node DNE, or there's already an edge with the same direction, or the new edge would introduce cycle.</para>
        /// </summary>
        public bool AddEdge(string pSourceNodeId, string pTargetNodeId)
        {
            if (!(this.HasNode(pSourceNodeId) && this.HasNode(pTargetNodeId))
                || this.GetEdge(pSourceNodeId, pTargetNodeId) != null)
                return false;
            Edge tEdge = new(pSourceNodeId, pTargetNodeId, new SEdge<int>(this.mNodes[pSourceNodeId].mGraphId, this.mNodes[pTargetNodeId].mGraphId));
            this.mGraph.AddEdge(tEdge.GetEdge());
            // check cycle
            if (!this.mGraph.IsDirectedAcyclicGraph())
            {
                this.mGraph.RemoveEdge(tEdge.GetEdge());
                return false;
            }
            this.mEdges.Add(tEdge);

            return true;
        }
        /// <summary><para>Options: SOURCE to use edge's source for searching. TARGET for target, and EITHER for either source or target.</para></summary>
        public List<Edge> GetEdgesWithNodeId(string pNodeId, EdgeEndpointOption pOption = EdgeEndpointOption.Either)
        {
            List<Edge> tRes = new();
            foreach (Edge e in this.mEdges)
            {
                if (pOption switch
                    {
                        EdgeEndpointOption.Source => e.StartsWith(pNodeId),
                        EdgeEndpointOption.Target => e.EndsWith(pNodeId),
                        EdgeEndpointOption.Either => e.EitherWith(pNodeId),
                        _ => e.EitherWith(pNodeId)
                    })
                {
                    tRes.Add(e);
                }
            }
            return tRes;
        }
        /// <summary>Return an edge with the same direction, otherwise null.</summary>
        public Edge? GetEdge(string pSourceNodeId, string pTargetNodeId)
        {
            foreach (Edge e in this.mEdges)
            {
                if (e.BothWith(pSourceNodeId, pTargetNodeId)) return e;
            }
            return null;
        }
        /// <summary>Return false if no edge has the endpoint with given type equal to nodeId, otherwise true.</summary>
        public bool RemoveEdgesWithNodeId(string pNodeId, EdgeEndpointOption pOption = EdgeEndpointOption.Either)
        {
            List<Edge> tEdges = this.GetEdgesWithNodeId(pNodeId, pOption);
            if (tEdges.Count == 0) return false;
            foreach (Edge e in tEdges)
            {
                this.mGraph.RemoveEdge(e.GetEdge());
                this.mEdges.Remove(e);
            }
            return true;
        }
        /// <summary>Return true if an edge is removed, otherwise false.</summary>
        public bool RemoveEdge(string pSourceNodeId, string pTargetNodeId)
        {
            var tEdge = this.GetEdge(pSourceNodeId, pTargetNodeId);
            if (tEdge == null) return false;
            this.mGraph.RemoveEdge(tEdge.GetEdge());
            this.mEdges.Remove(tEdge);
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
                if (this._selectedNodes.Contains(pNode.mId)
                    && pNode.mStyle.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
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
                        if (!this._selectedNodes.Contains(pNode.mId))
                            this._selectedNodes.Add(pNode.mId);
                        // remove
                        else
                            this._selectedNodes.Remove(pNode.mId);
                    }
                    // single-selecting node
                    else if (!pInputPayload.mIsALmbDragRelease)
                    {
                        pReadClicks = false;
                        this._selectedNodes.Clear();
                        this._selectedNodes.Add(pNode.mId);
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
                        if (!pInputPayload.mIsKeyCtrl && !this._selectedNodes.Contains(pNode.mId))
                        {
                            tIsNodeHandleClicked = true;
                            this._selectedNodes.Clear();
                            this._selectedNodes.Add(pNode.mId);
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
        public CanvasDrawFlags ProcessInputOnCanvas(UtilsGUI.InputPayload pInputPayload, CanvasDrawFlags pCanvasDrawFlagIn)
        {
            CanvasDrawFlags pCanvasDrawFlags = CanvasDrawFlags.None;
            // Mouse drag
            if (pInputPayload.mLmbDragDelta.HasValue) 
            { 
                this.mMap.AddBaseOffset(pInputPayload.mLmbDragDelta.Value / this.mConfig.scaling);
                pCanvasDrawFlags |= CanvasDrawFlags.StateCanvasDrag;
            }
            // Mouse wheel zooming
            if (!pCanvasDrawFlagIn.HasFlag(CanvasDrawFlags.NoCanvasZooming))
            {
                switch (pInputPayload.mMouseWheelValue)
                {
                    case 1:
                        this.mConfig.scaling += NodeCanvas.stepScale;
                        pCanvasDrawFlags |= CanvasDrawFlags.StateCanvasDrag;
                        break;
                    case -1:
                        this.mConfig.scaling -= NodeCanvas.stepScale;
                        pCanvasDrawFlags |= CanvasDrawFlags.StateCanvasDrag;
                        break;
                };
            }
            return pCanvasDrawFlags;
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
            CanvasDrawFlags pCanvasDrawFlag = CanvasDrawFlags.None,
            List<ViewerNotification>? pNotiListener = null)
        {
            bool tIsAnyNodeHandleClicked = false;
            bool tIsReadingClicksOnNode = true;
            bool tIsAnyNodeClicked = false;
            bool tIsAnySelectedNodeInteracted = false;
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
                    if (this._snappingNode != null && node.mId != this._snappingNode.mId && !this._selectedNodes.Contains(node.mId))  // avoid snapping itself & selected nodess
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

            // =====================
            // Draw
            // =====================
            FirstClickType tFirstClickScanRes = FirstClickType.None;
            bool tIsAnyNodeBusy = false;
            bool tIsLockingSelection = false;
            bool tIsRemovingConn = false;        // for outside node drawing loop

            // Draw edges
            ImDrawListPtr tDrawList = ImGui.GetWindowDrawList();
            List<Edge> tEdgeToRemove = new();
            foreach (Edge e in this.mEdges)
            {
                if (!this.mNodes.TryGetValue(e.GetSourceNodeId(), out var tSourceNode)
                    || !this.mNodes.TryGetValue(e.GetTargetNodeId(), out var tTargetNode)) continue;
                Vector2? tSourceOSP = this.mMap.GetNodeScreenPos(tSourceNode.mId, tCanvasOSP, this.mConfig.scaling);
                if (!tSourceOSP.HasValue) continue;
                Vector2? tTargetOSP = this.mMap.GetNodeScreenPos(tTargetNode.mId, tCanvasOSP, this.mConfig.scaling);
                if (!tTargetOSP.HasValue) continue;

                NodeInteractionFlags tEdgeRes = e.Draw(tDrawList, tSourceOSP.Value, tTargetOSP.Value, pIsHighlighted: this._selectedNodes.Contains(e.GetSourceNodeId()));
                if (tEdgeRes.HasFlag(NodeInteractionFlags.Edge)) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasDrag;
                if (tEdgeRes.HasFlag(NodeInteractionFlags.RequestEdgeRemoval)) tEdgeToRemove.Add(e);
            }
            foreach (var e in tEdgeToRemove) this.RemoveEdge(e.GetSourceNodeId(), e.GetTargetNodeId());
            // Draw nodes
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
                    if (t.Item3)
                    {
                        tIsAnyNodeClicked = true;
                        if (this._selectedNodes.Contains(id)) tIsAnySelectedNodeInteracted = true;
                    }
                    if (t.Item4 != FirstClickType.None)
                    {
                        tFirstClickScanRes = (t.Item4 == FirstClickType.Body && this._selectedNodes.Contains(id))
                                             ? FirstClickType.BodySelected
                                             : t.Item4;
                    }
                    if (tIsAnySelectedNodeInteracted) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasDrag;
                    if (tNode.mStyle.CheckPosWithin(tNodeOSP.Value, this.GetScaling(), pInputPayload.mMousePos)
                        && pInputPayload.mMouseWheelValue != 0
                        && this._selectedNodes.Contains(id))
                    {
                        pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasZooming;
                    }
                    // Select using selectArea
                    if (tSelectScreenArea != null && !this._isNodeBeingDragged && this._firstClickInDrag == FirstClickType.None)
                    {
                        if (tNode.mStyle.CheckAreaIntersect(tNodeOSP.Value, this.mConfig.scaling, tSelectScreenArea))
                        {
                            this._selectedNodes.Add(id);
                        }
                    }
                }

                // Draw using NodeOSP
                NodeInteractionFlags tNodeRes = tNode.Draw(
                                                    tSnapDelta != null && this._selectedNodes.Contains(id)
                                                        ? tNodeOSP.Value + tSnapDelta.Value
                                                        : tNodeOSP.Value,
                                                    this.mConfig.scaling,
                                                    this._selectedNodes.Contains(id),
                                                    pInputPayload,
                                                    pIsEstablishingConn: this._nodeConnTemp != null && this._nodeConnTemp.IsSource(tNode.mId));
                var tNodeRelaPos = this.mMap.GetNodeRelaPos(id);
                if (this._isNodeBeingDragged && this._selectedNodes.Contains(tNode.mId) && tNodeRelaPos.HasValue) 
                    ImGui.GetWindowDrawList().AddText(
                        tNodeOSP.Value + new Vector2(0, -30) * this.GetScaling()
                        , ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeText), 
                        $"({(tNodeRelaPos.Value.X / 10):F1}, {(tNodeRelaPos.Value.Y / 2):F1})");

                if (tNode._isBusy) tIsAnyNodeBusy = true;
                // Process node's content interaction
                if (tNodeRes.HasFlag(NodeInteractionFlags.Internal)) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasInteraction | CanvasDrawFlags.NoNodeDrag | CanvasDrawFlags.NoNodeSnap;
                if (tNodeRes.HasFlag(NodeInteractionFlags.LockSelection))
                {
                    tIsLockingSelection = true;
                }
                if (this._nodeConnTemp != null
                    && pInputPayload.mIsMouseRmb
                    && this._nodeConnTemp.IsSource(tNode.mId)
                    && !(tNodeRes.HasFlag(NodeInteractionFlags.RequestingEdgeConn)))
                {
                    tIsRemovingConn = true;             // abort connection establishing if RMB outside of connecting plug
                }
                // Node connection
                var tConnRes = this._nodeConnTemp?.GetConn();
                if (tConnRes != null)                                                     // implement conn
                {
                    if (!this.AddEdge(tConnRes.Item1, tConnRes.Item2) && pNotiListener != null)
                    {
                        pNotiListener.Add(new($"NodeCanvasEdgeConnection{this.mId}", "Invalid node connection.\n(Connection is either duplicated, or causing cycles)", ViewerNotificationType.Error));
                    }
                    this._nodeConnTemp = null;
                }
                else if (tNodeRes.HasFlag(NodeInteractionFlags.UnrequestingEdgeConn))
                {
                    this._nodeConnTemp = null;
                }
                else if (tNodeRes.HasFlag(NodeInteractionFlags.RequestingEdgeConn))       // setup conn
                {
                    // establishing new conn
                    if (this._nodeConnTemp == null)
                    {
                        this._nodeConnTemp = new(tNode.mId);
                    }
                    // connect to existing conn
                    else
                    {
                        this._nodeConnTemp.Connect(tNode.mId);
                    }
                }
                // Draw conn tether to cursor
                if (this._nodeConnTemp != null && this._nodeConnTemp.IsSource(tNode.mId))
                {
                    tDrawList.AddLine(tNodeOSP.Value, pInputPayload.mMousePos, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg));
                    tDrawList.AddText(pInputPayload.mMousePos, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeText), "[Right-click] another plug to connect.\n[Right-click] elsewhere to cancel.\n\nConnections that cause cycling will not connect. For more info, please hover the question mark on the toolbar.");
                }
            }
            if (tIsRemovingConn && this._nodeConnTemp?.GetConn() == null) this._nodeConnTemp = null;
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
                && !pInputPayload.mIsALmbDragRelease
                && !tIsAnySelectedNodeInteracted)
            {
                this._selectedNodes.Clear();
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
                    foreach (var id in this._selectedNodes)
                    {
                        this.mMap.MoveNodeRelaPos(
                            id,
                            pInputPayload.mLmbDragDelta.Value,
                            this.mConfig.scaling);
                    }
                    pCanvasDrawFlag |= CanvasDrawFlags.StateNodeDrag;
                }
                // Snap if available
                else if (!this._isNodeBeingDragged 
                         && this._lastSnapDelta != null 
                         && (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoNodeDrag) || !pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoNodeSnap)))
                {
                    foreach (var id in this._selectedNodes)
                    {
                        this.mMap.MoveNodeRelaPos(
                            id,
                            this._lastSnapDelta.Value,
                            this.mConfig.scaling);
                    }
                    this._lastSnapDelta = null;
                    pCanvasDrawFlag |= CanvasDrawFlags.StateNodeDrag;
                }
                // Process input on canvas
                if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoCanvasInteraction)
                    && !pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoCanvasDrag)
                    && !this._isNodeBeingDragged 
                    && !tIsAnyNodeClicked
                    && (this._firstClickInDrag == FirstClickType.None || this._firstClickInDrag == FirstClickType.Body)
                    && this._selectAreaOSP == null)
                {
                    pCanvasDrawFlag |= this.ProcessInputOnCanvas(pInputPayload, pCanvasDrawFlag);
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
            Body = 2,
            BodySelected = 3
        }
        private class EdgeConn
        {
            private readonly string source;
            private string? target;

            private EdgeConn() { }
            public EdgeConn(string sourceNodeId) => this.source = sourceNodeId;
            /// <summary>Set the target node. If target is the same as source, return false, otherwise true.</summary>
            public bool Connect(string targetNodeId)
            {
                if (this.source == targetNodeId) return false;
                this.target = targetNodeId;
                return true;
            }
            private bool IsEstablished() => this.target != null;
            public bool IsSource(string nodeId) => this.source == nodeId;
            /// <summary>Get a connection between two nodes. If connection is not established, returns null, otherwise a tuple of (source, target)</summary>
            public Tuple<string, string>? GetConn()
            {
                if (!this.IsEstablished()) return null;
                return new(this.source, this.target!);
            }
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
    public enum EdgeEndpointOption
    {
        None = 0,
        Source = 1,
        Target = 2,
        Either = 3
    }
}
