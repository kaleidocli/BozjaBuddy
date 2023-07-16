using BozjaBuddy.Data;
using System.Numerics;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// <para>Contains info about node positions (relative/screen)</para>
    /// <para>XY plane, origin O. OO is the origin of the canvas one level upper.</para>
    /// <para>If this is a map of top-level canvas, then OO is Viewer's origin, 
    /// and the scaling anchor C will be in the mid of the viewer.</para>
    /// <para>C is a point with distance ofsBaseScalingAnchor away from OO.</para>
    /// </summary>
    public class NodeMap
    {
        // 2d offset of O to OO.
        [JsonProperty]
        private Vector2 ofsBase = Vector2.Zero;
        // Key is a 
        [JsonProperty]
        private Dictionary<string, Vector2> _nodeMap = new();
        [JsonProperty]
        private HashSet<string> _nodeKeys = new();
        [JsonProperty]
        private bool _needInitOfs = true;

        public NodeMap() { }
        /// <summary>
        /// <para>Contains info about node positions (relative/screen)</para>
        /// <para>XY plane, origin O. OO is the origin of the canvas one level upper.</para>
        /// <para>If this is a map of top-level canvas, then OO is Viewer's origin, 
        /// and the scaling anchor C will be in the mid of the viewer.</para>
        /// <para>C is a point with distance ofsBaseScalingAnchor away from OO.</para>
        /// </summary>
        public NodeMap(Vector2 ofsBase)
        {
            this.ofsBase = ofsBase;
        }

        // ========================= CANVAS =========================
        public Vector2 GetBaseOffset() => this.ofsBase;
        public void AddBaseOffset(Vector2 offset) => ofsBase += offset;
        public void ResetBaseOffset() => ofsBase = Vector2.Zero;
        public bool FocusOnNode(string nodeId, Vector2? extraOfs = null)
        {
            if (!this._nodeMap.TryGetValue(nodeId, out var nodeOfsFromLocalBase)) return false;
            this.ResetBaseOffset();
            this.AddBaseOffset(-nodeOfsFromLocalBase + (extraOfs ?? Vector2.Zero));
            return true;
        }

        // ========================= NODE =========================
        public Vector2? GetNodeScreenPos(string nodeId, Vector2 canvasOrigninScreenPos, float canvasScaling)
        {
            var relaPos = this.GetNodeRelaPos(nodeId);
            if (relaPos == null) return null;
            return canvasOrigninScreenPos + relaPos.Value * canvasScaling;
        }
        public Vector2? GetNodeRelaPos(string nodeId)
        {
            if (_nodeMap.TryGetValue(nodeId, out Vector2 pos))
                return pos;
            return null;
        }


        /// <summary>
        /// Set a node's pos.
        /// </summary>
        public void SetNodeRelaPos(string nodeId, Vector2 relaPos)
        {
            if (!_nodeKeys.Contains(nodeId)) return;
            _nodeMap[nodeId] = relaPos;
        }
        /// <summary>For canvas related info, pick the canvas that this node belongs to.</summary>
        public void SetNodeRelaPos(string nodeId, Vector2 screenPos, Vector2 canvasOrigninScreenPos, float canvasScaling)
            => this.SetNodeRelaPos(nodeId, (screenPos - canvasOrigninScreenPos) / canvasScaling);
        /// <summary>
        /// <para>Set a node's relative to an anchor node, by a delta distance from the anchor.</para>
        /// </summary>
        public void SetNodeRelaPos(string nodeId, string anchorNodeId, Vector2 relaDelta)
        {
            if (!_nodeKeys.Contains(nodeId) || !_nodeKeys.Contains(anchorNodeId)) return;
            var anotherPos = this.GetNodeRelaPos(anchorNodeId);
            _nodeMap[nodeId] = anotherPos == null ? _nodeMap[nodeId] : (anotherPos.Value + relaDelta);
        }
        /// <summary>For canvas related info, pick the canvas that this node belongs to.</summary>
        public void SetNodeRelaPos(string nodeId, string anchorNodeId, Vector2 screenDelta, float canvasScaling)
            => this.SetNodeRelaPos(nodeId, anchorNodeId, screenDelta / canvasScaling);


        /// <summary>
        /// <para>Move a node by a delta distance from its current pos.</para>
        /// </summary>
        private void MoveNodeRelaPos(string nodeId, Vector2 relaDelta) => this.SetNodeRelaPos(nodeId, nodeId, relaDelta);
        /// <summary>For canvas related info, pick the canvas that this node belongs to.</summary>
        public void MoveNodeRelaPos(string nodeId, Vector2 screenDelta, float canvasScaling) => this.MoveNodeRelaPos(nodeId, screenDelta / canvasScaling);


        public void AddNode(string nodeId, Vector2 relaPos)
        {
            if (_nodeKeys.Contains(nodeId)) return;
            _nodeKeys.Add(nodeId);
            this.SetNodeRelaPos(nodeId, relaPos);
        }
        /// <summary>For canvas related info, pick the canvas that this node belongs to.</summary>
        public void AddNode(string nodeId, Vector2 screenPos, Vector2 canvasOrigninScreenPos, float canvasScaling)
            => this.AddNode(nodeId, (screenPos - canvasOrigninScreenPos) * canvasScaling);
        public bool RemoveNode(string nodeId)
        {
            return _nodeMap.Remove(nodeId) & _nodeKeys.Remove(nodeId);
        }
        public bool CheckNodeExist(string nodeId) => _nodeKeys.Contains(nodeId);

        public bool CheckNeedInitOfs() => this._needInitOfs;
        public void MarkUnneedInitOfs() => this._needInitOfs = false;
    }
}
