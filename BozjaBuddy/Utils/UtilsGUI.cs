using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Utils
{
    internal class UtilsGUI
    {
        // https://www.programcreek.com/cpp/?code=kswaldemar%2Frewind-viewer%2Frewind-viewer-master%2Fsrc%2Fimgui_impl%2Fimgui_widgets.cpp
        public static void ShowHelpMarker(string desc, string markerText = "(?)", bool disabled = true)
        {
            if (disabled)
                ImGui.TextDisabled(markerText);
            else
                ImGui.TextUnformatted(markerText);

            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(450.0f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
