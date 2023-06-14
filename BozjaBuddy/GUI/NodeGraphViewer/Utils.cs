using Dalamud.Logging;
using ImGuiNET;
using System;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    internal class Utils
    {
        public static float defaultFontScale { get; } = 1;
        /// <summary>
        /// https://github.com/ocornut/imgui/issues/1018#issuecomment-1397768472
        /// </summary>
        public static void PushFontScale(float scale)
        {
            ImGui.GetFont().Scale *= scale;
            ImGui.PushFont(ImGui.GetFont());
        }
        public static void PopFontScale()
        {
            ImGui.PopFont();
            ImGui.GetFont().Scale = Utils.defaultFontScale;
        }
    }

    [Flags]
    public enum InputFlag
    {
        None = 0,
        MouseRight = 1,
        MouseLeft = 2,
        MouseMiddle = 3,
        MouseUp = 4,
        MouseDown = 5,
        KeyShift = 6,
        KeyCtrl = 7,
        KeyAlt = 8,
        KeyPlus = 9,
        KeyMinus = 10,
        KeyZero = 11
    }
}
