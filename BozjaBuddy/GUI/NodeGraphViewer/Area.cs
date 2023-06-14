using Dalamud.Logging;
using System.Numerics;

namespace BozjaBuddy.GUI.NodeGraphViewer
{

    /// <summary> Represent an area </summary>
    public class Area
    {
        public Vector2 start;
        public Vector2 end;
        public Vector2 size;

        public Area(Vector2 pos, Vector2 size)
        {
            this.size = size;
            start = pos;
            end = pos + size;
        }

        public bool CheckPosIsWithin(Vector2 pos)
        {
            bool xRev = end.X < start.X;
            bool yRev = end.Y < start.Y;
            return (xRev ? (pos.X < start.X) : (pos.X > start.X))
                && (xRev ? (pos.X > end.X) : (pos.X < end.X))
                && (yRev ? (pos.Y < start.Y) : (pos.Y > start.Y))
                && (yRev ? (pos.Y > end.Y) : (pos.Y < end.Y));
        }
    }
}
