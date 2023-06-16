using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// https://stackoverflow.com/questions/5953552/how-to-get-the-closest-number-from-a-listint-with-linq
        public static float GetClosestItem(float itemToCompare, List<float> items)
            => items.Aggregate((x, y) => Math.Abs(x - itemToCompare) < Math.Abs(y - itemToCompare) ? x : y);
        public static Vector2 SnapToClosestPos(Vector2 currPos, List<float> xVals, List<float> yVals, float proximity)
        {
            var x = Utils.GetClosestItem(currPos.X, xVals);
            if (Math.Abs(x - currPos.X) < proximity) x = currPos.X;
            var y = Utils.GetClosestItem(currPos.Y, yVals);
            if (Math.Abs(y - currPos.Y) < proximity) y = currPos.Y;
            return new(x, y);
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
