using System;
using System.Numerics;
using BozjaBuddy.Utils;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Contains node's content, and its styling.
    /// </summary>
    public abstract class Node : IDisposable
    {
        public static readonly Vector2 nodePadding = new(3.5f, 3.5f);
        public static readonly Vector2 minHandleSize = new(50, 20);

        public abstract string mType { get; }
        public string mId { get; protected set; } = string.Empty;

        protected NodeContent mContent = new();
        protected NodeStyle mStyle = new(Vector2.Zero, Vector2.Zero);
        protected virtual Vector2 mRecommendedInitSize { get; } = new(100, 200);
        public bool _isBusy = false;

        /// <summary>
        /// Never init this or its child. Get an instance from NodeCanvas.AddNode()
        /// Factory is not an option due to AddNode being a generic method.
        /// Fix this smelly thing prob?
        /// </summary>
        public Node() { }
        public virtual void Init(string pNodeId, string pHeader)
        {
            this.mId = pNodeId;
            this.SetHeader(pHeader);
            this.SetSize(this.mRecommendedInitSize);
        }
        public virtual void SetSize(Vector2 pSize, float pCanvasScaling = 1) => this.mStyle.SetSize(pSize / pCanvasScaling);
        public Vector2 GetSize() => this.mStyle.GetSize();
        public Vector2 GetHandleSize() => this.mStyle.GetHandleSize();
        public virtual void SetHeader(string pText, bool pAutoSizing = true)
        {
            this.mContent.header = pText;
            this.mStyle.SetMinSize(ImGui.CalcTextSize(this.mContent.header) + Node.nodePadding * 2);
            if (pAutoSizing) this.AdjustSizeToContent();
        }
        public virtual void SetDescription(string pText, bool pAutoSizing = true)
        {
            this.mContent.detail = pText;
            if (pAutoSizing) this.AdjustSizeToContent();
        }
        public virtual string GetHeader() => this.mContent.header;
        public virtual string GetDescription() => this.mContent.detail;
        public virtual void AdjustSizeToContent()
        {
            this.mStyle.SetSize(ImGui.CalcTextSize(this.mContent.header) + Node.nodePadding * 2);
        }
        public bool CheckPosWithin(Vector2 pNodeOSP, float pCanvasScaling, Vector2 pScreenPos)
        {
            var tNodeSize = this.mStyle.GetSize(pCanvasScaling);
            Area tArea = new(pNodeOSP, tNodeSize);
            return tArea.CheckPosIsWithin(pScreenPos);
        }
        public bool CheckPosWithinHandle(Vector2 pNodeOSP, float pCanvasScaling, Vector2 pScreenPos)
        {
            var tNodeSize = this.mStyle.GetHandleSize() * pCanvasScaling;
            Area tArea = new(pNodeOSP, tNodeSize);
            return tArea.CheckPosIsWithin(pScreenPos);
        }

        public InputFlag Draw(Vector2 pNodeOSP, float pCanvasScaling, bool pIsActive, UtilsGUI.InputPayload pInputPayload)
        {
            ImGui.SetCursorScreenPos(pNodeOSP);

            var tNodeSize = this.mStyle.GetSize(pCanvasScaling);
            var tDrawList = ImGui.GetWindowDrawList();
            var tStyle = ImGui.GetStyle();
            var tEnd = pNodeOSP + tNodeSize;

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

            Utils.PushFontScale(pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Node.nodePadding * pCanvasScaling);

            // resize grip
            Vector2 tGripSize = new(7, 7);
            tGripSize *= pCanvasScaling * 0.8f;     // making this scale less
            ImGui.SetCursorScreenPos(tEnd - tGripSize / 2);
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(this.mStyle.colorFg));
            ImGui.Button($"##{this.mId}", tGripSize);
            if (ImGui.IsItemHovered()) { ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE); }
            //else { ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow); }
            if (ImGui.IsItemActive()) 
            { 
                this._isBusy = true;
                this.SetSize(pInputPayload.mMousePos - pNodeOSP, pCanvasScaling);
            }
            if (this._isBusy && !pInputPayload.mIsMouseLmbDown) { this._isBusy = false; }
            ImGui.PopStyleColor();
            ImGui.SetCursorScreenPos(pNodeOSP);

            // node content (handle, body)
            ImGui.BeginChild(
                this.mId,
                tNodeSize,
                border: false,
                ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                );
            var tRes = this.DrawHandle(pNodeOSP, pCanvasScaling, tDrawList, pIsActive);
            ImGui.EndChild();

            ImGui.PopStyleVar();
            Utils.PopFontScale();

            return tRes;
        }
        protected virtual InputFlag DrawHandle(Vector2 pNodeOSP, float pCanvasScaling, ImDrawListPtr pDrawList, bool pIsActive)
        {
            var tHandleSize = this.GetHandleSize() * pCanvasScaling;
            pDrawList.AddRectFilled(
                pNodeOSP, 
                pNodeOSP + tHandleSize, 
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, pIsActive ? 0.55f : 0.25f)));
            ImGui.TextColored(UtilsGUI.Colors.NodeText, this.GetHeader());

            return InputFlag.None;
        }
        protected abstract InputFlag DrawBody(Vector2 pNodeOSP, float pCanvasScaling);

        public abstract void Dispose();



        public struct NodeContent
        {
            public string header = "";
            public string detail = "";

            public NodeContent() { }
        }
        public class NodeStyle
        {
            private Vector2 size;
            private Vector2 sizeHandle = Vector2.Zero;
            private Vector2 sizeBody = Vector2.Zero;
            private Vector2 minSize;

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
            public Vector2 GetSize() => this.size;
            public Vector2 GetSize(float scaling) => this.GetSize() * scaling;
            public Vector2 GetHandleSize() => this.sizeHandle;
            public Vector2 GetHandleSize(float scaling) => this.GetHandleSize() * scaling;
            public void SetMinSize(Vector2 minBound)
            {
                this.minSize.X = minBound.X < Node.minHandleSize.X ? Node.minHandleSize.X : minBound.X;
                this.minSize.Y = minBound.Y < Node.minHandleSize.Y ? Node.minHandleSize.Y : minBound.Y;
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
        }
    }
}
