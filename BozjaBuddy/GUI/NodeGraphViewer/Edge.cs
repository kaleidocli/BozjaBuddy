using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        private const float kArrowPosOffsetMax = 30;

        private string mSourceNodeId;
        private string mTargetNodeId;
        private SEdge<int> mQgEdge;

        private bool mDrawUpright = true;
        private bool mSquarePathingEnabled = false;
        private AnchorButtonState? _anchorFirstState = null;
        private Vector2? _anchorFirstPos = null;

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
        public NodeInteractionFlags Draw(ImDrawListPtr pDrawList, Vector2 pSourceOSP, Vector2 pTargetOSP, bool pIsHighlighted = false)
        {
            var tOriginalAnchor = ImGui.GetCursorScreenPos();
            NodeInteractionFlags tRes = NodeInteractionFlags.None;

            // Anchor stuff
            Vector2 tAnchorOSP;
            Vector2 tAnchorSize = new(7.5f, 7.5f);
            float tTrasnsparency = 0.4f;
            Vector2? tClipRectEnd = null;
            if (this.mSquarePathingEnabled)
            {
                if (this.mDrawUpright)
                {
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
                // Angle indicator
                tClipRectEnd = new(
                        tAnchorOSP.X + (
                                Math.Abs(tAnchorOSP.X - pSourceOSP.X) > Math.Abs(tAnchorOSP.X - pTargetOSP.X)
                                ? (tAnchorSize.X * (pSourceOSP.X > tAnchorOSP.X ? 1 : -1))
                                : (tAnchorSize.X * (pTargetOSP.X > tAnchorOSP.X ? 1 : -1))),
                        tAnchorOSP.Y + (
                                Math.Abs(tAnchorOSP.X - pSourceOSP.X) > Math.Abs(tAnchorOSP.X - pTargetOSP.X)
                                ? (tAnchorSize.Y * (pTargetOSP.Y > tAnchorOSP.Y ? 1 : -1))
                                : (tAnchorSize.Y * (pSourceOSP.Y > tAnchorOSP.Y ? 1 : -1)))
                        );
            }
            else
            {
                tAnchorOSP = pSourceOSP + (pTargetOSP - pSourceOSP) * 0.5f;
            }
            // Anchor button (behaviour)
            bool tIsHovered = false;
            ImGui.SetCursorScreenPos(tAnchorOSP - tAnchorSize);
            ImGui.InvisibleButton($"ea{this.mSourceNodeId}{this.mTargetNodeId}", tAnchorSize * 3f, ImGuiButtonFlags.MouseButtonLeft);
            if (ImGui.IsItemActive())
            {
                tRes |= NodeInteractionFlags.Edge;
                // Get original state
                if (!this._anchorFirstState.HasValue)
                {
                    if (!this.mSquarePathingEnabled) this._anchorFirstState = AnchorButtonState.Mid;
                    else
                    {
                        if (this.mDrawUpright) this._anchorFirstState = AnchorButtonState.Top;
                        else this._anchorFirstState = AnchorButtonState.Bottom;
                    }
                }
                // Get original pos
                if (!this._anchorFirstPos.HasValue) { this._anchorFirstPos = tAnchorOSP; }
                if (this._anchorFirstState.HasValue)
                {
                    var tMouseDelta = ImGui.GetMouseDragDelta();
                    var tMouseDeltaAbsAvg = (Math.Abs(tMouseDelta.X) + Math.Abs(tMouseDelta.Y)) / 2;
                    var tRange = tAnchorSize.Y * 4f;
                    // Draw tether to cursor
                    pDrawList.AddLine(this._anchorFirstPos.Value, this._anchorFirstPos.Value + tMouseDelta, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeText, 0.5f)));
                    // Get requesting state
                    AnchorButtonState tRequestingState = this._anchorFirstState.Value;
                    if (tMouseDelta.Y < 0)   // upward
                    {
                        // all -> top
                        if (tMouseDeltaAbsAvg > tRange * 2) tRequestingState = AnchorButtonState.Top;
                        else if (tMouseDeltaAbsAvg > tRange)
                        {
                            // bot -> mid
                            if (this._anchorFirstState == AnchorButtonState.Bottom) tRequestingState = AnchorButtonState.Mid;
                            // mid -> top && top -> top
                            else tRequestingState = AnchorButtonState.Top;
                        }
                    }
                    else                    // downward
                    {
                        // all -> bot
                        if (tMouseDeltaAbsAvg > tRange * 2) tRequestingState = AnchorButtonState.Bottom;
                        else if (tMouseDeltaAbsAvg > tRange)
                        {
                            // top -> mid
                            if (this._anchorFirstState == AnchorButtonState.Top) tRequestingState = AnchorButtonState.Mid;
                            // mid -> bot && bot -> bot
                            else tRequestingState = AnchorButtonState.Bottom;
                        }
                    }
                    // Process requested state
                    switch (tRequestingState)
                    {
                        case AnchorButtonState.Mid: this.mSquarePathingEnabled = false; break;
                        case AnchorButtonState.Top:
                            this.mSquarePathingEnabled = true;
                            this.mDrawUpright = true;
                            break;
                        case AnchorButtonState.Bottom:
                            this.mSquarePathingEnabled = true;
                            this.mDrawUpright = false;
                            break;
                    }
                }
            }
            else
            {
                this._anchorFirstState = null;
                this._anchorFirstPos = null;
                tIsHovered = UtilsGUI.SetTooltipForLastItem($"[Left-click + drag] to switch between 3 paths. (Upper squared-edge, Free, Bottom squared-edge)");
            }
            // Anchor button (drawing)
            ImGui.SetCursorScreenPos(tOriginalAnchor);
            pDrawList.AddCircle(tAnchorOSP, tAnchorSize.X * (tIsHovered ? 1.2f : 1), ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NodeFg));
            if (tClipRectEnd.HasValue) pDrawList.PushClipRect(
                                            new Vector2(Utils.GetSmallerVal(tAnchorOSP.X, tClipRectEnd.Value.X), Utils.GetSmallerVal(tAnchorOSP.Y, tClipRectEnd.Value.Y)),
                                            new Vector2(Utils.GetGreaterVal(tAnchorOSP.X, tClipRectEnd.Value.X), Utils.GetGreaterVal(tAnchorOSP.Y, tClipRectEnd.Value.Y))
                                            );
            pDrawList.AddCircleFilled(tAnchorOSP, tAnchorSize.X * 0.75f, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, (tIsHovered || tClipRectEnd.HasValue) ? 1 : tTrasnsparency)));
            if (tClipRectEnd.HasValue) pDrawList.PopClipRect();

            // Line
            pDrawList.AddLine(pSourceOSP, tAnchorOSP, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, pIsHighlighted ? 1 : tTrasnsparency)), pIsHighlighted ? Edge.kThickness * 1.4f : Edge.kThickness);
            pDrawList.AddLine(tAnchorOSP, pTargetOSP, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, pIsHighlighted ? 1 : tTrasnsparency)), pIsHighlighted ? Edge.kThickness * 1.4f : Edge.kThickness);
            // Edge's direction arrow (arrowhead)
            float tArrowOfsSource = Vector2.Distance(pSourceOSP, tAnchorOSP); if (tArrowOfsSource > Edge.kArrowPosOffsetMax) tArrowOfsSource = Edge.kArrowPosOffsetMax;
            float tArrowOfsAnchor = Vector2.Distance(tAnchorOSP, pTargetOSP); if (tArrowOfsAnchor > Edge.kArrowPosOffsetMax) tArrowOfsAnchor = Edge.kArrowPosOffsetMax;
            float tArrowHeight = pIsHighlighted ? 10 * 1.5f : 10;
            var tUnitSource = Vector2.Normalize(tAnchorOSP - pSourceOSP);
            var tUnitAnchor = Vector2.Normalize(pTargetOSP - tAnchorOSP);

            Vector2 tArrowSourceStart = pSourceOSP + (tArrowOfsSource * tUnitSource);                     // from source
            Vector2 tArrowAnchorStart = tAnchorOSP + (tArrowOfsAnchor * tUnitAnchor);                     // from anchor
            Vector2 tArrowAnchorStart2 = tAnchorOSP - ((tArrowOfsSource + tArrowHeight) * tUnitSource);   // from anchor (but before entering anchor)
            Vector2 tArrowTargetStart = pTargetOSP - ((tArrowOfsAnchor + tArrowHeight) * tUnitAnchor);    // from target

            //Utils.DrawArrow(pDrawList, tArrowSourceStart, tArrowSourceStart + tArrowHeight * tUnitSource);
            Utils.DrawArrow(pDrawList, tArrowAnchorStart2, tArrowAnchorStart2 + tArrowHeight * tUnitSource, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, pIsHighlighted ? 1 : tTrasnsparency));
            Utils.DrawArrow(pDrawList, tArrowAnchorStart, tArrowAnchorStart + tArrowHeight * tUnitAnchor, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.NodeFg, pIsHighlighted ? 1 : tTrasnsparency));
            //Utils.DrawArrow(pDrawList, tArrowTargetStart, tArrowTargetStart + tArrowHeight * tUnitAnchor);

            return tRes;
        }

        private enum AnchorButtonState
        {
            Top = 0,
            Mid = 1,
            Bottom = 2
        }
    }
}
