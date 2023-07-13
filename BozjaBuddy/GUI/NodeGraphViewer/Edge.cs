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
using Dalamud.Interface.Components;
using Newtonsoft.Json;
using BozjaBuddy.GUI.NodeGraphViewer.utils;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonRelicNoteBook;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    public class Edge
    {
        private const float kThickness = 2f;
        private const float kArrowPosOffsetMax = 30;

        [JsonProperty]
        private string mSourceNodeId;
        [JsonProperty]
        private string mTargetNodeId;
        [JsonProperty]
        private SEdge<int> mQgEdge;

        [JsonProperty]
        private bool mDrawUpright = true;
        [JsonProperty]
        private bool mSquarePathingEnabled = false;
        [JsonProperty]
        private AnchorButtonState? _anchorFirstState = null;
        [JsonProperty]
        private Vector2? _anchorFirstPos = null;

        public Edge(string pSourceNodeId, string pTargetNodeId, SEdge<int> pQgEdge, bool pSquarePathing = false, bool pUpright = true)
        {
            this.mSourceNodeId = pSourceNodeId;
            this.mTargetNodeId = pTargetNodeId;
            this.mQgEdge = pQgEdge;
            this.mSquarePathingEnabled = pSquarePathing;
            this.mDrawUpright = pUpright;
        }
        public string GetSourceNodeId() => this.mSourceNodeId;
        public string GetTargetNodeId() => this.mTargetNodeId;
        public bool IsSquarePathing() => this.mSquarePathingEnabled;
        public bool IsDrawingUpRight() => this.mDrawUpright;
        public SEdge<int> GetEdge() => this.mQgEdge;
        public bool EitherWith(string pNodeId) => this.StartsWith(pNodeId) || this.EndsWith(pNodeId);
        public bool StartsWith(string pSourceNodeId) => this.mSourceNodeId == pSourceNodeId;
        public bool EndsWith(string pTargetNodeId) => this.mTargetNodeId == pTargetNodeId;
        public bool BothWith(string pSourceNodeId, string pTargetNodeId)
            => this.StartsWith(pSourceNodeId) && this.EndsWith(pTargetNodeId);
        public NodeInteractionFlags Draw(ImDrawListPtr pDrawList, Vector2 pSourceOSP, Vector2 pTargetOSP, bool pIsHighlighted = false, bool pIsTargetPacked = false)
        {
            var tOriginalAnchor = ImGui.GetCursorScreenPos();
            NodeInteractionFlags tRes = NodeInteractionFlags.None;

            // Anchor stuff
            Vector2 tAnchorOSP;
            Vector2 tAnchorSize = new(7.5f, 7.5f);
            float tTrasnsparency = 0.2f;
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
                                : (tAnchorSize.X * (pTargetOSP.X > tAnchorOSP.X ? 1 : -1))) * 3,
                        tAnchorOSP.Y + (
                                Math.Abs(tAnchorOSP.X - pSourceOSP.X) > Math.Abs(tAnchorOSP.X - pTargetOSP.X)
                                ? (tAnchorSize.Y * (pTargetOSP.Y > tAnchorOSP.Y ? 1 : -1))
                                : (tAnchorSize.Y * (pSourceOSP.Y > tAnchorOSP.Y ? 1 : -1))) * 3
                        );
            }
            else
            {
                tAnchorOSP = pSourceOSP + (pTargetOSP - pSourceOSP) * 0.5f;
            }
            // Anchor button (behaviour)
            bool tIsHovered = false;
            ImGui.SetCursorScreenPos(tAnchorOSP - tAnchorSize);
            ImGui.InvisibleButton($"ea{this.mSourceNodeId}{this.mTargetNodeId}", tAnchorSize * 3f, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
            if (ImGui.IsItemActive())
            {
                tRes |= NodeInteractionFlags.Edge;
                // Popup
                if (ImGui.GetIO().MouseClicked[1] == true)
                {
                    ImGui.OpenPopup($"##epu{this.mSourceNodeId}{this.mTargetNodeId}");
                }
                // Delete
                else if (ImGui.GetIO().MouseClicked[2] == true)
                {
                    tRes |= NodeInteractionFlags.RequestEdgeRemoval;
                }
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
                tIsHovered = UtilsGUI.SetTooltipForLastItem($"[Left-click + drag] diagonally to switch between 3 paths: Upper or Lower perpendicular, or Diagonal.\n[Right-click] or [Middle-click] to delete.");
            }
            // Anchor button PU
            tRes |= this.DrawPU();

            // Anchor button (drawing)
            var tColor = pIsTargetPacked ? UtilsGUI.Colors.NodePack : UtilsGUI.Colors.NodeFg;
            ImGui.SetCursorScreenPos(tOriginalAnchor);
            if (tClipRectEnd.HasValue) pDrawList.PushClipRect(
                                new Vector2(Utils.GetSmallerVal(tAnchorOSP.X, tClipRectEnd.Value.X), Utils.GetSmallerVal(tAnchorOSP.Y, tClipRectEnd.Value.Y)),
                                new Vector2(Utils.GetGreaterVal(tAnchorOSP.X, tClipRectEnd.Value.X), Utils.GetGreaterVal(tAnchorOSP.Y, tClipRectEnd.Value.Y)),
                                true
                                );
            pDrawList.AddCircle(tAnchorOSP, tAnchorSize.X * (tIsHovered ? (tClipRectEnd.HasValue ? 1.9f : 0.95f) : (tClipRectEnd.HasValue ? 1.35f : 0.65f)), ImGui.ColorConvertFloat4ToU32(tColor));
            if (tClipRectEnd.HasValue) pDrawList.PopClipRect();
            pDrawList.AddCircleFilled(tAnchorOSP, tAnchorSize.X * 0.4f, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(tColor, (tIsHovered || tClipRectEnd.HasValue) ? tTrasnsparency * 1.25f : tTrasnsparency)));

            // Line
            pDrawList.AddLine(pSourceOSP, tAnchorOSP, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(tColor, pIsHighlighted ? 1 : tTrasnsparency)), pIsHighlighted ? Edge.kThickness * 1.4f : Edge.kThickness);
            pDrawList.AddLine(tAnchorOSP, pTargetOSP, ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(tColor, pIsHighlighted ? 1 : tTrasnsparency)), pIsHighlighted ? Edge.kThickness * 1.4f : Edge.kThickness);
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
            Utils.DrawArrow(pDrawList, tArrowAnchorStart2, tArrowAnchorStart2 + tArrowHeight * tUnitSource, UtilsGUI.AdjustTransparency(tColor, pIsHighlighted ? 1 : tTrasnsparency));
            Utils.DrawArrow(pDrawList, tArrowAnchorStart, tArrowAnchorStart + tArrowHeight * tUnitAnchor, UtilsGUI.AdjustTransparency(tColor, pIsHighlighted ? 1 : tTrasnsparency));
            //Utils.DrawArrow(pDrawList, tArrowTargetStart, tArrowTargetStart + tArrowHeight * tUnitAnchor);

            return tRes;
        }
        public NodeInteractionFlags DrawPU()
        {
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            if (ImGui.BeginPopup($"##epu{this.mSourceNodeId}{this.mTargetNodeId}"))
            {
                // Delete button
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash))
                {
                    tRes |= NodeInteractionFlags.RequestEdgeRemoval;
                }
                ImGui.EndPopup();
            }
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
