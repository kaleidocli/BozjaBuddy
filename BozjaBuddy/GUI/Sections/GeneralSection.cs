﻿using System;
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
using Dalamud.Interface.Colors;
using System.Linq;

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
            //// Test window
            //UtilsGUI.WindowLinkedButton(mPlugin, TestWindow.kHandle, Dalamud.Interface.FontAwesomeIcon.Tape);
            //ImGui.SameLine();
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
            GeneralSection.DrawSearchAllBox(this.mPlugin, ref tSearchVal, pTextBoxWidth: ImGui.GetContentRegionAvail().X - 52);
            this.mGuiVars["searchAll"] = tSearchVal;
            // Help button
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Question))
            {
                ImGui.OpenPopup("##helperpu");
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("Keybinds\n\n1. With a link (hinted by symbol »):\n- Hover for quick info.\n- [LMB] to open link in info viewer section at the bottom.\n- [RMB] to see more options (marketboard, location, alarm, etc.).\n- For action's icon link, [Shift+LMB/RMB] will add the selected action to the currently edited Custom Loadout.");
            }
            if (ImGui.BeginPopup("##helperpu", ImGuiWindowFlags.NoResize))
            {
                GeneralSection.DrawHelper(this.mPlugin);
                ImGui.EndPopup();
            }
            // Section switch button
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ProjectDiagram, defaultColor: this.mPlugin.Configuration.isAuxiVisible == 0
                                                                                                         ? null
                                                                                                         : this.mPlugin.Configuration.isAuxiVisible == 1
                                                                                                            ? UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_Red, 0.15f)
                                                                                                            : UtilsGUI.Colors.MycItemBoxOverlay_Red))
            {
                this.mPlugin.Configuration.isAuxiVisible = this.mPlugin.Configuration.isAuxiVisible == 2 ? 0 : this.mPlugin.Configuration.isAuxiVisible + 1;
                this.mPlugin.MainWindow.RearrangeSection();
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem($"Expanding info-graph: {(this.mPlugin.Configuration.isAuxiVisible == 0 
                                                                        ? "Hidden" 
                                                                        : this.mPlugin.Configuration.isAuxiVisible == 0
                                                                            ? "Half"
                                                                            : "Full")}");
            }

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
            UtilsGUI.SetTooltipForLastItem("- Search by name.\n- Search by phrases: action, fate, fragment, mob, loadout.\n\nCapitalization insensitive. No regex.");

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
        private static void DrawHelper(Plugin pPlugin)
        {
            if (!ImGui.BeginTabBar("##helpertb")) return;
            
            // Tab: Keybinds
            if (ImGui.BeginTabItem("Keybinds"))
            {
                ImGui.BeginChild("##hw", new Vector2(500, 200));
                ImGui.PushTextWrapPos();
                ImGui.Text("1. [Alt] to toggle alternative layout while plugin's main window is focused.");
                ImGui.Separator();
                ImGui.Text("2. With a link (hinted by symbol » like");
                ImGui.SameLine();
                UtilsGUI.SelectableLink_WithPopup(pPlugin, "this", pPlugin.mBBDataManager.mLostActions.First().Value.GetGenId());
                ImGui.SameLine();
                ImGui.Text("):");
                ImGui.Text("- Hover for quick info.\n- [LMB] to open link in info viewer section at the bottom.\n- [RMB] to see available options (marketboard, location, alarm, etc.).\n- For action's icon link, [Shift+LMB/RMB] will add the selected action to the currently edited Custom Loadout.");
                ImGui.PopTextWrapPos();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            // Tab: General
            if (ImGui.BeginTabItem("General"))
            {
                ImGui.BeginChild("##hw", new Vector2(550, 250));
                ImGui.PushTextWrapPos();
                ImGui.Text("1. Any website or communities mentioned in this plugin do not sponsor or affiliated with the plugin in anyway.");
                ImGui.Separator();
                UtilsGUI.GreyText("2. No content in this plugin is created/owned by the devs. Any issues related to ownership or validity of community-created content, such as recommended loadouts; please let us know.");
                ImGui.Separator();
                ImGui.Text("3. The plugin is designed with convenience in mind. However, if anything feels intrusive to you, feel free to let us know.");
                ImGui.Separator();
                UtilsGUI.GreyText("4. The plugin will not have any automatic or braindead functionality (i.e. solving mechanics for you)");
                ImGui.Separator();
                ImGui.Text("5. Technical issues can be sent through Dalamud's feedback function.");
                ImGui.SameLine(); UtilsGUI.ShowHelpMarker("This functionality can be found in Dalamud's plugin browser > Next to where you toggle on/off a plugin.");
                ImGui.Text("Suggestions/inquiries please forward to XIVLauncher's discord > Plugin-help-forum > Bozja-buddy.");
                ImGui.Separator();
                UtilsGUI.GreyText("6. Out of respect, we ask our users to AVOID mentioning this plugin in any community.");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Info in this plugin should be taken with a grain of salt.\nWe don't want people to annoy the mods/hosts by bringing this plugin up as an argument or excuse (i.e. '...but Bozja Buddy made me bring wrong stuff').\nWe created this plugin to makes life easier, not to become a nuisance to anyone.");
                ImGui.PopTextWrapPos();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
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

                //ImGui.Text(tTextNode->ToString() ?? "None");
            }
            catch (Exception e)
            {

            }
        }

        public override void DrawGUIDebug()
        {

        }

        public override void Dispose() { }
    }
}
