﻿using Dalamud.Logging;
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
        public Tuple<bool, bool> ProcessInputOnNode(Node pNode, Vector2 pNodeOSP, UtilsGUI.InputPayload pInputPayload, bool pReadClicks)
        {
            bool pIsNodeClicked = false;
            // Process node delete
            if (pReadClicks && !pIsNodeClicked && pInputPayload.mIsMouseMid)
            {
                if (pNode.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                    this.RemoveNode(pNode.mId);
            }
            // Process node select
            if (pReadClicks && !pIsNodeClicked && pInputPayload.mIsMouseLmb)
            {
                if (pNode.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
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
                    pIsNodeClicked = true;
                }
            }
            // Process node holding and dragging, except for when multiselecting
            if (pInputPayload.mIsMouseLmbDown)          // if mouse is hold, and the holding's first pos is within a selected node
            {                                           // then mark state as being dragged
                                                        // as long as the mouse is hold, even if mouse then moving out of node zone
                if (!this._isNodeBeingDragged
                    && !pIsNodeClicked
                    && !pInputPayload.mIsKeyCtrl
                    && pNode.CheckPosWithin(pNodeOSP, this.mConfig.scaling, pInputPayload.mMousePos))
                {
                    this._isNodeBeingDragged = true;
                    this._snappingNode = pNode;
                    // single-selecting new node
                    if (!pInputPayload.mIsKeyCtrl && !this._selectedNode.Contains(pNode.mId))
                    {
                        pIsNodeClicked = true;
                        this._selectedNode.Clear();
                        this._selectedNode.Add(pNode.mId);
                    }
                }
            }
            else
            {
                this._isNodeBeingDragged = false;
                this._snappingNode = null;
            }
            return new Tuple<bool, bool>(pIsNodeClicked, pReadClicks);
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
            bool tIsAnyNodeClicked = false;
            bool tIsReadingClicksOnNode = true;
            Vector2? tSnapDelta = null;
            // Get this canvas' origin' screenPos
            Vector2 tCanvasOSP = pBaseOriginScreenPos + this.mMap.GetBaseOffset();

            if (!pInteractable)     // clean up stuff in case viewer is involuntarily lose focus, to avoid potential accidents.
            {
                this._lastSnapDelta = null;
                this._snappingNode = null;
                this._isNodeBeingDragged = false;
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
                    if (t.Item1) tIsAnyNodeClicked = t.Item1;
                    tIsReadingClicksOnNode = t.Item2;
                }

                // Draw using NodeOSP
                tNode.Draw(
                    tSnapDelta != null && this._selectedNode.Contains(id)
                        ? tNodeOSP.Value + tSnapDelta.Value
                        : tNodeOSP.Value, 
                    this.mConfig.scaling);
            }
            if (pInteractable 
                && pInputPayload.mIsMouseLmb 
                && (!tIsAnyNodeClicked && (pInputPayload.mLmbDragDelta == null))
                && !pInputPayload.mIsALmbDragRelease) this._selectedNode.Clear();

            if (pInteractable)
            {
                // Drag selected node
                if (this._isNodeBeingDragged && pInputPayload.mLmbDragDelta.HasValue)
                {
                    foreach (var id in this._selectedNode)
                    {
                        if (pInputPayload.mLmbDragDelta.Value != Vector2.Zero)
                            PluginLog.LogDebug($"> 1: d={pInputPayload.mLmbDragDelta.Value} s={this.mConfig.scaling} pos={this.mMap.GetNodeScreenPos(id, tCanvasOSP, this.mConfig.scaling)}");
                        this.mMap.MoveNodeRelaPos(
                            id,
                            pInputPayload.mLmbDragDelta.Value,
                            this.mConfig.scaling);
                        if (pInputPayload.mLmbDragDelta.Value != Vector2.Zero)
                            PluginLog.LogDebug($"> 2: d={pInputPayload.mLmbDragDelta.Value} s={this.mConfig.scaling} pos={this.mMap.GetNodeScreenPos(id, tCanvasOSP, this.mConfig.scaling)}");
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
                if (!this._isNodeBeingDragged) this.ProcessInputOnCanvas(pInputPayload);
            }
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