using System;
using System.Numerics;
using BozjaBuddy.Utils;
using ImGuiNET;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Contains node's content, and its styling.
    /// </summary>
    public abstract class Node : IDisposable
    {
        public static readonly Vector2 nodePadding = new(3.5f, 3.5f);

        public abstract string mType { get; }
        public string mId { get; protected set; } = string.Empty;

        protected NodeContent mContent = new();
        protected NodeStyle mStyle = new();

        /// <summary>
        /// Never init this or its child. Get an instance from NodeCanvas.AddNode()
        /// Factory is not an option due to AddNode being a generic method.
        /// Fix this smelly thing prob?
        /// </summary>
        public Node() { }
        public virtual void Init(string pNodeId)
        {
            this.mId = pNodeId;
        }
        public virtual void SetSize(Vector2 pSize) => this.mStyle.size = pSize;
        public virtual void SetWidth(float pWidth) => this.mStyle.size.X = pWidth;
        public virtual void SetHeight(float pHeight) => this.mStyle.size.Y = pHeight;
        public Vector2 GetSize() => this.mStyle.size;
        public float GetWidth() => this.mStyle.size.X;
        public float GetHeight() => this.mStyle.size.Y;
        public virtual void SetHeader(string pText, bool pAutoSizing = true)
        {
            this.mContent.header = pText;
            if (pAutoSizing) this.AdjustSizeToContent();
        }
        public virtual void SetDescription(string pText, bool pAutoSizing = true)
        {
            this.mContent.description = pText;
            if (pAutoSizing) this.AdjustSizeToContent();
        }
        public virtual string GetHeader() => this.mContent.header;
        public virtual string GetDescription() => this.mContent.description;
        public virtual void AdjustSizeToContent()
        {
            this.mStyle.size = new Vector2(20, 20);
        }
        public bool CheckPosWithin(Vector2 pNodeOSP, float pCanvasScaling, Vector2 pScreenPos)
        {
            var tNodeSize = this.mStyle.size * pCanvasScaling;
            Area tArea = new(pNodeOSP, tNodeSize);
            return tArea.CheckPosIsWithin(pScreenPos);
        }

        public InputFlag Draw(Vector2 pNodeOSP, float pCanvasScaling)
        {
            ImGui.SetCursorScreenPos(pNodeOSP);

            var tNodeSize = this.mStyle.size * pCanvasScaling;
            var tDrawList = ImGui.GetWindowDrawList();
            var tStyle = ImGui.GetStyle();
            var tEnd = pNodeOSP + tNodeSize;

            tDrawList.AddRectFilled(
                pNodeOSP,
                tEnd,
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.Button_Green));

            Utils.PushFontScale(pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Node.nodePadding * pCanvasScaling);
            //ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, tStyle.FramePadding * pCanvasScaling);
            //ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, tStyle.ItemSpacing * pCanvasScaling);
            //ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, tStyle.IndentSpacing * pCanvasScaling);
            
            ImGui.BeginChild(
                this.mId,
                tNodeSize,
                border: true,
                ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar
                );
            var tRes = this.DrawInternal(pNodeOSP, pCanvasScaling);
            ImGui.EndChild();

            //ImGui.PopStyleVar();
            //ImGui.PopStyleVar();
            //ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            Utils.PopFontScale();

            return tRes;
        }
        protected abstract InputFlag DrawInternal(Vector2 pNodeOSP, float pCanvasScaling);

        public abstract void Dispose();



        public struct NodeContent
        {
            public string header = "";
            public string description = "";

            public NodeContent() { }
        }
        public struct NodeStyle
        {
            public Vector2 size = Vector2.Zero;

            public NodeStyle() { }
        }
    }
}
