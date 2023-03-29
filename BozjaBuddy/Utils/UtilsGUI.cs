using Dalamud.Interface.Colors;
using ImGuiNET;
using System.Numerics;
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
            UtilsGUI.SetTooltipForLastItem(desc);
        }
        public static void SetTooltipForLastItem(string tDesc, float tSize = 450.0f)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(tSize);
            ImGui.TextUnformatted(tDesc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
        public static void TextWithHelpMarker(string pText, string pHelpMarkerText = "", Vector4? pColor = null)
        {
            if (pColor != null)
                ImGui.TextColored(pColor.Value, pText);
            else
                ImGui.Text(pText);
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker(pHelpMarkerText);
        }
        
        internal class Colors
        {
            public static Vector4 NormalText_White = ImGuiColors.DalamudWhite2;
            public static Vector4 BackgroundText_Grey = ImGuiColors.ParsedGrey;
            public static Vector4 ActivatedText_Green = ImGuiColors.ParsedGreen;
            public static Vector4 NormalBar_Grey = Utils.RGBAtoVec4(165, 165, 165, 80);
            public static Vector4 ActivatedBar_Green = Utils.RGBAtoVec4(176, 240, 6, 80);
        }
    }
}
