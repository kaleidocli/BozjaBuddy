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
using JsonSubTypes;
using Newtonsoft.Json;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using static Lumina.Data.Files.Pcb.PcbResourceFile;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;
using BozjaBuddy.GUI.NodeGraphViewer.NodeContent;
using BozjaBuddy.GUI.NodeGraphViewer.utils;
using Dalamud.Interface.Components;

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

        public HashSet<string> mPack = new();           // represents the nodes that this node packs
        public string? mPackerNodeId = null;        // represents the node which packs this node (the master packer, not just the parent node). Only ONE packer per node.
        public bool mIsPacked = false;
        public PackingStatus mPackingStatus = PackingStatus.None;
        public Vector2? _relaPosLastPackingCall = null;

        protected bool _isBeingEdited = false;
        protected string? _newHeader = null;
        protected Vector4? _newColorUnique = null;        

        public abstract string mType { get; }
        public string mId { get; protected set; } = string.Empty;
        public int mGraphId = -1;
        protected Queue<NodeCanvas.Seed> _seeds = new();

        public Node() { }


        // ====================
        // GUI Related
        // ====================
        public NodeContent.NodeContent mContent = new();
        public NodeStyle mStyle = new(Vector2.Zero, Vector2.Zero);
        protected virtual Vector2 mRecommendedInitSize { get; } = new(100, 200);
        [JsonProperty]
        protected bool mIsMinimized = false;
        public bool _isBusy = false;
        [JsonProperty]
        protected Vector2? _lastUnminimizedSize = null;

        /// <summary>
        /// <para>Never init this or its child. Get an instance from NodeCanvas.AddNode()</para>
        /// <para>param _style is for deserializing</para>
        /// Factory is not an option due to AddNode being a generic method.
        /// Fix this smelly thing prob?
        /// Used for json.
        /// </summary>
        public virtual void Init(string pNodeId, int pGraphId, NodeContent.NodeContent pContent, NodeStyle? _style = null)
        {
            this.mId = pNodeId;
            this.mGraphId = pGraphId;
            this.mContent = pContent;
            if (_style != null) this.mStyle = _style;

            // Basically SetHeader() and AdjustSizeToContent(),
            // but we need non-ImGui option for loading out of Draw()
            if (Plugin._isImGuiSafe)
            {
                this.SetHeader(this.mContent.GetHeader());
            }
            else
            {
                this.mContent._setHeader(this.mContent.GetHeader());
                this.mStyle.SetHandleTextSize(new Vector2(this.mContent.GetHeader().Length * 6, 11));
                this.mStyle.SetSize(new Vector2(this.mContent.GetHeader().Length * 6, 11) + Node.nodeInsidePadding * 2);
                this._needReinit = true;
            }

            this.mStyle.SetSize(this.mRecommendedInitSize);
            PluginLog.LogDebug($"> Node.Init(): type={this.GetType()} id={this.mId} contentType={this.mContent._contentType}");
        }
        protected virtual void ReInit()
        {
            this.SetHeader(this.mContent.GetHeader());               // adjust minSize to new header
            this.mStyle.SetSize(this.mRecommendedInitSize);        // adjust size to the new minSize
        }

        /// <summary>
        /// <para>pAdjustWidthOnly:         (requires: autoSizing) Adjust node's width only, keep node's height the same.</para>
        /// <para>pChooseGreaterWidth:      (requires: autoSizing, adjustWidthOnly) Only adjust if the new width is greater than the current one.</para>
        /// </summary>
        public virtual void SetHeader(string pText, bool pAutoSizing = true, bool pAdjustWidthOnly = false, bool pChooseGreaterWidth = false)
        {
            this.mContent._setHeader(pText);
            this.mStyle.SetHandleTextSize(ImGui.CalcTextSize(this.mContent.GetHeader()));
            if (pAutoSizing) this.AdjustSizeToHeader(pAdjustWidthOnly, pChooseGreaterWidth);
        }
        /// <summary>
        /// <para>pAdjustWidthOnly:         Adjust node's width only, keep node's height the same.</para>
        /// <para>pChooseGreaterWidth:      Only adjust if the new width is greater than the current one.</para>
        /// </summary>
        public virtual void AdjustSizeToHeader(bool pAdjustWidthOnly = false, bool pChooseGreaterWidth = false)
        {
            if (pAdjustWidthOnly)
            {
                float tW = (ImGui.CalcTextSize(this.mContent.GetHeader()) + Node.nodeInsidePadding * 2).X;
                this.mStyle.SetSize(
                    new(
                        pChooseGreaterWidth
                        ? tW > this.mStyle.GetSize().X
                            ? tW
                            : this.mStyle.GetSize().X
                        : tW,
                        this.mStyle.GetSize().Y
                        )
                    );
            }
            else
                this.mStyle.SetSize(ImGui.CalcTextSize(this.mContent.GetHeader()) + Node.nodeInsidePadding * 2);
        }
        public virtual NodeCanvas.Seed? GetSeed() => this._seeds.Count == 0 ? null : this._seeds.Dequeue();
        protected virtual void SetSeed(NodeCanvas.Seed pSeed) => this._seeds.Enqueue(pSeed);
        public void Minimize() => this.mIsMinimized = true;
        public void Unminimize() => this.mIsMinimized = false;

        public NodeInteractionFlags Draw(
            Vector2 pNodeOSP, 
            float pCanvasScaling, 
            bool pIsActive, 
            UtilsGUI.InputPayload pInputPayload, 
            ImDrawListPtr pDrawList, 
            bool pIsEstablishingConn = false,
            bool pIsDrawingHndCtxMnu = false)
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

            var tNodeSize = this.mStyle.GetSizeScaled(pCanvasScaling);
            Vector2 tOuterWindowSizeOfs = new(15 * pCanvasScaling);
            var tStyle = ImGui.GetStyle();
            var tEnd = pNodeOSP + tNodeSize;
            NodeInteractionFlags tRes = NodeInteractionFlags.None;

            // resize grip
            if (!this.mIsMinimized)
            {
                Vector2 tGripSize = new(10, 10);
                tGripSize *= pCanvasScaling * 0.8f;     // making this scale less
                ImGui.SetCursorScreenPos(tEnd - tGripSize * (pIsActive ? 0.425f : 0.57f));
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

            // Each node drawing have 2 child windows.
            // One to get this node's drawList so that it would take priority over master drawlist.
            // One to format the node content.
            ImGui.SetCursorScreenPos(pNodeOSP - tOuterWindowSizeOfs / 2);
            ImGui.BeginChild(
                $"##outer{this.mId}", tNodeSize + tOuterWindowSizeOfs, false,
                ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar);
            var tDrawList = ImGui.GetWindowDrawList();

            ImGui.SetCursorScreenPos(pNodeOSP);
            // outline
            tDrawList.AddRect(
                pNodeOSP,
                tEnd,
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(this.mStyle.colorFg, pIsActive ? 0.7f : 0.2f)),
                1,
                ImDrawFlags.None,
                (pIsActive ? 6.5f : 4f) * pCanvasScaling);

            //node content(handle, body)
            Utils.PushFontScale(pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Node.nodeInsidePadding * pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 1.5f);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertFloat4ToU32(this.mStyle.colorBg));
            ImGui.PushStyleColor(ImGuiCol.Border, UtilsGUI.AdjustTransparency(this.mStyle.colorFg, pIsActive ? 0.7f : 0.2f));

            ImGui.BeginChild(
                this.mId,
                tNodeSize,
                border: true,
                ImGuiWindowFlags.ChildWindow
                );
            // backdrop (leave this here so the backgrop can overwrite the child's bg)
            if (!this.mIsMinimized) tDrawList.AddRectFilled(pNodeOSP,tEnd,ImGui.ColorConvertFloat4ToU32(this.mStyle.colorBg));
            tRes |= this.DrawHandle(pNodeOSP, pCanvasScaling, tDrawList, pIsActive, out var tHndCtxMnu);
            ImGui.SetCursorScreenPos(new Vector2(pNodeOSP.X + 2 * pCanvasScaling, ImGui.GetCursorScreenPos().Y + 5 * pCanvasScaling));
            if (!this.mIsMinimized) tRes |= this.DrawBody(pNodeOSP, pCanvasScaling);
            ImGui.EndChild();

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            Utils.PopFontScale();

            ImGui.EndChild();

            // HndCtxMnu
            if (pIsDrawingHndCtxMnu || tHndCtxMnu) ImGui.OpenPopup(this.GetExtraOptionPUGuiId());
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.NodeText);
            tRes |= this.DrawHandeExtraOptionPU();
            ImGui.PopStyleColor();

            tRes |= this.DrawEdgePlugButton(tDrawList, pNodeOSP, pIsActive, pIsEstablishingConn: pIsEstablishingConn);

            return tRes;
        }
        protected virtual NodeInteractionFlags DrawHandle(Vector2 pNodeOSP, float pCanvasScaling, ImDrawListPtr pDrawList, bool pIsActive, out bool pHndCtxMnu)
        {
            var tHandleSize = this.mStyle.GetHandleSizeScaled(pCanvasScaling);
            pDrawList.AddRectFilled(
                pNodeOSP,
                pNodeOSP + tHandleSize,
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, pIsActive ? 0.45f : 0.15f)));
            //ImGui.SetCursorScreenPos(
            //    pNodeOSP + new Vector2(
            //            this.mStyle.handleTextPadding.X, 
            //            ((tHandleSize.Y - this.mStyle.GetHandleTextSize().Y * pCanvasScaling) / 2) + this.mStyle.handleTextPadding.Y * pCanvasScaling
            //        )
            //    );
            ImGui.TextColored(UtilsGUI.Colors.NodeText, this.mContent.GetHeader());

            // ButtonBox
            ImGui.SameLine();
            Utils.AlignRight(Node.handleButtonBoxItemWidth * 3 * pCanvasScaling, pConsiderImguiPaddings: false);
            
            var tRes = this.DrawHandleButtonBox(pNodeOSP, pCanvasScaling, pDrawList, out var tHndCtxMnu);
            pHndCtxMnu = tHndCtxMnu;

            return tRes;
        }
        protected abstract NodeInteractionFlags DrawBody(Vector2 pNodeOSP, float pCanvasScaling);
        protected NodeInteractionFlags DrawHandleButtonBox(Vector2 pNodeOSP, float pCanvasScaling, ImDrawListPtr pDrawList, out bool tHndCtxMnu)
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
            tHndCtxMnu = ImGui.Selectable(" …", false, ImGuiSelectableFlags.DontClosePopups, tBSize);
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
        protected NodeInteractionFlags DrawHandeExtraOptionPU()
        {
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            bool tIsOpeningEditPU = false;
            if (ImGui.BeginPopup(this.GetExtraOptionPUGuiId()))
            {
                // Select all
                if (ImGui.Selectable("Select all child nodes"))
                {
                    tRes |= NodeInteractionFlags.RequestSelectAllChild;
                }
                // Pack all
                if (ImGui.Selectable(this.mPackingStatus == PackingStatus.PackingDone ? $"Unpack {this.mPack.Count} child node(s)" : "Pack up all child nodes"))
                {
                    this.mPackingStatus = this.mPackingStatus switch
                    {
                        PackingStatus.PackingDone => PackingStatus.UnpackingUnderway,
                        PackingStatus.None => PackingStatus.PackingUnderway,
                        _ => this.mPackingStatus
                    };
                }
                ImGui.Separator();
                // Edit
                if (ImGui.Selectable("Edit node", false, ImGuiSelectableFlags.DontClosePopups))
                {
                    this.NodeEditPU_VarsInit();
                    tIsOpeningEditPU = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
                tRes |= NodeInteractionFlags.Internal | NodeInteractionFlags.LockSelection;
            }
            if (tIsOpeningEditPU) ImGui.OpenPopup($"##nepu{this.mId}");
            tRes |= this.DrawNodeEditPU();

            return tRes;
        }
        protected string GetExtraOptionPUGuiId() => $"##hepu{this.mId}";
        protected NodeInteractionFlags DrawNodeEditPU()
        {
            if (!this._isBeingEdited || this._newHeader == null || !this._newColorUnique.HasValue) return NodeInteractionFlags.None;
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            ImGui.SetNextWindowSize(new(200, 300));
            if (ImGui.BeginPopup($"##nepu{this.mId}"))
            {
                // Edit fields
                ImGui.BeginGroup();
                ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 1.5f);
                ImGui.BeginChild("##nepuw", new(ImGui.GetWindowContentRegionMax().X / 6 * 5f, ImGui.GetContentRegionAvail().Y), true);
                tRes |= this.DrawNodeEditPU_Fields();
                ImGui.EndChild();
                ImGui.PopStyleVar();
                ImGui.EndGroup();
                // Op buttons
                ImGui.SameLine();
                ImGui.BeginGroup();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                {
                    this.NodeEditPU_VarsSave();
                }
                if (ImGui.Button("×"))
                {
                    this.NodeEditPU_VarsCancel();
                }
                ImGui.EndGroup();

                ImGui.EndPopup();
                tRes |= NodeInteractionFlags.Internal | NodeInteractionFlags.LockSelection;
            }
            else       // split it here so that the save button wouldn't close the pop up
            {
                this.NodeEditPU_VarsCancel();
            }
            return tRes;
        }
        protected virtual NodeInteractionFlags DrawNodeEditPU_Fields()
        {
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            if (this._newColorUnique == null) return tRes;

            ImGui.Text("Header");
            ImGui.SameLine();
            ImGui.InputText("##nodeHeader", ref this._newHeader, 200);
            if (ImGui.CollapsingHeader("Colors"))
            {
                var tCol = this._newColorUnique.Value;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.ColorPicker4("##colp", ref tCol, ImGuiColorEditFlags.NoSidePreview);
                this._newColorUnique = tCol;
            }

            return tRes;
        }
        protected virtual void NodeEditPU_VarsInit()
        {
            this._isBeingEdited = true;
            this._newHeader = this.mContent.GetHeader();
            this._newColorUnique = new(this.mStyle.colorUnique.X, this.mStyle.colorUnique.Y, this.mStyle.colorUnique.Z, this.mStyle.colorUnique.W);
        }
        protected virtual void NodeEditPU_VarsSave()
        {
            if (this._newHeader == null || !this._newColorUnique.HasValue) return;
            this.SetHeader(this._newHeader, pAdjustWidthOnly: true, pChooseGreaterWidth: true);
            this.mStyle.colorUnique = this._newColorUnique.Value;
        }
        protected virtual void NodeEditPU_VarsCancel()
        {
            this._isBeingEdited = false;
            this._newHeader = null;
            this._newColorUnique = null;
        }
        /// <summary>Draw this in NodeCanvas. Drawing it in Node would mask the thing.</summary>
        public NodeInteractionFlags DrawEdgePlugButton(ImDrawListPtr pDrawList, Vector2 pNodeOSP, bool pIsActive, bool pIsEstablishingConn = false)
        {
            NodeInteractionFlags tRes = NodeInteractionFlags.None;
            Vector2 tSize = new(4f, 4f);
            bool tIsHovered = false;
            Vector2 tOriAnchor = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(pNodeOSP - (tSize * 3f));
            if (ImGui.InvisibleButton($"eb{this.mId}", tSize * 3f, ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonLeft))
            {
                // LMB: Collapse child nodes
                if (ImGui.GetIO().MouseReleased[0])
                {
                    this.mPackingStatus = this.mPackingStatus switch
                    {
                        PackingStatus.PackingDone => PackingStatus.UnpackingUnderway,
                        PackingStatus.None => PackingStatus.PackingUnderway,
                        _ => this.mPackingStatus
                    };
                }
                // RMB: Node conn
                else if (ImGui.GetIO().MouseReleased[1])
                {
                    tRes |= pIsEstablishingConn ? NodeInteractionFlags.UnrequestingEdgeConn : NodeInteractionFlags.RequestingEdgeConn;
                }

            }
            else if (UtilsGUI.SetTooltipForLastItem(string.Format("[Left-click] to {0}\n[Right-click] to start connecting nodes", this.mPackingStatus == PackingStatus.PackingDone ? $"unpack {this.mPack.Count} child node(s)" : "pack up all child nodes")))
            {
                tIsHovered = true;
            }
            ImGui.SetCursorScreenPos(tOriAnchor);
            // Draw
            pDrawList.AddCircleFilled(
                pNodeOSP - tSize, tSize.X * ((tIsHovered || pIsEstablishingConn || (this.mPackingStatus != PackingStatus.None)) ? 2.5f : 1), 
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(
                    this.mPackingStatus == PackingStatus.None ? UtilsGUI.Colors.NodeFg : UtilsGUI.Colors.NodePack, 
                    (pIsActive || tIsHovered || pIsEstablishingConn) ? 1f : 0.7f)));
            pDrawList.AddCircleFilled(pNodeOSP - tSize, (tSize.X * 0.7f) * ((tIsHovered || pIsEstablishingConn || (this.mPackingStatus != PackingStatus.None)) ? 2.5f : 1), ImGui.ColorConvertFloat4ToU32(this.mStyle.colorBg));
            pDrawList.AddCircleFilled(pNodeOSP - tSize, (tSize.X * 0.5f) * ((tIsHovered || pIsEstablishingConn) ? 2.5f : 1), ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, (pIsActive || pIsEstablishingConn) ? 0.55f : 0.25f)));
            return tRes;
        }

        public abstract void Dispose();

        public class NodeStyle
        {
            [JsonProperty]
            private Vector2 size;
            [JsonProperty]
            private Vector2 sizeHandle = Vector2.Zero;
            [JsonProperty]
            private Vector2 sizeBody = Vector2.Zero;
            [JsonProperty]
            private Vector2 minSize;
            public Vector2 handleTextPadding = new(3, 0);
            [JsonProperty]
            private Vector2 handleTextSize = Vector2.Zero;
            [JsonProperty]
            private Vector2? minestMinSize = null;

            public Vector4 colorUnique = UtilsGUI.Colors.GenObj_BlueAction;
            public Vector4 colorBg = UtilsGUI.Colors.NodeBg;
            public Vector4 colorFg = UtilsGUI.Colors.NodeFg;

            public NodeStyle(Vector2 size, Vector2 minSize, Vector2? minestMinSize = null)
            {
                this.minestMinSize = minestMinSize;
                PluginLog.LogDebug($"> Initing NodeStyle...");
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
                PluginLog.LogDebug($"> Setting handleTextSize...");
                this.SetMinSize(this.GetHandleTextSize() + Node.nodeInsidePadding * 2 + new Vector2(Node.handleButtonBoxItemWidth * 3, 0));
            }
            private void SetMinSize(Vector2 handleSize)
            {
                this.minSize.X = handleSize.X < (minestMinSize == null ? Node.minHandleSize.X : minestMinSize.Value.X) 
                                 ? (minestMinSize == null ? Node.minHandleSize.X : minestMinSize.Value.X) 
                                 : handleSize.X;
                this.minSize.Y = handleSize.Y < (minestMinSize == null ? Node.minHandleSize.Y : minestMinSize.Value.Y) 
                                 ? (minestMinSize == null ? Node.minHandleSize.Y : minestMinSize.Value.Y) 
                                 : handleSize.Y;
                PluginLog.LogDebug($"> pH={handleSize} M={this.minSize} mM={this.minestMinSize} nM={Node.minHandleSize}");
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

        public enum PackingStatus
        {
            None = 0,       // synonamous with UnpackingDone
            PackingUnderway = 1,
            PackingDone = 2,
            UnpackingUnderway = 3
        }
    }
}
