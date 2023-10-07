using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;
using BozjaBuddy.GUI.Sections;
using Dalamud.Logging;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;
using Dalamud.Interface;

namespace BozjaBuddy.GUI.GUIExtension
{
    internal class ExtGui_MycInfo : ExtGui
    {
        public override string mId { get; set; } = "extMycInfo";
        public override string mAddonName { get; set; } = "MYCInfo";
        private Plugin mPlugin;
        private Dictionary<string, string> mGuiVars = new()
        {
            { "searchAll", "" }
        };

        private ExtGui_MycInfo() { }
        public ExtGui_MycInfo(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override void Draw()
        {
            // Getting addon info
            Vector2 tOrigin = ImGui.GetCursorScreenPos();
            Vector2? tEnd = null;
            float tScale = 0;
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

            // Open main window button
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare))
            {
                Plugin.GetWindow("Bozja Buddy")!.IsOpen = !(Plugin.GetWindow("Bozja Buddy")!.IsOpen);
            }
            UtilsGUI.SetTooltipForLastItem("Open Bozja Buddy window. To turn off this UI: Config > UI Assist > [A] > [1]");

            // Search all bar
            var tTemp = this.mGuiVars["searchAll"];
            ImGui.SameLine();
            GeneralSection.DrawSearchAllBox(
                this.mPlugin, 
                ref tTemp, 
                pPUSize: tEnd == null
                         ? null
                         : tEnd.Value - tOrigin,
                pTextBoxWidth: tEnd == null
                               ? null
                               : tEnd.Value.X - ImGui.GetCursorScreenPos().X - 3
                );
            this.mGuiVars["searchAll"] = tTemp;
        }
    }
}
