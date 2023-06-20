using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.Interop.Attributes;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using QuickGraph;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Contains node's content, and its styling.
    /// </summary>
    public abstract class Node : IDisposable
    {
        public static readonly Vector2 nodeInsidePadding = new(3.5f, 3.5f);
        public static readonly Vector2 minHandleSize = new(50, 20);
        public static readonly float handleButtonBoxItemWidth = 20;
        protected bool _needReinit = false;
        public bool _isMarkedDeleted = false;

        public abstract string mType { get; }
        public string mId { get; protected set; } = string.Empty;

        public Node() { }


        // ====================
        // GUI Related
        // ====================
        public NodeContent mContent = new();
        public NodeStyle mStyle = new(Vector2.Zero, Vector2.Zero);
        protected virtual Vector2 mRecommendedInitSize { get; } = new(100, 200);
        protected bool mIsMinimized = false;
        public bool _isBusy = false;
        protected Vector2? _lastUnminimizedSize = null;

        /// <summary>
        /// Never init this or its child. Get an instance from NodeCanvas.AddNode()
        /// Factory is not an option due to AddNode being a generic method.
        /// Fix this smelly thing prob?
        /// </summary>
        public virtual void Init(string pNodeId, string pHeader)
        {
            this.mId = pNodeId;

            // Basically SetHeader() and AdjustSizeToContent(),
            // but we need non-ImGui option for loading out of Draw()
            this.SetHeader(pHeader);
            if (Plugin._isImGuiSafe)
            {
                this.mStyle.SetHandleTextSize(ImGui.CalcTextSize(this.mContent.GetHeader()));
            }
            else
            {
                this.mStyle.SetHandleTextSize(new Vector2(this.mContent.GetHeader().Length * 6, 11));
                this._needReinit = true;
            }

            this.mStyle.SetSize(this.mRecommendedInitSize);
        }
        protected virtual void ReInit()
        {
            this.SetHeader(this.mContent.GetHeader());               // adjust minSize to new header
            this.mStyle.SetSize(this.mRecommendedInitSize);        // adjust size to the new minSize
        }

        public virtual void SetHeader(string pText, bool pAutoSizing = true)
        {
            this.mContent._setHeader(pText);
            this.mStyle.SetHandleTextSize(ImGui.CalcTextSize(this.mContent.GetHeader()));
            if (pAutoSizing) this.AdjustSizeToHeader();
        }
        public virtual void AdjustSizeToHeader()
        {
            this.mStyle.SetSize(ImGui.CalcTextSize(this.mContent.GetHeader()) + Node.nodeInsidePadding * 2);
        }

        public NodeInteractionFlags Draw(Vector2 pNodeOSP, float pCanvasScaling, bool pIsActive, UtilsGUI.InputPayload pInputPayload)
        {
            // Re-calculate ImGui-dependant members, if required.
            if (this._needReinit)
            {
                this.ReInit();
                this._needReinit = false;
            }
            // Minimize/Unminimize
            if (this.mIsMinimized && !this._lastUnminimizedSize.HasValue)
            {
                this._lastUnminimizedSize = this.mStyle.GetSize();
                this.mStyle.SetSize(new Vector2(this._lastUnminimizedSize.Value.X, 0));
            }
            else if (!this.mIsMinimized && this._lastUnminimizedSize.HasValue)
            {
                this.mStyle.SetSize(this._lastUnminimizedSize.Value);
                this._lastUnminimizedSize = null;
            }

            ImGui.SetCursorScreenPos(pNodeOSP);

            var tNodeSize = this.mStyle.GetSizeScaled(pCanvasScaling);
            var tDrawList = ImGui.GetWindowDrawList();
            var tStyle = ImGui.GetStyle();
            var tEnd = pNodeOSP + tNodeSize;
            NodeInteractionFlags tRes = NodeInteractionFlags.None;

            // outline
            tDrawList.AddRect(
                pNodeOSP,
                tEnd,
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(this.mStyle.colorFg, pIsActive ? 1f : 0.7f)),
                1,
                ImDrawFlags.None,
                (pIsActive ? 6.5f : 4f) * pCanvasScaling);

            // backdrop
            tDrawList.AddRectFilled(
                pNodeOSP,
                tEnd,
                ImGui.ColorConvertFloat4ToU32(this.mStyle.colorBg));

            // resize grip
            if (!this.mIsMinimized)
            {
                Vector2 tGripSize = new(10, 10);
                tGripSize *= pCanvasScaling * 0.8f;     // making this scale less
                ImGui.SetCursorScreenPos(tEnd - tGripSize * 0.7f);
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(this.mStyle.colorFg));
                ImGui.Button($"##{this.mId}", tGripSize);
                if (ImGui.IsItemHovered()) { ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE); }
                //else { ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow); }
                if (ImGui.IsItemActive())
                {
                    this._isBusy = true;
                    this.mStyle.SetSizeScaled(pInputPayload.mMousePos - pNodeOSP, pCanvasScaling);
                }
                if (this._isBusy && !pInputPayload.mIsMouseLmbDown) { this._isBusy = false; }
                ImGui.PopStyleColor();
                ImGui.SetCursorScreenPos(pNodeOSP);
            }

            // node content (handle, body)
            Utils.PushFontScale(pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Node.nodeInsidePadding * pCanvasScaling);

            ImGui.BeginChild(
                this.mId,
                tNodeSize,
                border: false,
                ImGuiWindowFlags.NoScrollbar
                );
            tRes |= this.DrawHandle(pNodeOSP, pCanvasScaling, tDrawList, pIsActive);
            tRes |= this.DrawBody(pNodeOSP, pCanvasScaling);
            ImGui.EndChild();

            ImGui.PopStyleVar();
            Utils.PopFontScale();

            return tRes;
        }
        protected virtual NodeInteractionFlags DrawHandle(Vector2 pNodeOSP, float pCanvasScaling, ImDrawListPtr pDrawList, bool pIsActive)
        {
            var tHandleSize = this.mStyle.GetHandleSizeScaled(pCanvasScaling);
            pDrawList.AddRectFilled(
                pNodeOSP, 
                pNodeOSP + tHandleSize, 
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, pIsActive ? 0.55f : 0.25f)));
            ImGui.SetCursorScreenPos(
                pNodeOSP + new Vector2(
                        this.mStyle.handleTextPadding.X, 
                        ((tHandleSize.Y - this.mStyle.GetHandleTextSize().Y * pCanvasScaling) / 2) + this.mStyle.handleTextPadding.Y * pCanvasScaling
                    )
                );
            ImGui.TextColored(UtilsGUI.Colors.NodeText, this.mContent.GetHeader());

            // ButtonBox
            ImGui.SameLine();
            Utils.AlignRight(Node.handleButtonBoxItemWidth * 3 * pCanvasScaling, pConsiderImguiPaddings: false);
            var tRes = this.DrawHandleButtonBox(pNodeOSP, pCanvasScaling, pDrawList);

            return tRes;
        }
        protected abstract NodeInteractionFlags DrawBody(Vector2 pNodeOSP, float pCanvasScaling);
        protected NodeInteractionFlags DrawHandleButtonBox(Vector2 pNodeOSP, float pCanvasScaling, ImDrawListPtr pDrawList)
        {
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.NodeText);

            Vector2 tBSize = new(Node.handleButtonBoxItemWidth, this.mStyle.GetHandleSize().Y * 0.8f);
            tBSize *= pCanvasScaling;
            if (ImGui.Selectable(this.mIsMinimized ? "  ^" : "  v", false, ImGuiSelectableFlags.DontClosePopups, tBSize))
            {
                this.mIsMinimized = !this.mIsMinimized;
            }
            if (ImGui.IsItemActive()) tRes |= NodeInteractionFlags.Internal;
            ImGui.SameLine(); 
            if (ImGui.Selectable(" …", false, ImGuiSelectableFlags.DontClosePopups, tBSize))
            {
                ImGui.OpenPopup($"##hepu{this.mId}");
            }
            tRes |= this.DrawHandeExtraOptionPU($"##hepu{this.mId}");
            if (ImGui.IsItemActive()) tRes |= NodeInteractionFlags.Internal;
            ImGui.SameLine(); 
            if (ImGui.Selectable(" ×", false, ImGuiSelectableFlags.DontClosePopups, tBSize))
            {
                this._isMarkedDeleted = true;
            }
            if (ImGui.IsItemActive()) tRes |= NodeInteractionFlags.Internal;

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            return tRes;
        }
        protected NodeInteractionFlags DrawHandeExtraOptionPU(string pPU_id)
        {
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            if (ImGui.BeginPopup(pPU_id))
            {
                ImGui.Text("Testing 1");
                ImGui.Text("Testing 22");
                ImGui.Text("Testing 333");
                ImGui.EndPopup();
                tRes |= NodeInteractionFlags.Internal | NodeInteractionFlags.LockSelection;
            }

            return tRes;
        }

        public abstract void Dispose();



        public struct NodeContent
        {
            private string header = "";
            private string detail = "";

            public NodeContent() { }

            public string GetHeader() => this.header;
            public void _setHeader(string header) => this.header = header;
            public string GetDetail() => this.detail;
            public void SetDetail(string detail) => this.detail = detail;

        }
        public class NodeStyle
        {
            private Vector2 size;
            private Vector2 sizeHandle = Vector2.Zero;
            private Vector2 sizeBody = Vector2.Zero;
            private Vector2 minSize;
            public Vector2 handleTextPadding = new(3, 0);
            private Vector2 handleTextSize = Vector2.Zero;

            public Vector4 colorUnique = UtilsGUI.Colors.GenObj_BlueAction;
            public Vector4 colorBg = UtilsGUI.Colors.NodeBg;
            public Vector4 colorFg = UtilsGUI.Colors.NodeFg;

            public NodeStyle(Vector2 size, Vector2 minSize)
            {
                this.SetMinSize(minSize);
                this.SetSize(size);
                this.UpdatePartialSizes();
            }

            public void SetSize(Vector2 size)
            {
                this.size.X = size.X < this.minSize.X ? this.minSize.X : size.X;
                this.size.Y = size.Y < this.minSize.Y ? this.minSize.Y : size.Y;
                this.UpdatePartialSizes();
            }
            public void SetSizeScaled(Vector2 size, float canvasScaling = 1) => this.SetSize(size / canvasScaling);
            public Vector2 GetSize() => this.size;
            public Vector2 GetSizeScaled(float canvasScaling) => this.GetSize() * canvasScaling;
            public Vector2 GetHandleSize() => this.sizeHandle;
            public Vector2 GetHandleSizeScaled(float scaling) => this.GetHandleSize() * scaling;
            public Vector2 GetHandleTextSize() => this.handleTextSize;
            public void SetHandleTextSize(Vector2 handleTextSize)
            {
                this.handleTextSize = handleTextSize;
                this.SetMinSize(this.GetHandleTextSize() + Node.nodeInsidePadding * 2 + new Vector2(Node.handleButtonBoxItemWidth * 3, 0));
            }
            private void SetMinSize(Vector2 handleSize)
            {
                this.minSize.X = handleSize.X < Node.minHandleSize.X ? Node.minHandleSize.X : handleSize.X;
                this.minSize.Y = handleSize.Y < Node.minHandleSize.Y ? Node.minHandleSize.Y : handleSize.Y;
            }
            private void UpdatePartialSizes()
            {
                this.UpdateHandleSize();
                this.UpdateSizeBody();
            }
            private void UpdateHandleSize()
            {
                this.sizeHandle.X = this.GetSize().X;
                this.sizeHandle.Y = this.minSize.Y;
            }
            private void UpdateSizeBody()
            {
                this.sizeBody.X = this.size.X;
                this.sizeBody.Y = this.size.Y - this.sizeHandle.Y;
            }

            public bool CheckPosWithin(Vector2 nodeOSP, float canvasScaling, Vector2 screenPos)
            {
                var tNodeSize = this.GetSizeScaled(canvasScaling);
                Area tArea = new(nodeOSP, tNodeSize);
                return tArea.CheckPosIsWithin(screenPos);
            }
            public bool CheckPosWithinHandle(Vector2 nodeOSP, float canvasScaling, Vector2 screenPos)
            {
                var tNodeSize = this.GetHandleSize() * canvasScaling;
                Area tArea = new(nodeOSP, tNodeSize);
                return tArea.CheckPosIsWithin(screenPos);
            }
            public bool CheckAreaIntersect(Vector2 nodeOSP, float canvasScaling, Area screenArea)
            {
                var tNodeSize = this.GetSizeScaled(canvasScaling);
                Area tArea = new(nodeOSP, tNodeSize);
                return tArea.CheckAreaIntersect(screenArea);
            }
        }
    }
}
