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
        public static float? GetClosestItem(float itemToCompare, List<float> items)
            => items.Count == 0
               ? null
               : items.Aggregate((x, y) => Math.Abs(x - itemToCompare) < Math.Abs(y - itemToCompare) ? x : y);
        public static void AlignRight(float pTargetItemWidth, bool pConsiderScrollbar = false, bool pConsiderImguiPaddings = true)
        {
            ImGuiStylePtr tStyle = ImGui.GetStyle();
            float tPadding = (pConsiderImguiPaddings ? (tStyle.WindowPadding.X + tStyle.FramePadding.X) : 0)
                             + (pConsiderScrollbar ? tStyle.ScrollbarSize : 0);
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - pTargetItemWidth - tPadding);
        }
    }

    [Flags]
    public enum NodeInteractionFlags
    {
        None = 0,
        Handle = 1,
        Internal = 2,
        LockSelection = 4
    }

    [Flags]
    public enum CanvasDrawFlags
    {
        None = 0,
        NoInteract = 1,
        NoCanvasInteraction = 2,
        NoNodeInteraction = 4,
        NoNodeDrag = 8,
        NoNodeSnap = 16,
        StateNodeDrag = 32,
        StateCanvasDrag = 64
    }
}
