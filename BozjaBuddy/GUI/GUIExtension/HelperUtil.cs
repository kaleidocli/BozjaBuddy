using System;
using System.Collections.Generic;
using System.Numerics;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;

namespace BozjaBuddy.GUI.GUIExtension
{
    /// <summary>
    /// https://git.anna.lgbt/ascclemens/Glamaholic/src/branch/main/Glamaholic/Ui/Helpers/HelperUtil.cs
    /// </summary>
    internal static class HelperUtil
    {
        internal const ImGuiWindowFlags HelperWindowFlags = ImGuiWindowFlags.NoBackground
                                                            | ImGuiWindowFlags.NoDecoration
                                                            | ImGuiWindowFlags.NoCollapse
                                                            | ImGuiWindowFlags.NoTitleBar
                                                            | ImGuiWindowFlags.NoNav
                                                            | ImGuiWindowFlags.NoNavFocus
                                                            | ImGuiWindowFlags.NoNavInputs
                                                            | ImGuiWindowFlags.NoResize
                                                            | ImGuiWindowFlags.NoScrollbar
                                                            | ImGuiWindowFlags.NoSavedSettings
                                                            | ImGuiWindowFlags.NoFocusOnAppearing
                                                            | ImGuiWindowFlags.AlwaysAutoResize
                                                            | ImGuiWindowFlags.NoDocking;

        internal static unsafe Vector2? DrawPosForAddon(AtkUnitBase* addon, Vector2? extSize, bool right = false)
        {
            if (addon == null)
            {
                return null;
            }

            var root = addon->RootNode;
            if (root == null)
            {
                return null;
            }

            var xModifier = right && extSize != null
                ? root->Width * addon->Scale - extSize!.Value.X
                : 0;
            //var yModifier = extSize != null
            //    ? extSize!.Value.Y
            //    : 0;

            return ImGuiHelpers.MainViewport.Pos
                   + new Vector2(addon->X, addon->Y)
                   + Vector2.UnitX * xModifier
                   - Vector2.UnitY
                   - Vector2.UnitY * (ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().FrameBorderSize);
        }

        internal class HelperStyles : IDisposable
        {
            internal HelperStyles()
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
            }

            public void Dispose()
            {
                ImGui.PopStyleVar(3);
            }
        }

        internal static unsafe void DrawHelper(AtkUnitBase* addon, string id, Action extDrawer, Vector2? extSize, Vector2? padding = null, bool right = false, bool isDisabled = false)
        {
            var drawPos = DrawPosForAddon(addon, extSize, right: right) + (right ? -1 : 1) * (padding ?? Vector2.Zero);
            if (drawPos == null)
            {
                return;
            }

            using (new HelperStyles())
            {
                // get first frame
                ImGui.SetNextWindowPos(drawPos.Value, ImGuiCond.Appearing);   
                if (!ImGui.Begin($"##{id}", HelperWindowFlags | (isDisabled ? ImGuiWindowFlags.NoInputs : ImGuiWindowFlags.None)))
                {
                    if (extSize.HasValue) ImGui.SetWindowSize(extSize!.Value);

                    ImGui.End();
                    return;
                }
            }

            try
            {
                if (isDisabled) { ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.4f); }
                extDrawer();
                if (isDisabled) { ImGui.PopStyleVar(); }
            }
            catch (Exception ex)
            {
                if (isDisabled) { ImGui.PopStyleVar(); }
            }

            ImGui.SetWindowPos(drawPos.Value);

            ImGui.End();
        }
        internal static unsafe void DrawHelper(Plugin plugin, ExtGui extGui, Vector2? extSize, Vector2? padding = null, bool right = false, bool isDisabled = false)
        {
            var tAddon = (AtkUnitBase*)plugin.GameGui.GetAddonByName(extGui.mAddonName).Address;
            if (tAddon == null) { return; }
            HelperUtil.DrawHelper(
                tAddon, 
                extGui.mId, 
                extGui.Draw, 
                extSize, 
                padding, 
                right,
                isDisabled);
        }
    }
}
