using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using QuickGraph;
using ImGuiNET;
using BozjaBuddy.Utils;
using Dalamud.Logging;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    public class Edge
    {
        private const float kThickness = 3;

        private string mSourceNodeId;
        private string mTargetNodeId;
        private SEdge<int> mQgEdge;

        private bool mDrawUpright = true;

        public Edge(string pSourceNodeId, string pTargetNodeId, SEdge<int> pQgEdge)
        {
            this.mSourceNodeId = pSourceNodeId;
            this.mTargetNodeId = pTargetNodeId;
            this.mQgEdge = pQgEdge;
        }
        public string GetSourceNodeId() => this.mSourceNodeId;
        public string GetTargetNodeId() => this.mTargetNodeId;
        public SEdge<int> GetEdge() => this.mQgEdge;
        public bool EitherWith(string pNodeId) => this.StartsWith(pNodeId) || this.EndsWith(pNodeId);
        public bool StartsWith(string pSourceNodeId) => this.mSourceNodeId == pSourceNodeId;
        public bool EndsWith(string pTargetNodeId) => this.mTargetNodeId == pTargetNodeId;
        public bool BothWith(string pSourceNodeId, string pTargetNodeId)
            => this.StartsWith(pSourceNodeId) && this.EndsWith(pTargetNodeId);
        public NodeInteractionFlags Draw(ImDrawListPtr pDrawList, Vector2 pSourceOSP, Vector2 pTargetOSP)
        {
            var tOriginalAnchor = ImGui.GetCursorScreenPos();

            // Anchor stuff
            Vector2 tAnchorOSP;
            Vector2 tAnchorSize = new(12, 12);
            if (this.mDrawUpright)
            {
                tAnchorOSP = new(Utils.GetGreaterVal(pSourceOSP.X, pTargetOSP.X), Utils.GetSmallerVal(pSourceOSP.Y, pTargetOSP.Y));
                if ((pSourceOSP.X < pTargetOSP.X && pSourceOSP.Y > pTargetOSP.Y)
                    || (pSourceOSP.X > pTargetOSP.X && pSourceOSP.Y < pTargetOSP.Y))
                {
                    tAnchorOSP = new(Utils.GetSmallerVal(pSourceOSP.X, pTargetOSP.X), Utils.GetSmallerVal(pSourceOSP.Y, pTargetOSP.Y));
                }
                else
                {
                    tAnchorOSP = new(Utils.GetGreaterVal(pSourceOSP.X, pTargetOSP.X), Utils.GetSmallerVal(pSourceOSP.Y, pTargetOSP.Y));
                }
            }
            else
            {
                if ((pSourceOSP.X < pTargetOSP.X && pSourceOSP.Y > pTargetOSP.Y)
                    || (pSourceOSP.X > pTargetOSP.X && pSourceOSP.Y < pTargetOSP.Y))
                {
                    tAnchorOSP = new(Utils.GetGreaterVal(pSourceOSP.X, pTargetOSP.X), Utils.GetGreaterVal(pSourceOSP.Y, pTargetOSP.Y));
                }
                else
                {
                    tAnchorOSP = new(Utils.GetSmallerVal(pSourceOSP.X, pTargetOSP.X), Utils.GetGreaterVal(pSourceOSP.Y, pTargetOSP.Y));
                }
            }    
            // Anchor button
            pDrawList.AddCircle(tAnchorOSP, tAnchorSize.X, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg));
            pDrawList.AddCircleFilled(tAnchorOSP, tAnchorSize.X * 0.75f, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg));
            ImGui.SetCursorScreenPos(tAnchorOSP - tAnchorSize);
            if (ImGui.InvisibleButton($"ea{this.mSourceNodeId}{this.mTargetNodeId}", tAnchorSize * 2))
            {
                this.mDrawUpright = !this.mDrawUpright;
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("Left-click to switch path.\nRight-click to disable square path.");
            }
            ImGui.SetCursorScreenPos(tOriginalAnchor);

            // Line
            pDrawList.AddLine(pSourceOSP, tAnchorOSP, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg), Edge.kThickness);
            pDrawList.AddLine(tAnchorOSP, pTargetOSP, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg), Edge.kThickness);
            return NodeInteractionFlags.None;
        }
    }
}
