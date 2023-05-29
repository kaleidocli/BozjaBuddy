using BozjaBuddy.Data;
using BozjaBuddy.Filter.LostActionTableSection;
using BozjaBuddy.Utils;
using BozjaBuddy.Windows;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System.Numerics;

namespace BozjaBuddy.GUI.GUIExtension
{
    internal class ExtGui_MycItemBox : ExtGui
    {
        public override string mId { get; set; } = "extMycItemBox";
        public override string mAddonName { get; set; } = "MYCItemBox";
        private Plugin mPlugin;
        public FilterName mFilterName { get; set; } = new();
        public FilterRole mFilterRole { get; set; } = new();
        public FilterFragment mFilterFragment { get; set; }
        public FilterWeight mFilterWeight { get; set; } = new();
        private ExtGui_MycItemBagTrade mExtGui_MycItemBagTrade;

        private ExtGui_MycItemBox() { }
        public ExtGui_MycItemBox(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.mExtGui_MycItemBagTrade = new(this.mPlugin);
            this.mFilterFragment = new(true, this.mPlugin);
            this.mFilterRole.mIsCompact = false;
            this.mFilterWeight.mIsCompact = false;

            this.mFilterName.mIsContainedInCell = false;
            this.mFilterRole.mIsContainedInCell = false;
            this.mFilterFragment.mIsContainedInCell = false;
            this.mFilterWeight.mIsContainedInCell = false;
        }

        public bool CanPassAllFilters(LostAction pLostAction)
            => this.mFilterName.CanPassFilter(pLostAction)
                && this.mFilterRole.CanPassFilter(pLostAction)
                && this.mFilterFragment.CanPassFilter(pLostAction)
                && this.mFilterWeight.CanPassFilter(pLostAction);
        public bool IsAnyFilterActive(LostAction pLostAction)
        {
            return this.mFilterName.GetCurrValue() != string.Empty
                || this.mFilterRole.GetCurrValue() != "_____"
                || this.mFilterFragment.GetCurrValue() != string.Empty
                || this.mFilterWeight.GetCurrValue() != "0-9999";
        }

        public override void Draw()
        {
            // Getting Addon info
            Vector2 tOrigin = ImGui.GetCursorScreenPos();
            Vector2? tEnd = null;
            float tScale = 0;
            Vector2 tAnchor1;
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(this.mAddonName);
                if (tAddon != null)
                {
                    var tNode = (AtkResNode*)tAddon->RootNode;
                    if (tNode != null)
                    {
                        tEnd = tOrigin + new Vector2(tAddon->GetScaledWidth(true), tAddon->GetScaledHeight(true));
                    }
                    tScale = tAddon->Scale;
                }
            }
            //ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.GenObj_BlueAction, UtilsGUI.Colors.GenObj_BlueAction.W + 0.1f));
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.Colors.MycItemBoxOverlay_RedDarkBright);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(1, 0));
            // Setting button
            ImGui.SameLine();
            if (tEnd != null) ImGui.SetCursorScreenPos(new Vector2(tEnd.Value.X - 33.25f, tOrigin.Y));
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Cogs))
            {
                ImGui.OpenPopup("pu");
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("UI Assist configs for Lost Find Cache window\n(also found in: Config > UI Assist > [B])");
            }
            if (ImGui.BeginPopup("pu"))
            {
                ConfigWindow.DrawTabUiHint_LostFindCacheSection(this.mPlugin, pFixedSize: 300);
                ImGui.EndPopup();
            }
            // Open main window button
            ImGui.SetCursorScreenPos(tOrigin + new Vector2(5f, 0));
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare))
            {
                this.mPlugin.WindowSystem.GetWindow("Bozja Buddy")!.IsOpen = !(this.mPlugin.WindowSystem.GetWindow("Bozja Buddy")!.IsOpen);
            }
            UtilsGUI.SetTooltipForLastItem("Open Bozja Buddy window");
            // Disable UI Assist for MycItemBox and MycItemBagTrade
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(
                    this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All
                    ? FontAwesomeIcon.EyeSlash
                    : FontAwesomeIcon.Eye
                    )
                )
            {
                this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All = !this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All;
                this.mPlugin.Configuration.Save();
            }
            UtilsGUI.SetTooltipForLastItem("Enable / Disable most UI features for this section.");
            ImGui.PopStyleVar();

            if (this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All)
            {
                ImGui.PopStyleColor();
                return;
            }

            if (!this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar)
            {
                // Filters
                ImGui.SameLine();
                ImGui.PushID("ext_fname");
                ImGui.PushItemWidth(120 * tScale);
                this.mFilterName.DrawFilterGUI();
                ImGui.PopItemWidth();
                ImGui.PopID();

                ImGui.SameLine();
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));
                this.mFilterRole.DrawFilterGUI(); 
                ImGui.PopStyleVar();

                tAnchor1 = ImGui.GetCursorScreenPos();
                ImGui.PushID("ext_ffrag");
                ImGui.SameLine();
                ImGui.PushItemWidth(120 * tScale);
                this.mFilterFragment.DrawFilterGUI();
                ImGui.PopItemWidth();
                ImGui.PopID();

                ImGui.SameLine();
                if (tScale > 0.8)
                {
                    ImGui.Text("Weight: ");
                    ImGui.SameLine();
                }
                ImGui.PushItemWidth(90 * tScale);
                this.mFilterWeight.DrawFilterGUI();
                ImGui.PopItemWidth();

                // Filter fields clear button
                ImGui.SameLine();
                if (ImGui.Button("  X  "))
                {
                    this.mFilterName.ClearInputValue();
                    this.mFilterWeight.ClearInputValue();
                    this.mFilterFragment.ClearInputValue();
                    this.mFilterRole.ClearInputValue();
                }
                UtilsGUI.SetTooltipForLastItem("Clear all fields.");
            }

            ImGui.PopStyleColor();

            // Holster drawing (pair with MycItemBox to limit the draw to when MycItemBox is visible)
            unsafe
            {
                ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.Colors.MycItemBoxOverlay_RedDarkBright);
                try
                {
                    HelperUtil.DrawHelper(
                        this.mPlugin,
                        this.mExtGui_MycItemBagTrade,
                        tEnd - tOrigin,
                        padding: new Vector2(2, -ImGui.CalcTextSize("A").Y)
                    );
                }
                catch (System.Exception e) { PluginLog.LogDebug(e.Message); }
                ImGui.PopStyleColor();
            }
        }
    }
}
