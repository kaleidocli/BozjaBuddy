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
using static BozjaBuddy.GUI.NodeGraphViewer.NodeCanvas;
using QuickGraph.Algorithms.Search;
using System.Runtime.CompilerServices;
using QuickGraph.Algorithms.ShortestPath;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// <para>Represents a layer of node graph.</para>
    /// <para>Contains info about nodes and its position.</para>
    /// </summary>
    public class NodeCanvas : IDisposable
    {
        public static float minScale = 0.1f;
        public static float maxScale = 2f;
        public static float stepScale = 0.1f;

        public int mId;
        public string mName;
        [JsonProperty]
        private int _nodeCounter { get; set; } = -1;

        [JsonProperty]
        private NodeMap mMap = new();
        [JsonProperty]
        private Dictionary<string, Node> mNodes = new();
        [JsonProperty]
        private HashSet<string> _nodeIds = new();
        [JsonProperty]
        private OccupiedRegion mOccuppiedRegion;       // this shouldn't be serialized, and only initiated using node list and maps.
        [JsonProperty]
        public AdjacencyGraph<int, SEdge<int>> mGraph;        // whatever this is
        [JsonProperty]
        private List<Edge> mEdges = new();

        [JsonProperty]
        private CanvasConfig mConfig { get; set; } = new();

        private bool _isNodeBeingDragged = false;
        private HashSet<string> _selectedNodes = new();
        [JsonProperty]
        private LinkedList<string> _nodeRenderZOrder = new();
        private string? _nodeQueueingHndCtxMnu = null;
        private Node? _snappingNode = null;
        [JsonProperty]
        private Dictionary<int, string> _nodeIdAndNodeGraphId = new();
        private Vector2? _lastSnapDelta = null;
        private FirstClickType _firstClickInDrag = FirstClickType.None;
        private bool _isFirstFrameAfterLmbDown = true;      // specifically for Draw()
        private Vector2? _selectAreaOSP = null;
        private bool _isNodeSelectionLocked = false;
        private EdgeConn? _nodeConnTemp = null;
        private Dictionary<string, string> _cachePathToPTarget = new();
        private Dictionary<string, string> _cachePathToPSource = new();

        public NodeCanvas(int pId, string? pName = null)
        {
            this.mId = pId;
            this.mName = pName == null ? $"Canvas {this.mId}" : pName;
            this.mOccuppiedRegion = new();
            this.mGraph = new();
        }

        /// <summary> Specifically for JsonConverter. </summary>
        public void Init(
            int _nodeCounter, 
            NodeMap mMap,
            Dictionary<string, Node> mNodes,
            HashSet<string> _nodeIds,
            OccupiedRegion mOccuppiedRegion,
            AdjacencyGraph<int, SEdge<int>> mGraph,
            List<Edge> mEdges,
            CanvasConfig mConfig,
            LinkedList<string> _nodeRenderZOrder,
            Dictionary<int, string> _nodeIdAndNodeGraphId)
        {
            this._nodeCounter = _nodeCounter;
            this.mMap = mMap;
            this.mNodes = mNodes;
            this._nodeIds = _nodeIds;
            this.mOccuppiedRegion = mOccuppiedRegion;
            this.mGraph = mGraph;
            this.mEdges = mEdges;
            this.mConfig = mConfig;
            this._nodeRenderZOrder = _nodeRenderZOrder;
            this._nodeIdAndNodeGraphId = _nodeIdAndNodeGraphId;
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
            // create node  (the node id is just a dummy. AddNode() should create from incremental static id val)
            T tNode = new();
            tNode.Init("-1", -1, pNodeContent, _style: new(Vector2.Zero, Vector2.Zero, tNode.GetType() == typeof(AuxNode) ? AuxNode.minHandleSize : null));
            // add node
            if (this.AddNode(tNode, pDrawRelaPos) == null) return null;

            return tNode.mId;
        }
        private string? AddNode(Node pNode, Vector2 pDrawRelaPos)
        {
            int tNewId = this._nodeCounter + 1;
            // assimilate
            pNode._setId(tNewId.ToString());
            pNode.mGraphId = tNewId;
            // add node
            try
            {
                if (this.mNodes.ContainsKey(pNode.mId) || this._nodeIds.Contains(pNode.mId)) return null;       // check nevertheless
                if (!this.mNodes.TryAdd(pNode.mId, pNode)) return null;
                if (!this._nodeIds.Add(pNode.mId)) return null;
                if (!this.mOccuppiedRegion.IsUpdatedOnce()) this.mOccuppiedRegion.Update(this.mNodes, this.mMap);
                this._nodeRenderZOrder.AddLast(pNode.mId);
                this.mMap.AddNode(pNode.mId, pDrawRelaPos);
            }
            catch (Exception e) { PluginLog.LogDebug(e.Message); }

            this.mOccuppiedRegion.Update(this.mNodes, this.mMap);
            // add node vertex to graph
            this.mGraph.AddVertex(pNode.mGraphId);
            this._nodeIdAndNodeGraphId.TryAdd(pNode.mGraphId, pNode.mId);

            this._nodeCounter++;
            return pNode.mId;
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
            return this.AddNode<T>(
                    pNodeContent,
                    this.mOccuppiedRegion.GetAvailableRelaPos(pRelaAreaToScanForAvailableRegion)
                );
        }
        public string? AddNodeWithinViewOffset<T>(
                NodeContent.NodeContent pNodeContent,
                Vector2 pNodeOffset,
                Vector2? pOffsetExtra = null
                                ) where T : Node, new()
        {
            Vector2 tViewerRelaPos = this.mMap.GetBaseOffset();
            return this.AddNode<T>(
                    pNodeContent,
                    -tViewerRelaPos + pNodeOffset + (pOffsetExtra ?? Vector2.Zero) * (1 / this.GetScaling())
                );
        }
        public string? AddNodeWithinViewOffset(
                Node pNode,
                Vector2 pNodeOffset,
                Vector2? pOffsetExtra = null)
        {
            Vector2 tViewerRelaPos = this.mMap.GetBaseOffset();
            return this.AddNode(
                    pNode,
                    -tViewerRelaPos + pNodeOffset + (pOffsetExtra ?? Vector2.Zero) * (1 / this.GetScaling())
                );
        }
        public string? AddNodeAdjacent<T>(
                NodeContent.NodeContent pNodeContent,
                string pNodeIdToAdjoin,
                Vector2? pOffset = null
                ) where T : Node, new()
        {
            if (!this.mNodes.TryGetValue(pNodeIdToAdjoin, out var pPrevNode) || pPrevNode == null) return null;
            Vector2 tRelaPosRes;

            this.mGraph.TryGetOutEdges(pPrevNode.mGraphId, out var edges);
            if ( edges == null ) return null;
            // Getting child nodes that is positioned at greatest Y
            float? tChosenY = null;
            Node? tChosenNode = null;
            foreach (var e in edges)
            {
                PluginLog.LogDebug($"> Evaluating child with graphId={e.Target}");
                string? iChildId = this.GetNodeIdWithNodeGraphId(e.Target);
                if (iChildId == null) continue;
                PluginLog.LogDebug($"> A");
                var iChildRelaPos = this.mMap.GetNodeRelaPos(iChildId);
                if (!iChildRelaPos.HasValue) continue;
                PluginLog.LogDebug($"> cY={iChildRelaPos.Value.Y} > chY={tChosenY}");
                if (!tChosenY.HasValue || iChildRelaPos.Value.Y > tChosenY)
                {
                    tChosenY = iChildRelaPos.Value.Y;
                    this.mNodes.TryGetValue(iChildId, out var val);
                    tChosenNode = val ?? tChosenNode;
                }
            }
            // Calc final draw pos
            if (tChosenNode == null) tRelaPosRes = (this.mMap.GetNodeRelaPos(pNodeIdToAdjoin) ?? Vector2.One) + new Vector2(pPrevNode.mStyle.GetSize().X, 0) + (pOffset ?? Vector2.One);
            else
            {
                tRelaPosRes = new(
                        ((this.mMap.GetNodeRelaPos(pNodeIdToAdjoin) ?? Vector2.One) + new Vector2(pPrevNode.mStyle.GetSize().X, 0) + (pOffset ?? Vector2.One)).X,
                        (this.mMap.GetNodeRelaPos(tChosenNode.mId) ?? this.mMap.GetNodeRelaPos(pNodeIdToAdjoin) ?? Vector2.One).Y + tChosenNode.mStyle.GetSize().Y + (pOffset ?? Vector2.One).Y
                    );
            }

            return this.AddNode<T>(
                    pNodeContent,
                    tRelaPosRes
                );
        }
        public string? AddNodeAdjacent(
                Seed pSeed,
                string pNodeIdToAdjoin)
        {
            var tRes = pSeed.nodeType switch
            {
                BasicNode.nodeType => this.AddNodeAdjacent<BasicNode>(pSeed.nodeContent, pNodeIdToAdjoin, pSeed.ofsToPrevNode),
                BBNode.nodeType => this.AddNodeAdjacent<BBNode>(pSeed.nodeContent, pNodeIdToAdjoin, pSeed.ofsToPrevNode),
                AuxNode.nodeType => this.AddNodeAdjacent<AuxNode>(pSeed.nodeContent, pNodeIdToAdjoin, pSeed.ofsToPrevNode),
                _ => this.AddNodeAdjacent<BasicNode>(pSeed.nodeContent, pNodeIdToAdjoin, pSeed.ofsToPrevNode)
            };
            // Connect node
            if (pSeed.isEdgeConnected && tRes != null)
            {
                var tResRelaPos = this.mMap.GetNodeRelaPos(tRes);
                var tAdjRelaPos = this.mMap.GetNodeRelaPos(pNodeIdToAdjoin);
                bool tIsUpright = (tResRelaPos.HasValue && tAdjRelaPos.HasValue)
                                  ? (tAdjRelaPos.Value.Y > tResRelaPos.Value.Y)
                                  : false;
                this.AddEdge(pNodeIdToAdjoin, tRes, pSquarePathing: true, pUpright: tIsUpright);
            }

            return tRes;
        }
        /// <summary>
        /// Return false if the process partially/fully fails.
        /// </summary>
        public bool RemoveNode(string pNodeId, bool _isUpdatingOccupiedRegion = true)
        {
            bool tRes = true;
            int? tNodeGraphId = null;
            if (!this.mMap.RemoveNode(pNodeId)) tRes = false;
            if (mNodes.TryGetValue(pNodeId, out Node? tNode) && tNode != null) 
            {
                tNodeGraphId = tNode.mGraphId;
                this.mNodes[pNodeId].Dispose();
            }
            if (!this.mNodes.Remove(pNodeId)) tRes = false;
            if (!_nodeIds.Remove(pNodeId)) tRes = false;
            this._selectedNodes.Remove(pNodeId);
            this._nodeRenderZOrder.Remove(pNodeId);

            // Also removing pack nodes (place this above OccupiedRegion.Update())
            if (tNode != null && tNode.mPackingStatus == Node.PackingStatus.PackingDone)
            {
                foreach (string packMemId in tNode.mPack)
                {
                    if (!this.mNodes.ContainsKey(packMemId)) continue;
                    this.RemoveNode(packMemId, _isUpdatingOccupiedRegion: false);
                }
            }

            if (_isUpdatingOccupiedRegion) this.mOccuppiedRegion.Update(this.mNodes, this.mMap);
            // Graph stuff
            this.RemoveEdgesContainingNodeId(pNodeId);
            if (tNodeGraphId.HasValue)
            {
                this._nodeIdAndNodeGraphId.Remove(tNodeGraphId.Value);
                this.mGraph.RemoveVertex(tNodeGraphId.Value);
            }


            return tRes;
        }
        private string? GetNodeIdWithNodeGraphId(int pNodeGraphId)
        {
            this._nodeIdAndNodeGraphId.TryGetValue(pNodeGraphId, out var iChildId);
            return iChildId;
        }
        /// <summary>
        /// <para>Create a directional edge connecting two nodes.</para>
        /// <para>Return false if any node DNE, or there's already an edge with the same direction, or the new edge would introduce cycle.</para>
        /// </summary>
        public bool AddEdge(string pSourceNodeId, string pTargetNodeId, bool pSquarePathing = false, bool pUpright = true)
        {
            if (!(this.HasNode(pSourceNodeId) && this.HasNode(pTargetNodeId))
                || this.GetEdge(pSourceNodeId, pTargetNodeId) != null)
                return false;
            Edge tEdge = new(pSourceNodeId, pTargetNodeId, new SEdge<int>(this.mNodes[pSourceNodeId].mGraphId, this.mNodes[pTargetNodeId].mGraphId), pSquarePathing: pSquarePathing, pUpright: pUpright);
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
        public bool RemoveEdgesContainingNodeId(string pNodeId, EdgeEndpointOption pOption = EdgeEndpointOption.Either)
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
        /// <summary> Returns true if import successfully, otherwise false.</summary>
        public bool ImportNodes(string pDataJson)
        {
            if (pDataJson == string.Empty) return false;
            PartialCanvasData? tData;
            try
            {
                tData = JsonConvert.DeserializeObject<PartialCanvasData>(pDataJson, new JsonConverters.PartialCanvasDataConverter());
            }
            catch (Newtonsoft.Json.JsonReaderException _)
            {
                return false;
            }
            Dictionary<string, string> tNodeIdOldToNew = new();
            if (tData == null) return false;

            // add nodes
            foreach (Node n in tData.nodes)
            {
                string oldNodeId = n.mId;
                string? newNodeId;
                if (tData.offsetFromAnchor.TryGetValue(n.mId, out var ofs))
                    newNodeId = this.AddNodeWithinViewOffset(n, ofs, pOffsetExtra: new(-30, -30));
                else
                    newNodeId = this.AddNodeWithinViewOffset(n, Vector2.Zero);
                if (newNodeId != null)
                {
                    tNodeIdOldToNew.TryAdd(oldNodeId, newNodeId);
                }
            }
            // translating edges
            foreach (Edge e in tData.relatedEdges)
            {
                if (!tNodeIdOldToNew.TryGetValue(e.GetSourceNodeId(), out var newSourceId) || newSourceId == null) continue;
                if (!tNodeIdOldToNew.TryGetValue(e.GetTargetNodeId(), out var newTargetId) || newTargetId == null) continue;

                this.AddEdge(newSourceId, newTargetId, pSquarePathing: e.IsSquarePathing(), pUpright: e.IsDrawingUpRight());
            }
            return true;    
        }
        protected PartialCanvasData? ExportNodes(HashSet<string> pNodeIds)
        {
            if (pNodeIds.Count == 0) return null;
            PartialCanvasData tData = new();
            string tNodeWithSmallestX = pNodeIds.First();
            float? tSmallestX = this.mMap.GetNodeRelaPos(tNodeWithSmallestX).HasValue ? this.mMap.GetNodeRelaPos(tNodeWithSmallestX)!.Value.X : null;

            foreach (string nid in pNodeIds)
            {
                if (!this.mNodes.TryGetValue(nid, out var node) || node == null) continue;
                tData.nodes.Add(node);
                // smallest X
                var tRelaPos = this.mMap.GetNodeRelaPos(nid);
                if ((tRelaPos.HasValue && tSmallestX.HasValue && tRelaPos.Value.X < tSmallestX.Value)
                    || (!tSmallestX.HasValue && tRelaPos.HasValue))
                {
                    tSmallestX = tRelaPos.Value.X;
                    tNodeWithSmallestX = nid;
                }
            }
            foreach (Edge e in this.mEdges)
            {
                if (!pNodeIds.Contains(e.GetSourceNodeId()) || !pNodeIds.Contains(e.GetTargetNodeId()))
                    continue;
                tData.relatedEdges.Add(e);
            }
            // position-related info. Get offsets.
            tData.anchorNodeId = tNodeWithSmallestX;
            Vector2? tAnchorRelaPos = this.mMap.GetNodeRelaPos(tData.anchorNodeId);
            if (tAnchorRelaPos == null) return null;
            foreach (Node n in tData.nodes)
            {
                var relaPos = this.mMap.GetNodeRelaPos(n.mId);
                if (relaPos == null)
                {
                    tData.offsetFromAnchor.TryAdd(n.mId, Vector2.Zero);
                    continue;
                }
                tData.offsetFromAnchor.TryAdd(n.mId, relaPos.Value - tAnchorRelaPos.Value);
            }

            return tData;
        }
        protected string? ExportNodesAsJson(HashSet<string> pNodeIds, string? _ = null)
        {
            PartialCanvasData? tData = this.ExportNodes(pNodeIds);
            if (tData == null) return null;
            return JsonConvert.SerializeObject(tData, Formatting.Indented);
        }
        /// <summary> Returns json data of seleected nodes (and related edges). If export fails, returns null. </summary>
        public string? ExportSelectedNodes()
        {
            if (this._selectedNodes.Count == 0) return null;
            return this.ExportNodesAsJson(this._selectedNodes);
        }
        public void MoveCanvas(Vector2 pDelta)
        {
            this.mMap.AddBaseOffset(pDelta);
        }
        public void MinimizeSelectedNodes()
        {
            foreach (var nid in this._selectedNodes)
            {
                if (this.mNodes.TryGetValue(nid, out var n) && n != null)
                {
                    n.Minimize();
                }
            }
        }
        public void UnminimizeSelectedNodes()
        {
            foreach (var nid in this._selectedNodes)
            {
                if (this.mNodes.TryGetValue(nid, out var n) && n != null)
                {
                    n.Unminimize();
                }
            }
        }
        public void RemoveSelectedNodes()
        {
            foreach (var nid in this._selectedNodes)
            {
                this.RemoveNode(nid);
            }
        }
        /// <summary>Might be useful for caller to detect changes in selected nodes of on this canvas.</summary>
        public int GetSelectedCount() => this._selectedNodes.Count;
        /// <summary>Will not pack if node does not have PackingUnderway status</summary>
        private void PackNode(Node pPacker)
        {
            if (pPacker.mPackingStatus != Node.PackingStatus.PackingUnderway) return;
            if (!this.mGraph.TryGetOutEdges(pPacker.mGraphId, out var tEdges) || tEdges == null) return;
            this._packNodeWalker(pPacker.mGraphId, ref pPacker.mPack, pPacker.mId);
            if (pPacker.mPack.Count == 0)
            {
                pPacker.mPackingStatus = Node.PackingStatus.None;
                return;
            }
            pPacker._relaPosLastPackingCall = this.mMap.GetNodeRelaPos(pPacker.mId);
            pPacker.mPackingStatus = Node.PackingStatus.PackingDone;
        }
        private void _packNodeWalker(int pId, ref HashSet<string> pNodeIds, string pPackerNodeId)
        {
            if (!this.mGraph.TryGetOutEdges(pId, out var tEdges) || tEdges == null) return;
            foreach (var outEdge in tEdges)
            {
                var tTargetNodeId = this.GetNodeIdWithNodeGraphId(outEdge.Target);
                if (tTargetNodeId == null) continue;
                if (!this.mNodes.TryGetValue(tTargetNodeId, out var n) || n == null) continue;

                if (n.mPackerNodeId != null) continue;      // ignore nodes that are already packed

                n.mIsPacked = true;
                n.mPackerNodeId = pPackerNodeId;
                pNodeIds.Add(tTargetNodeId);
                // free cache path + packer
                this._cachePathToPTarget.Clear();
                this._cachePathToPSource.Clear();
                this._packNodeWalker(outEdge.Target, ref pNodeIds, pPackerNodeId);
            }
        }
        /// <summary>
        /// Will unpack if node's status is None and its pack still not empty.
        /// <para>A node can only be unpacked by its packer.</para>
        /// </summary>
        private void UnpackNode(Node pPacker)
        {
            if (pPacker.mPackingStatus != Node.PackingStatus.UnpackingUnderway || pPacker.mPack.Count == 0) return;
            Vector2? tRelaPosDelta = null;
            var tCurrRelaPos = this.mMap.GetNodeRelaPos(pPacker.mId);
            if (pPacker._relaPosLastPackingCall.HasValue && tCurrRelaPos.HasValue)
            {
                tRelaPosDelta =  tCurrRelaPos - pPacker._relaPosLastPackingCall.Value;
            }
            foreach (string packedNodeId in pPacker.mPack)
            {
                if (!this.mNodes.TryGetValue(packedNodeId, out var n) || n == null) continue;
                
                if (n.mPackerNodeId == pPacker.mId)     // only packer can unpack the node
                {
                    n.mPackerNodeId = null;
                    n.mIsPacked = false;

                    // Set pack's nodes to new pos
                    var npos = this.mMap.GetNodeRelaPos(n.mId);
                    if (tRelaPosDelta.HasValue && npos.HasValue)
                    {
                        this.mMap.SetNodeRelaPos(n.mId, npos.Value + tRelaPosDelta.Value);
                    }
                }
            }
            pPacker.mPack.Clear();
            pPacker._relaPosLastPackingCall = null;
            pPacker.mPackingStatus = Node.PackingStatus.None;
            // free cache path + packer
            this._cachePathToPTarget.Clear();
            this._cachePathToPSource.Clear();
        }
        private void SelectAllChild(Node pParent)
        {
            if (!this.mGraph.TryGetOutEdges(pParent.mGraphId, out var tEdges) || tEdges == null) return;
            this._selectedNodes.Add(pParent.mId);
            this._selectAllChildWalker(pParent.mGraphId);
        }
        private void _selectAllChildWalker(int pId)
        {
            if (!this.mGraph.TryGetOutEdges(pId, out var tEdges) || tEdges == null) return;
            foreach (var outEdge in tEdges)
            {
                var tTargetNodeId = this.GetNodeIdWithNodeGraphId(outEdge.Target);
                if (tTargetNodeId == null) continue;
                this._selectedNodes.Add(tTargetNodeId);

                this._selectAllChildWalker(outEdge.Target);
            }
        }
        public NodeInputProcessResult ProcessInputOnNode(Node pNode, Vector2 pNodeOSP, UtilsGUI.InputPayload pInputPayload, bool pReadClicks)
        {
            bool tIsNodeHandleClicked = false;
            bool tIsNodeClicked = false;
            bool tIsCursorWithin = pNode.mStyle.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos);
            bool tIsCursorWithinHandle = pNode.mStyle.CheckPosWithinHandle(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos);
            bool tIsMarkedForDelete = false;
            bool tIsMarkedForSelect = false;
            bool tIsMarkedForDeselect = false;
            bool tIsReqqingClearSelect = false;
            bool tIsEscapingMultiselect = false;
            FirstClickType tFirstClick = FirstClickType.None;
            CanvasDrawFlags tCDFRes = CanvasDrawFlags.None;

            // Process node delete (we don't delete node in this method, but pass it to Draw())
            if (pReadClicks && !tIsNodeHandleClicked && pInputPayload.mIsMouseMid)
            {
                if (tIsCursorWithinHandle) tIsMarkedForDelete = true;
            }
            else if (pNode._isMarkedDeleted)
            {
                tIsMarkedForDelete = true;
            }
            // Process node select (on lmb release)
            if (pReadClicks && !tIsNodeHandleClicked && pInputPayload.mIsMouseLmb)
            {
                if (tIsCursorWithinHandle)
                {
                    tIsNodeClicked = true;
                    tIsNodeHandleClicked = true;
                    // single-selecting a node and deselect other node (while in multiselecting)
                    if (!pInputPayload.mIsKeyCtrl && !pInputPayload.mIsALmbDragRelease && this._selectedNodes.Count > 1)
                    {
                        tIsEscapingMultiselect = true;
                        //pReadClicks = false;
                        tIsReqqingClearSelect = true;
                        tIsMarkedForSelect = true;
                    }
                }
                else if (tIsCursorWithin)
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
                    if (tIsCursorWithin)
                    {
                        if (tIsCursorWithinHandle)
                            tFirstClick = FirstClickType.Handle;
                        else
                            tFirstClick = FirstClickType.Body;
                    }
                }

                if (!this._isNodeBeingDragged
                    && tFirstClick != FirstClickType.None
                    && !tIsNodeHandleClicked)
                    
                {
                    if (tFirstClick == FirstClickType.Handle)
                    {
                        tIsNodeHandleClicked = true;
                        // multi-selecting
                        if (pInputPayload.mIsKeyCtrl)
                        {
                            // select (should be true, regardless of node's select status)
                            tIsMarkedForSelect = true;
                            // remove (process selecting first, then deselecting the node)
                            if (this._selectedNodes.Contains(pNode.mId))
                                tIsMarkedForDeselect = true;
                        }
                        // single-selecting new node
                        else if (!pInputPayload.mIsKeyCtrl)     // don't check if node is alrady selected here
                        {
                            this._snappingNode = pNode;
                            if (!this._selectedNodes.Contains(pNode.mId)) tIsReqqingClearSelect = true;
                            tIsMarkedForSelect = true;
                        }
                    }
                    else if (tFirstClick == FirstClickType.Body)
                    {
                        tIsNodeClicked = true;
                    }
                }

                // determine node drag
                if (!this._isNodeBeingDragged
                    && this._firstClickInDrag == FirstClickType.Handle
                    && !pInputPayload.mIsKeyCtrl
                    && !pInputPayload.mIsKeyShift)
                {
                    if (pInputPayload.mLmbDragDelta != null)
                    {
                        this._isNodeBeingDragged = true;
                    }
                }
            }
            else
            {
                this._isNodeBeingDragged = false;
                this._snappingNode = null;
            }

            if (tIsCursorWithin) tCDFRes |= CanvasDrawFlags.NoCanvasZooming;

            NodeInputProcessResult tRes = new()
            {
                isNodeHandleClicked = tIsNodeHandleClicked,
                readClicks = pReadClicks,
                isNodeClicked = tIsNodeClicked,
                firstClick = tFirstClick,
                CDFRes = tCDFRes,
                isMarkedForDelete = tIsMarkedForDelete,
                isWithin = tIsCursorWithin,
                isWithinHandle = tIsCursorWithinHandle,
                isMarkedForSelect = tIsMarkedForSelect,
                isMarkedForDeselect = tIsMarkedForDeselect,
                isReqqingClearSelect = tIsReqqingClearSelect,
                isEscapingMultiselect = tIsEscapingMultiselect
            };

            return tRes;
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
        /// <para>Base isn't necessarily Viewer. It is recommended that Base is a point in the center of Viewer.</para>
        /// </summary>
        public CanvasDrawFlags Draw(
            Vector2 pBaseOSP,               // Base isn't necessarily Viewer. In this case, Base is a point in the center of Viewer.
            Vector2 pViewerOSP,         // Viewer OSP.
            Vector2 pViewerSize,
            Vector2 pInitBaseOffset,
            float pGridSnapProximity,
            UtilsGUI.InputPayload pInputPayload,
            ImDrawListPtr pDrawList,
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
            Vector2 tCanvasOSP = pBaseOSP + this.mMap.GetBaseOffset() * this.mConfig.scaling;

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
                        tSnapOSP = pSnapData?.GetClosestSnapPos(tNodeOSP.Value, pGridSnapProximity);
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
            List<Edge> tEdgeToRemove = new();
            List<Tuple<string, string>> tPEdges = new();
            foreach (Edge e in this.mEdges)
            {
                if (!this.mNodes.TryGetValue(e.GetSourceNodeId(), out var tSourceNode)
                    || !this.mNodes.TryGetValue(e.GetTargetNodeId(), out var tTargetNode)) continue;

                // Check pack
                //if (tSourceNode.mIsPacked) continue;        // ignore if source is packed
                string? _currTPackerId = tTargetNode.mPackerNodeId;
                string? _currSPackerId = tSourceNode.mPackerNodeId;
                Node? tFinalTarget = null;          // null if there's no pack or there's something wrong with pack data
                Node? tFinalSource = null;
                if (tTargetNode.mIsPacked && _currTPackerId == null) continue;
                if (tSourceNode.mIsPacked && _currSPackerId == null) continue;
                // Case: Target is also a PSource
                if (tTargetNode.mPackerNodeId != null && tTargetNode.mPackerNodeId == tSourceNode.mId)
                {
                    continue;         // avoid drawing edge from packer to packed node.
                }
                // Getting final target
                string path = tSourceNode.mId + tTargetNode.mId;
                if (this._cachePathToPTarget.TryGetValue(path, out var PTargetId) && PTargetId != null)     // try retrieving finalTarget from cache
                {
                    if (this.mNodes.TryGetValue(PTargetId, out var PTarget) && PTarget != null)
                    {
                        tFinalTarget = PTarget;
                        _currTPackerId = null;
                    }
                }
                else
                {
                    while (_currTPackerId != null)
                    {
                        if (this.mNodes.TryGetValue(_currTPackerId, out var iFinalTarget) && iFinalTarget != null)
                        {
                            tFinalTarget = iFinalTarget;
                            _currTPackerId = tFinalTarget.mPackerNodeId;
                        }
                    }
                    if (tFinalTarget != null)
                    {
                        this._cachePathToPTarget.Add(path, tFinalTarget.mId);     // cache the path
                    }
                }
                // Getting final source
                if (this._cachePathToPSource.TryGetValue(path, out var PSourceId) && PSourceId != null)
                {
                    if (this.mNodes.TryGetValue(PSourceId, out var PSource) && PSource != null)
                    {
                        tFinalSource = PSource;
                        _currSPackerId = null;
                    }
                }
                else
                {
                    while (_currSPackerId != null)
                    {
                        if (this.mNodes.TryGetValue(_currSPackerId, out var iFinalSource) && iFinalSource != null)
                        {
                            tFinalSource = iFinalSource;
                            _currSPackerId = tFinalSource.mPackerNodeId;
                        }
                    }
                    if (tFinalSource != null)
                    {
                        this._cachePathToPSource.Add(path, tFinalSource.mId);
                    }
                }

                if (tFinalTarget != null && tFinalTarget.mId == tSourceNode.mId) continue;         // avoid drawing edge from packer to packed node.
                if (tFinalSource != null && tFinalSource.mId == tSourceNode.mId) continue;
                if (tFinalSource != null && tFinalTarget != null && tFinalSource.mId == tFinalTarget.mId) continue;

                Vector2? tFinalSourceOSP = this.mMap.GetNodeScreenPos(tFinalSource != null ? tFinalSource.mId : tSourceNode.mId, tCanvasOSP, this.mConfig.scaling);
                if (!tFinalSourceOSP.HasValue) continue;
                Vector2? tFinalTargetOSP = this.mMap.GetNodeScreenPos(tFinalTarget != null ? tFinalTarget.mId : tTargetNode.mId, tCanvasOSP, this.mConfig.scaling);
                if (!tFinalTargetOSP.HasValue) continue;

                // Skip rendering if both ends of an edge is out of view
                if (!Utils.IsLineIntersectRect(tFinalSourceOSP.Value, tFinalTargetOSP.Value, new(pViewerOSP, pViewerSize)))
                {
                    continue;
                }

                NodeInteractionFlags tEdgeRes = e.Draw(pDrawList, tFinalSourceOSP.Value, tFinalTargetOSP.Value, pIsHighlighted: this._selectedNodes.Contains(e.GetSourceNodeId()), pIsTargetPacked: tFinalTarget != null || tFinalSource != null);
                if (tEdgeRes.HasFlag(NodeInteractionFlags.Edge)) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasDrag;
                if (tEdgeRes.HasFlag(NodeInteractionFlags.RequestEdgeRemoval)) tEdgeToRemove.Add(e);
            }
            foreach (var e in tEdgeToRemove) this.RemoveEdge(e.GetSourceNodeId(), e.GetTargetNodeId());
            // Draw nodes
            List<Tuple<Seed, string>> tSeedToAdd = new();
            Stack<LinkedListNode<string>> tNodeToFocus = new();
            Stack<string> tNodeToSelect = new();
            Stack<string> tNodeReqqingHandleCtxMenu = new();
            List<string> tNodeToDeselect = new();
            string? tNodeToDelete = null;
            HashSet<string> tNodesReqqingClearSelect = new();
            bool tIsEscapingMultiselect = false;
            for (var znode = this._nodeRenderZOrder.First; znode != null; znode = znode?.Next)
            {
                if (znode == null) break;
                var id = znode.Value;
                // Get NodeOSP
                Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(id, tCanvasOSP, this.mConfig.scaling);
                if (tNodeOSP == null) continue;
                if (!this.mNodes.TryGetValue(id, out var tNode) || tNode == null) continue;

                // Packing / Check pack
                if (tNode.mIsPacked) continue;
                switch (tNode.mPackingStatus)
                {
                    case Node.PackingStatus.PackingUnderway: this.PackNode(tNode); break;
                    case Node.PackingStatus.UnpackingUnderway: this.UnpackNode(tNode); break;
                }
                // Skip rendering if node is out of view
                if (((tNodeOSP.Value.X + tNode.mStyle.GetSizeScaled(this.GetScaling()).X) < pViewerOSP.X || tNodeOSP.Value.X > pViewerOSP.X + pViewerSize.X)
                    || ((tNodeOSP.Value.Y + tNode.mStyle.GetSizeScaled(this.GetScaling()).Y) < pViewerOSP.Y || tNodeOSP.Value.Y > pViewerOSP.Y + pViewerSize.Y))
                {
                    continue;
                }

                // Process input on node
                // We record the inputs of each individual node.
                // Then, we evaluate those recorded inputs in context of z-order, determining which one we need and which we don't.
                if (!pCanvasDrawFlag.HasFlag(CanvasDrawFlags.NoInteract) && !this._isNodeSelectionLocked)
                {
                    // Process input on node    (tIsNodeHandleClicked, pReadClicks, tIsNodeClicked, tFirstClick)
                    var t = this.ProcessInputOnNode(tNode, tNodeOSP.Value, pInputPayload, tIsReadingClicksOnNode);
                    {
                        if (t.firstClick != FirstClickType.None)
                        {
                            tFirstClickScanRes = (t.firstClick == FirstClickType.Body && this._selectedNodes.Contains(id))
                                                 ? FirstClickType.BodySelected
                                                 : t.firstClick;
                        }
                        if (t.isEscapingMultiselect) tIsEscapingMultiselect = true;
                        if (t.isNodeHandleClicked)
                        {
                            tIsAnyNodeHandleClicked = t.isNodeHandleClicked;
                        }
                        if (t.isNodeHandleClicked && pInputPayload.mIsMouseLmbDown)
                        {
                            // Queue the focus nodes
                            if (znode != null)
                            {
                                tNodeToFocus.Push(znode);
                            }
                        }
                        else if (pInputPayload.mIsMouseLmbDown && t.isWithin && !t.isWithinHandle)     // if an upper node's body covers the previously chosen nodes, discard the focus/selection queue.
                        {
                            tNodeToFocus.Clear();
                            tNodeToSelect.Clear();
                            tFirstClickScanRes = tFirstClickScanRes == FirstClickType.BodySelected 
                                                 ? FirstClickType.BodySelected 
                                                 : FirstClickType.Body;
                        }
                        tIsReadingClicksOnNode = t.readClicks;
                        if (t.isNodeClicked)
                        {
                            tIsAnyNodeClicked = true;
                            if (this._selectedNodes.Contains(id)) tIsAnySelectedNodeInteracted = true;
                        }
                        pCanvasDrawFlag |= t.CDFRes;
                        if (t.isMarkedForDelete)       // proc node delete, only one node deletion allowed per button interaction.
                                                                                // If the interaction picks up multiple nodes, choose the highest one in the z-order (last one rendered)
                        {
                            tNodeToDelete = tNode.mId;      // the upper node will override the lower chosen node
                        }
                        else if (t.isNodeClicked && !t.isNodeHandleClicked)     // if an upper node's body covers a lower node that was chosen, nullify the deletion call.
                        {
                            tNodeToDelete = null;
                        }
                        if (t.isMarkedForSelect)                                       // Process node adding
                                                                                       // prevent marking multiple handles with a single lmbDown. Get the uppest node.
                        {
                            if (pInputPayload.mIsMouseLmbDown)      // this one is for lmbDown. General use.
                            {
                                if (pInputPayload.mIsKeyCtrl || tNodeToSelect.Count == 0) tNodeToSelect.Push(tNode.mId);
                                else if (tNodeToSelect.Count != 0)
                                {
                                    tNodeToSelect.Pop(); 
                                    tNodeToSelect.Push(tNode.mId);
                                }
                            }
                            else if (pInputPayload.mIsMouseLmb && t.isEscapingMultiselect)     // this one is for lmbClick. Used for when the node is marked at lmb is lift up.
                            {
                                tNodeToSelect.TryPop(out var _);
                                tNodeToSelect.Push(tNode.mId);
                            }
                        }
                        if (t.isMarkedForDeselect)
                        {
                            tNodeToDeselect.Add(tNode.mId);
                        }
                        if (t.isReqqingClearSelect) tNodesReqqingClearSelect.Add(tNode.mId);
                        // Handle ctx menu
                        if (t.isWithinHandle && pInputPayload.mIsMouseRmb) tNodeReqqingHandleCtxMenu.Push(tNode.mId);
                    }
                    
                    if (tNode.mStyle.CheckPosWithin(tNodeOSP.Value, this.GetScaling(), pInputPayload.mMousePos)
                        && pInputPayload.mMouseWheelValue != 0
                        && this._selectedNodes.Contains(id))
                    {
                        pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasZooming;
                    }
                    // Select using selectArea
                    if (tSelectScreenArea != null && !this._isNodeBeingDragged && this._firstClickInDrag != FirstClickType.Handle)
                    {
                        if (tNode.mStyle.CheckAreaIntersect(tNodeOSP.Value, this.mConfig.scaling, tSelectScreenArea))
                        {
                            if (this._selectedNodes.Add(id) && znode != null) 
                                tNodeToFocus.Push(znode);
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
                                                    ImGui.GetWindowDrawList(),
                                                    pIsEstablishingConn: this._nodeConnTemp != null && this._nodeConnTemp.IsSource(tNode.mId),
                                                    pIsDrawingHndCtxMnu: this._nodeQueueingHndCtxMnu != null && tNode.mId == this._nodeQueueingHndCtxMnu);
                if (this._nodeQueueingHndCtxMnu != null && tNode.mId == this._nodeQueueingHndCtxMnu)
                    this._nodeQueueingHndCtxMnu = null;
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
                if (tNodeRes.HasFlag(NodeInteractionFlags.RequestSelectAllChild))
                {
                    pCanvasDrawFlag |= CanvasDrawFlags.NoInteract;      // prevent node unselect
                    this.SelectAllChild(tNode);
                }
                // Get seed and grow if possible
                Seed? tSeed = tNode.GetSeed();
                if (tSeed != null) tSeedToAdd.Add(new(tSeed, tNode.mId));
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
                    pDrawList.AddLine(tNodeOSP.Value, pInputPayload.mMousePos, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg));
                    pDrawList.AddText(pInputPayload.mMousePos, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeText), "[Right-click] another plug to connect, elsewhere to cancel.\n\nConnection will fail if it causes cycling or repeated graph.");
                }
            }
            // Node interaction z-order process (order of op: clearing > selecting > deselecting)
            if (tNodeToSelect.Count != 0) pCanvasDrawFlag |= CanvasDrawFlags.NoCanvasDrag;
            if (tNodeToFocus.TryPeek(out var topF) && tNodesReqqingClearSelect.Contains(topF.Value)
                || tIsEscapingMultiselect)      // only accept a clear-select-req from a node that is on top of the focus queue
            {
                this._selectedNodes.Clear();
            }
            foreach (var tId in tNodeToSelect) this._selectedNodes.Add(tId);
            foreach (var tId in tNodeToDeselect) this._selectedNodes.Remove(tId);
            if (tNodeToDelete != null) this.RemoveNode(tNodeToDelete);
            if (tNodeReqqingHandleCtxMenu.TryPeek(out var tNodeReqCtxMnu) && tNodeReqCtxMnu != null)        // handle ctx menu
            {
                this._nodeQueueingHndCtxMnu = tNodeReqCtxMnu;
            }
            // Bring to focus (only get the top node)
            if (tNodeToFocus.Count != 0)
            {
                var zFocusNode = tNodeToFocus.Pop();
                if (zFocusNode != null)
                {
                    this._nodeRenderZOrder.Remove(zFocusNode);
                    this._nodeRenderZOrder.AddLast(zFocusNode);
                }
            }
            foreach (var pair in tSeedToAdd)
            {
                this.AddNodeAdjacent(pair.Item1, pair.Item2);
            }
            if (tIsRemovingConn && this._nodeConnTemp?.GetConn() == null) this._nodeConnTemp = null;
            if (tIsLockingSelection) this._isNodeSelectionLocked = true;
            else this._isNodeSelectionLocked = false;
            // Capture drag's first click. State Body or Handle can only be accessed from state None.
            if (pInputPayload.mIsMouseLmb) this._firstClickInDrag = FirstClickType.None;
            else if (pInputPayload.mIsMouseLmbDown && this._firstClickInDrag == FirstClickType.None && tFirstClickScanRes != FirstClickType.None)
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
                ImGui.GetForegroundDrawList().AddRectFilled(tSelectScreenArea.start, tSelectScreenArea.end, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, 0.5f)));
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
                    && (!this._isNodeBeingDragged || (this._isNodeBeingDragged && this._firstClickInDrag != FirstClickType.Handle))
                    && !tIsAnyNodeClicked
                    && (this._firstClickInDrag == FirstClickType.None || this._firstClickInDrag == FirstClickType.Body)
                    && this._selectAreaOSP == null)
                {
                    pCanvasDrawFlag |= this.ProcessInputOnCanvas(pInputPayload, pCanvasDrawFlag);
                }
            }

            // Mass delete nodes
            if (pInputPayload.mIsKeyDel)
            {
                foreach (var id in this._selectedNodes)
                {
                    this.RemoveNode(id);
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
        public class PartialCanvasData
        {
            public List<Node> nodes = new();
            public List<Edge> relatedEdges = new();

            // position-related info
            public string? anchorNodeId = null;
            public Dictionary<string, Vector2> offsetFromAnchor = new();
        }
        public struct NodeInputProcessResult
        {
            public bool isNodeHandleClicked = false;
            public bool isNodeClicked = false;
            public bool isWithin = false;
            public bool isWithinHandle = false;
            public bool isMarkedForDelete = false;
            public bool isMarkedForSelect = false;
            public bool isMarkedForDeselect = false;
            public bool isReqqingClearSelect = false;
            public bool isEscapingMultiselect = false;
            public FirstClickType firstClick = FirstClickType.None;
            public CanvasDrawFlags CDFRes = CanvasDrawFlags.None;
            public bool readClicks = false;
            public NodeInputProcessResult() { }
        }

        public class Seed
        {
            public string nodeType;
            public NodeContent.NodeContent nodeContent;
            public bool isEdgeConnected;
            public Vector2? ofsToPrevNode;

            private Seed() { }
            public Seed(string nodeType, NodeContent.NodeContent nodeContent, bool isEdgeConnected = true, Vector2? ofsToPrevNode = null)
            {
                this.nodeType = nodeType;
                this.nodeContent = nodeContent;
                this.isEdgeConnected = isEdgeConnected;
                this.ofsToPrevNode = ofsToPrevNode;
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
