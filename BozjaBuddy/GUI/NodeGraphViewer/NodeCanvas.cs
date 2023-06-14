using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;
using BozjaBuddy.Utils;

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

        private bool _isMouseDrag = true;
        private Vector2? _lastDragPos = null;
        private DateTime _lastMouseWheelTime = DateTime.Now;
        private string? _draggingNode = null;
        private HashSet<string> _selectedNode = new();

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
            tNode.Init(tNewId.ToString());
            tNode.SetHeader(pHeader);
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
                    this.mOccuppiedRegion.GetAvailableRelaPos(pCorner, pDirection, pPadding ?? this.mConfig.nodePadding)
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
        private Vector2? CaptureMouseDra()
        {
            Vector2? tRes = null;
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (this._lastDragPos == null)
                {
                    this._lastDragPos = ImGui.GetMousePos();
                    return tRes;
                }
                var d = ImGui.GetMouseDragDelta();
                if (this._isMouseDrag)
                {
                    tRes = d - this._lastDragPos;
                }

                this._isMouseDrag = true;
                this._lastDragPos = d;
            }
            else
            {
                this._isMouseDrag = false;
            }
            return tRes;
        }
        public InputFlag Draw(Vector2 pBaseOriginScreenPos, UtilsGUI.InputPayload pInputPayload)
        {
            // Get this canvas' origin' screenPos
            Vector2 tCanvasOSP = pBaseOriginScreenPos + this.mMap.GetBaseOffset();

            // Draw
            foreach (var id in this._nodeIds)
            {
                // Get NodeOSP
                Vector2? tNodeOSP = this.mMap.GetNodeScreenPos(id, tCanvasOSP, this.mConfig.scaling);
                if (tNodeOSP == null) continue;
                if (!this.mNodes.TryGetValue(id, out var tNode) || tNode == null) continue;
                // Process node select
                // Process node holding and dragging
                if (pInputPayload.mIsMouseLmbDown)      // if mouse is hold, and the holding's first pos is within node
                {                                       // then mark the node as being dragged
                    if (this._draggingNode == null      // as long as the mouse is hold, even if mouse then moving out of node zone
                        && tNode.CheckPosWithin(tNodeOSP.Value, this.mConfig.scaling, pInputPayload.mMousePos))
                    {
                        this._draggingNode = tNode.mId;
                    }
                }
                else
                {
                    this._draggingNode = null;
                }
                // Draw using NodeOSP
                tNode.Draw(tNodeOSP.Value, this.mConfig.scaling);
            }

            // Mouse drag
            if (pInputPayload.mMouseDragDelta.HasValue) this.mMap.AddBaseOffset(pInputPayload.mMouseDragDelta.Value);
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
            return InputFlag.None;
        }


        public class CanvasConfig
        {
            public float scaling;
            public Vector2 nodePadding;

            public CanvasConfig()
            {
                this.scaling = 1f;
                this.nodePadding = Vector2.One;
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
}
