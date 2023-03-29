using System;
using BozjaBuddy.Windows;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;

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
        public override void DrawGUIDebug()
        {

        }

        public override void Dispose() { }
    }
}
