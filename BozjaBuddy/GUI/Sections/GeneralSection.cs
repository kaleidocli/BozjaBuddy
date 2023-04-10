using System;
using BozjaBuddy.Windows;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace BozjaBuddy.GUI.Sections
{
    internal class GeneralSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }

        public GeneralSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override bool DrawGUI()
        {
            // Button Config
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog))
            {
                this.mPlugin.WindowSystem.GetWindow("Config - BozjaBuddy")!.IsOpen = true;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Open config window.");
                ImGui.EndTooltip();
            }

            ImGui.SameLine();
            // Button Alarm
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Bell))
            {
                this.mPlugin.WindowSystem.GetWindow("Alarm - BozjaBuddy")!.IsOpen = true;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Open alarm window.");
                ImGui.EndTooltip();
            }
            ImGui.SameLine();
            // Alarm notification bar
            AlarmWindow.DrawAlarmNotificationBar(this.mPlugin, "generalSection", pIsStretching: false, ImGui.GetContentRegionAvail().X / 4 + (float)2.5);

            return true;
        }

        public unsafe void DrawActionCount()
        {
            try
            {
                AtkUnitBase* tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCItemBox");
                if (tAddon == null || !tAddon->IsVisible) return;

                var tLv1 = tAddon->UldManager.SearchNodeById(201);
                if (tLv1 == null) return;
                var tLv2 = tLv1->GetComponent()->UldManager.SearchNodeById(5);
                if (tLv2 == null) return;
                var tLv3 = tLv2->GetComponent()->UldManager.SearchNodeById(6);
                if (tLv3 == null) return;
                PluginLog.LogDebug(String.Format("0x{0:x}", (new IntPtr(tLv3))));

                //ImGui.Text(tTextNode->ToString() ?? "None");
                PluginLog.LogDebug(tLv3->GetAsAtkTextNode()->NodeText.ToString() ?? "None");
            }
            catch (Exception e)
            {
                PluginLog.Debug(e.ToString());
            }
        }

        public override void DrawGUIDebug()
        {

        }

        public override void Dispose() { }
    }
}
