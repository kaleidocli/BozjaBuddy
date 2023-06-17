﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Numerics;
using BozjaBuddy.Utils;
using Dalamud.Logging;
using ImGuiNET;
using static BozjaBuddy.Data.Location;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// ref material: https://git.anna.lgbt/ascclemens/QuestMap/src/branch/main/QuestMap/PluginUi.cs
    /// </summary>
    public class NodeGraphViewer
    {
        private const float kUnitGridSmall_Default = 10;
        private const float kUnitGridLarge_Default = 50;
        public const float kGridSnapProximity = 3.5f;

        public Vector2? mSize = null;
        private NodeCanvas mNodeCanvas;
        private float mUnitGridSmall = NodeGraphViewer.kUnitGridSmall_Default;
        private float mUnitGridLarge = NodeGraphViewer.kUnitGridLarge_Default;

        private bool _isMouseHoldingViewer = false;

        public NodeGraphViewer()
        {
            this.mNodeCanvas = new();
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 1");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 22");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 333");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 4");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 5555555555");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 6");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 777777777777777");
            this.mNodeCanvas.AddNodeToAvailableCorner<NodeLink>("Testing 888");
        }

        public void Draw()
        {
            Draw(ImGui.GetCursorScreenPos());
        }
        public void Draw(Vector2 pPos, Vector2? pSize = null)
        {
            Area tGraphArea = new(pPos, pSize ?? ImGui.GetContentRegionAvail());
            this.DrawGraph(tGraphArea);
        }

        private void DrawGraph(Area pGraphArea)
        {
            var pDrawList = ImGui.GetWindowDrawList();
            ImGui.BeginChild(
                "nodegraphviewer",
                pGraphArea.size, 
                border: true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove);
            pDrawList.PushClipRect(pGraphArea.start, pGraphArea.end, true);

            var tSnapData = DrawGraphBg(pGraphArea);
            DrawGraphNodes(pGraphArea, tSnapData);

            ImGui.EndChild();
            pDrawList.PopClipRect();
        }
        private void DrawGraphNodes(Area pGraphArea, GridSnapData pSnapData)
        {
            ImGui.SetCursorScreenPos(pGraphArea.start);

            // check if mouse within viewer, and if mouse is holding on viewer.
            UtilsGUI.InputPayload tInputPayload = new();
            tInputPayload.CaptureInput();
            bool tIsWithinViewer = pGraphArea.CheckPosIsWithin(tInputPayload.mMousePos);
            this._isMouseHoldingViewer = tInputPayload.mIsMouseLmbDown && (tIsWithinViewer || this._isMouseHoldingViewer);
            //PluginLog.LogDebug($"> inView={tIsWithinViewer} holdView={this._isMouseHoldingViewer} lmbDown={tInputPayload.mIsMouseLmbDown}");
            if (tIsWithinViewer) { tInputPayload.CaptureMouseWheel(); }
            if (this._isMouseHoldingViewer) { tInputPayload.CaptureMouseDragDelta(); }

            this.mNodeCanvas.Draw(
                pGraphArea.start,
                tInputPayload,
                pSnapData,
                pInteractable: ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows) && (this._isMouseHoldingViewer || tIsWithinViewer)
                );
        }
        private GridSnapData DrawGraphBg(Area pArea)
        {
            GridSnapData tGridSnap = new();
            ImGui.SetCursorScreenPos(pArea.start);
            var pDrawList = ImGui.GetWindowDrawList();
            // backdrop
            pDrawList.AddRectFilled(pArea.start, pArea.end, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalBar_Grey));
            // grid
            uint tGridColor = ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalBar_Grey);
            for (var i = 0; i < pArea.size.X / mUnitGridSmall; i++)
            {
                pDrawList.AddLine(new Vector2(pArea.start.X + i * mUnitGridSmall, pArea.start.Y), new Vector2(pArea.start.X + i * mUnitGridSmall, pArea.end.Y), tGridColor, 1.0f);
            }

            for (var i = 0; i < pArea.size.Y / mUnitGridSmall; i++)
            {
                pDrawList.AddLine(new Vector2(pArea.start.X, pArea.start.Y + i * mUnitGridSmall), new Vector2(pArea.end.X, pArea.start.Y + i * mUnitGridSmall), tGridColor, 1.0f);
            }

            for (var i = 0; i < pArea.size.X / mUnitGridLarge; i++)
            {
                tGridSnap.X.Add(pArea.start.X + i * mUnitGridLarge);
                pDrawList.AddLine(new Vector2(pArea.start.X + i * mUnitGridLarge, pArea.start.Y), new Vector2(pArea.start.X + i * mUnitGridLarge, pArea.end.Y), tGridColor, 2.0f);
            }

            for (var i = 0; i < pArea.size.Y / mUnitGridLarge; i++)
            {
                tGridSnap.Y.Add(pArea.start.Y + i * mUnitGridLarge);
                pDrawList.AddLine(new Vector2(pArea.start.X, pArea.start.Y + i * mUnitGridLarge), new Vector2(pArea.end.X, pArea.start.Y + i * mUnitGridLarge), tGridColor, 2.0f);
            }
            return tGridSnap;
        }

        public class GridSnapData
        {
            public List<float> X = new();
            public List<float> Y = new();

            public void AddUsingPos(Vector2 pos)
            {
                this.X.Add(pos.X);
                this.Y.Add(pos.Y);
            }
            public Vector2 GetClosestSnapPos(Vector2 currPos, float proximity)
            {
                var x = Utils.GetClosestItem(currPos.X, this.X);
                if (Math.Abs(x - currPos.X) > proximity) x = currPos.X;
                var y = Utils.GetClosestItem(currPos.Y, this.Y);
                if (Math.Abs(y - currPos.Y) > proximity) y = currPos.Y;
                return new(x, y);
            }
        }
    }
}
