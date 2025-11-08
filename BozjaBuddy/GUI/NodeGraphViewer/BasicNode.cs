using System.Numerics;
using Dalamud.Bindings.ImGui;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Logging;
using BozjaBuddy.GUI.NodeGraphViewer.utils;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    /// <summary>
    /// Represents a basic info node with handle and description. 
    /// </summary>
    internal class BasicNode : Node
    {
        public new const string nodeType = "BasicNode";
        public override string mType { get; } = BasicNode.nodeType;

        protected string? _newDescription = null;

        public BasicNode() : base()
        {
            this.mStyle.colorUnique = UtilsGUI.Colors.NormalBar_Grey;
        }
        protected override NodeInteractionFlags DrawBody(Vector2 pNodeOSP, float pCanvasScaling)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.NodeText);

            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(this.mContent.GetDescription());
            ImGui.PopTextWrapPos();

            ImGui.PopStyleColor();
            return NodeInteractionFlags.None;
        }
        protected override NodeInteractionFlags DrawNodeEditPU_Fields()
        {
            var tRes = base.DrawNodeEditPU_Fields();
            if (this._newDescription == null) return tRes;

            if (ImGui.CollapsingHeader("Content"))
            {
                ImGui.InputTextMultiline("##nepum", ref this._newDescription, 500, new(ImGui.GetContentRegionMax().X, 50));
            }

            return tRes;
        }
        protected override void NodeEditPU_VarsInit()
        {
            base.NodeEditPU_VarsInit();
            this._newDescription = this.mContent.GetDescription();
        }
        protected override void NodeEditPU_VarsSave()
        {
            if (this._newDescription == null) return;
            base.NodeEditPU_VarsSave();
            this.mContent.SetDescription(this._newDescription);
        }
        protected override void NodeEditPU_VarsCancel()
        {
            base.NodeEditPU_VarsCancel();
            this._newDescription = null;
        }

        public override void Dispose()
        {

        }
    }
}
