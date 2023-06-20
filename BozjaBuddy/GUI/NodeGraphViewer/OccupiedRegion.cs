using System.Numerics;
using System.Collections.Generic;
using Dalamud.Logging;
using System.Data;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using System.Transactions;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// <para>Represents regions occuppied by nodes on a canvas.</para>
    /// </summary>
    internal class OccupiedRegion
    {
        private readonly Set X = new();
        private readonly Set Y = new();
        private readonly Dictionary<string, Node> mCanvasNodes;
        private readonly NodeMap mCanvasNodeMap;

        private OccupiedRegion() { }
        public OccupiedRegion(Dictionary<string, Node> pNodes, NodeMap pMap)
        {
            this.mCanvasNodeMap = pMap;
            this.mCanvasNodes = pNodes;
            this.Update();
        }

        public void Update()
        {
            this.ResetRegion();
            foreach (var n in this.mCanvasNodes.Values)
            {
                var tStart = this.mCanvasNodeMap.GetNodeRelaPos(n.mId);
                if (tStart == null) continue;
                var tEnd = tStart.Value + n.mStyle.GetSize();
                this.AddRegion(tStart.Value, tEnd);
            }
        }
        public void AddRegion(Vector2 pStart, Vector2 pEnd)
        {
            switch (pStart.X)
            {
                case var x when x < pEnd.X:
                    this.X.AddRange(x, pEnd.X); break;
                case var x when x > pEnd.X:
                    this.X.AddRange(pEnd.X, x); break;
            }
            switch (pStart.Y)
            {
                case var y when y < pEnd.Y:
                    this.Y.AddRange(y, pEnd.Y); break;
                case var y when y > pEnd.Y:
                    this.Y.AddRange(pEnd.Y, y); break;
            }
        }
        public void RemoveRegion(Vector2 pStart, Vector2 pEnd)
        {
            switch (pStart.X)
            {
                case var x when x < pEnd.X:
                    this.X.RemoveRange(x, pEnd.X); break;
                case var x when x > pEnd.X:
                    this.X.RemoveRange(pEnd.X, x); break;
            }
            switch (pStart.Y)
            {
                case var y when y < pEnd.Y:
                    this.Y.RemoveRange(y, pEnd.Y); break;
                case var y when y > pEnd.Y:
                    this.Y.RemoveRange(pEnd.Y, y); break;
            }
        }
        public void ResetRegion()
        {
            this.X.ResetRange();
            this.Y.ResetRange();
        }
        // |---------|
        // |         |
        // |         |
        // |---------|     <---- anchors are these corners. From that anchor, search for avail in direciton Dir.
        /// <summary>
        /// CornerAnchor: general area to search (NW, NE, SE, SW)
        /// Dir: cardinal direction to search for avail space from the anchor
        /// </summary>
        public Vector2 GetAvailableRelaPos(Direction pCornerAnchor, Direction pDir = Direction.None, Vector2? pPadding = null)
        {
            var tAnchor = OccupiedRegion.ToIntercardinal(pCornerAnchor);
            var tDir = OccupiedRegion.ToCardinal(pDir);
            var tPadding = pPadding ?? Vector2.Zero;

            this.Update();

            switch (tAnchor)
            {
                case Direction.NW: 
                    return tDir switch
                    {
                        Direction.N => new Vector2(this.X.minBound, this.Y.minBound - tPadding.Y),
                        Direction.S => new Vector2(this.X.minBound, this.Y.minBoundLarger + tPadding.Y),
                        Direction.W => new Vector2(this.X.minBound - tPadding.X, this.Y.minBoundLarger),
                        Direction.E => new Vector2(this.X.minBoundLarger + tPadding.X, this.Y.minBoundLarger),
                        _ => new Vector2(this.X.minBoundLarger + tPadding.X, this.Y.minBoundLarger)
                    };
                case Direction.NE:
                    return tDir switch
                    {
                        Direction.N => new Vector2(this.X.maxBound, this.Y.minBound - tPadding.Y),
                        Direction.S => new Vector2(this.X.maxBound, this.Y.minBoundLarger + tPadding.Y),
                        Direction.W => new Vector2(this.X.maxBoundSmaller - tPadding.X, this.Y.minBoundLarger),
                        Direction.E => new Vector2(this.X.maxBound + tPadding.X, this.Y.minBoundLarger),
                        _ => new Vector2(this.X.maxBound + tPadding.X, this.Y.minBoundLarger)
                    };
                case Direction.SE:
                    return tDir switch
                    {
                        Direction.N => new Vector2(this.X.maxBound, this.Y.maxBoundSmaller - tPadding.Y),
                        Direction.S => new Vector2(this.X.maxBound, this.Y.maxBound + tPadding.Y),
                        Direction.W => new Vector2(this.X.maxBoundSmaller - tPadding.X, this.Y.maxBoundSmaller),
                        Direction.E => new Vector2(this.X.maxBound + tPadding.X, this.Y.maxBoundSmaller),
                        _ => new Vector2(this.X.maxBound + tPadding.X, this.Y.maxBoundSmaller)
                    };
                case Direction.SW:
                    return tDir switch
                    {
                        Direction.N => new Vector2(this.X.minBound, this.Y.maxBoundSmaller - tPadding.Y),
                        Direction.S => new Vector2(this.X.minBound, this.Y.maxBound + tPadding.Y),
                        Direction.W => new Vector2(this.X.minBound - tPadding.X, this.Y.maxBound),
                        Direction.E => new Vector2(this.X.minBoundLarger + tPadding.X, this.Y.maxBound),
                        _ => new Vector2(this.X.minBoundLarger + tPadding.X, this.Y.maxBound)
                    };
            }

            return new Vector2(this.X.maxBound + tPadding.X, this.Y.minBound);
        }
        public Vector2 GetAvailableRelaPos(Direction pCornerAnchor, Direction pDir = Direction.None, float pPadding = 0)
        {
            return this.GetAvailableRelaPos(pCornerAnchor, pDir, new Vector2(pPadding));
        }
        /// <summary>
        /// <para>Try to get the free-est relaPos within given relaArea.</para>
        /// <para>relaArea is an Area on the canvas that this OccupiedRegion belongs to.</para>
        /// </summary>
        public Vector2 GetAvailableRelaPos(Area pRelaArea)
        {
            this.Update();
            float? tBestX = this.X.FindTwoFurthestEndpoints(pRelaArea.start.X, pRelaArea.end.X)?.Item1;
            float? tBestY = this.Y.FindTwoFurthestEndpoints(pRelaArea.start.Y, pRelaArea.end.Y)?.Item1;
            if (!tBestX.HasValue) tBestX = pRelaArea.start.X;
            if (!tBestY.HasValue) tBestY = pRelaArea.start.Y;
            return new(tBestX.Value, tBestY.Value);
        }

        public static Vector2? GetRecommendedOriginRelaPos(Area pArea, Direction pDir = Direction.E, Vector2? pPadding = null)
        {
            var tDir = ToCardinal(pDir);
            var tPadding = pPadding ?? Vector2.Zero;
            return tDir switch
            {
                Direction.N => new Vector2(pArea.start.X, pArea.start.Y - tPadding.Y),
                Direction.S => new Vector2(pArea.start.X, pArea.end.Y + tPadding.Y),
                Direction.W => new Vector2(pArea.start.X - tPadding.X, pArea.start.Y),
                Direction.E => new Vector2(pArea.end.X + tPadding.X, pArea.start.Y),
                _ => new Vector2(pArea.end.X + tPadding.X, pArea.start.Y)
            };
        }
        public static Vector2? GetRecommendedOriginRelaPos(Area pArea, Direction pDir = Direction.E, float pPadding = 0)
        {
            return OccupiedRegion.GetRecommendedOriginRelaPos(pArea, pDir, new Vector2(pPadding));
        }

        public static Direction ToCardinal(Direction pDir, Direction pDefaultForNone = Direction.S)
        {
            return pDir switch
            {
                Direction.NE => Direction.E,
                Direction.SE => Direction.E,
                Direction.NW => Direction.W,
                Direction.SW => Direction.W,
                Direction.None => pDefaultForNone,
                _ => pDir
            };
        }
        public static Direction ToIntercardinal(Direction pDir, Direction pDefaultForNone = Direction.SW)
        {
            return pDir switch
            {
                Direction.N => Direction.NE,
                Direction.E => Direction.NE,
                Direction.W => Direction.NW,
                Direction.S => Direction.SW,
                Direction.None => pDefaultForNone,
                _ => pDir
            };
        }
    }
}
