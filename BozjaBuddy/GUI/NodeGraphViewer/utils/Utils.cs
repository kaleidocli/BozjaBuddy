using Dalamud.Logging;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using BozjaBuddy.Utils;
using System.Drawing;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    public class Utils
    {
        public static float defaultFontScale { get; } = 1;
        private static float currFontScale = 1;
        /// <summary>
        /// This stuff is super funky.
        /// Calling the push/pop pair twice (in the same child/main window?) will cause the scaling to be applied twice. Prob even more.
        /// https://github.com/ocornut/imgui/issues/1018#issuecomment-1397768472
        /// https://forums.x-plane.org/index.php?/forums/topic/174419-imgui-text-size/
        /// </summary>
        public static void PushFontScale(float scale)
        {
            //ImGui.GetFont().Scale *= scale;
            //ImGui.PushFont(ImGui.GetFont());
            ImGui.SetWindowFontScale(ImGui.GetFont().Scale * scale);
            Utils.currFontScale = ImGui.GetFont().Scale * scale;
        }
        public static void PopFontScale()
        {
            ImGui.SetWindowFontScale(Utils.defaultFontScale);
            Utils.currFontScale = Utils.defaultFontScale;
            //ImGui.PopFont();
            //ImGui.GetFont().Scale = defaultFontScale;
        }
        public static float GetCurrFontScale() => Utils.currFontScale;
        /// https://stackoverflow.com/questions/5953552/how-to-get-the-closest-number-from-a-listint-with-linq
        public static float? GetClosestItem(float itemToCompare, List<float> items)
            => items.Count == 0
               ? null
               : items.Aggregate((x, y) => Math.Abs(x - itemToCompare) < Math.Abs(y - itemToCompare) ? x : y);
        public static void AlignRight(float pTargetItemWidth, bool pConsiderScrollbar = false, bool pConsiderImguiPaddings = true)
        {
            ImGuiStylePtr tStyle = ImGui.GetStyle();
            float tPadding = (pConsiderImguiPaddings ? tStyle.WindowPadding.X + tStyle.FramePadding.X : 0)
                             + (pConsiderScrollbar ? tStyle.ScrollbarSize : 0);
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - pTargetItemWidth - tPadding);
        }
        public static float GetGreaterVal(float v1, float v2) => v1 > v2 ? v1 : v2;
        public static float GetSmallerVal(float v1, float v2) => v1 < v2 ? v1 : v2;

        /// https://git.anna.lgbt/ascclemens/QuestMap/src/branch/main/QuestMap/PluginUi.cs#L778
        public static void DrawArrow(ImDrawListPtr drawList, Vector2 start, Vector2 end, Vector4 color, float baseMultiplier = 1.5f)
        {
            const float arrowAngle = 30f;
            var dir = end - start;
            var h = dir;
            dir /= dir.Length();

            var s = new Vector2(-dir.Y, dir.X);
            s *= (float)(h.Length() * Math.Tan(arrowAngle * 0.5f * (Math.PI / 180f)));

            drawList.AddTriangleFilled(
                start + s * baseMultiplier,
                end,
                start - s * baseMultiplier,
                ImGui.ColorConvertFloat4ToU32(color)
            );
        }
        /// <summary>https://stackoverflow.com/questions/51217546/test-for-rectangle-line-intersection</summary>
        public static bool IsLineIntersectRect(Vector2 a, Vector2 b, Area area)
        {
            if (Math.Min(a.X, b.X) > area.end.X) return false;   // *
            if (Math.Max(a.X, b.X) < area.start.X) return false;   // *
            if (Math.Min(a.Y, b.Y) > area.end.Y) return false;   // *
            if (Math.Max(a.Y, b.Y) < area.start.Y) return false;   // *

            if (area.CheckPosIsWithin(a)) return true;   // **
            if (area.CheckPosIsWithin(a)) return true;   // **

            return true;
        }
    }

    [Flags]
    public enum NodeInteractionFlags
    {
        None = 0,
        Handle = 1,
        Internal = 2,
        LockSelection = 4,      // requesting canvas to stop capture ALL nodes select/deselect/remove
        Edge = 8,
        RequestingEdgeConn = 16,
        UnrequestingEdgeConn = 32,
        RequestEdgeRemoval = 64,
        RequestSelectAllChild = 128
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
        StateCanvasDrag = 64,
        NoCanvasDrag = 128,
        NoCanvasZooming = 256
    }
}
