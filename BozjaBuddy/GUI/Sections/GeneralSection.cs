using System;
using System.Numerics;
using BozjaBuddy.Windows;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using BozjaBuddy.Utils;
using BozjaBuddy.Data;
using System.Collections.Generic;
using Dalamud.Interface.Components;
using System.Runtime.CompilerServices;
using System.Data;

namespace BozjaBuddy.GUI.Sections
{
    internal class GeneralSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }
        private Dictionary<string, string> mGuiVars = new()
        {
            { "searchAll", "" }
        };

        public GeneralSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override bool DrawGUI()
        {
            // Char stats button
            UtilsGUI.WindowLinkedButton(mPlugin, CharStatsWindow.kHandle, Dalamud.Interface.FontAwesomeIcon.Portrait);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("[LMB] Character Stats window & Lost find Cache\n========================================\n");
                CharStatsWindow.Draw_CharStatsCompact(this.mPlugin);
                ImGui.EndTooltip();
            }
            // Button Alarm
            ImGui.SameLine();
            UtilsGUI.WindowLinkedButton(mPlugin, "Alarm - BozjaBuddy", Dalamud.Interface.FontAwesomeIcon.Bell, "Open alarm window.");
            // Alarm notification bar
            ImGui.SameLine();
            AlarmWindow.DrawAlarmNotificationBar(this.mPlugin, "generalSection", pIsStretching: false, ImGui.GetContentRegionAvail().X / 4 + (float)2.5);
            // Button Config
            ImGui.SameLine();
            UtilsGUI.WindowLinkedButton(mPlugin, "Config - BozjaBuddy", Dalamud.Interface.FontAwesomeIcon.Cog, "Open config window.");
            // Search all box
            string tSearchVal = this.mGuiVars["searchAll"];
            ImGui.SameLine();
            GeneralSection.DrawSearchAllBox(this.mPlugin, ref tSearchVal);
            this.mGuiVars["searchAll"] = tSearchVal;

            return true;
        }

        public static void DrawSearchAllBox(Plugin pPlugin, ref string pCurrValue, Vector2? pPUSize = null, float? pTextBoxWidth = null)
        {
            var tAnchor = ImGui.GetCursorScreenPos();

            if (pTextBoxWidth != null) ImGui.SetNextItemWidth(pTextBoxWidth.Value);
            ImGui.InputTextWithHint("", "Search all...", ref pCurrValue, 200);
            bool tIsInputActive = ImGui.IsItemActive();
            bool tIsInputActivated = ImGui.IsItemActivated();
            bool tIsItemPUOpened = false;
            UtilsGUI.SetTooltipForLastItem("- Search by name.\n- Search by phrases: action, fate, fragment, mob, loadout.\n\nCaptitalization is ignored. Nevertheless, the search function is still pretty barebone with no regex.");

            if (pCurrValue.Length != 0 && tIsInputActive)
                ImGui.OpenPopup("##searchAllPU");

            {
                ImGui.SetNextWindowPos(tAnchor + new Vector2(0, 25));
                ImGui.SetNextWindowSizeConstraints(new Vector2(50, 25), pPUSize ?? new Vector2(300, 300));

                if (ImGui.BeginPopup("##searchAllPU", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.ChildWindow))
                {
                    // Lost action
                    // Fragment
                    // FateCe
                    // Mob
                    // Loadout
                    foreach (GeneralObject o in pPlugin.mBBDataManager.mGeneralObjects.Values)
                    {
                        if (o.GetSalt() == GeneralObject.GeneralObjectSalt.Loadout)
                        {
                            if (!pPlugin.Configuration.mIsShowingRecLoadout && o.mId > 9999) continue;
                        }
                        string tCheck = o.mName + o.GetSalt() switch
                        {
                            GeneralObject.GeneralObjectSalt.LostAction => "action",
                            GeneralObject.GeneralObjectSalt.Fragment => "fragment",
                            GeneralObject.GeneralObjectSalt.Fate => "fate",
                            GeneralObject.GeneralObjectSalt.Mob => "mob",
                            GeneralObject.GeneralObjectSalt.Loadout => "loadout",
                            _ => ""
                        };
                        if (tCheck.Contains(pCurrValue, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (o.mTabColor.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, o.mTabColor!.Value);
                            UtilsGUI.SelectableLink_WithPopup(pPlugin, o.mName, o.GetGenId(), pIsWrappedToText: false, pIsClosingPUOnClick: false);
                            if (o.mTabColor.HasValue) ImGui.PopStyleColor();
                            if (ImGui.IsPopupOpen(o.mName)) tIsItemPUOpened = true;
                        }
                    }
                    if (!tIsInputActive && !ImGui.IsWindowFocused() && !tIsItemPUOpened)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
            }
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
