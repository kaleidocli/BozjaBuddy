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
                UtilsGUI.SetTooltipForLastItem("Keybinds\n\n1. [Alt] to toggle alternative layout while plugin's main window is focused.\n2. With a link (hinted by symbol »):\n- Hover for quick info.\n- [LMB] to open link in info viewer section at the bottom.\n- [RMB] to see more options (marketboard, location, alarm, etc.).\n- For action's icon link, [Shift+LMB/RMB] will add the selected action to the currently edited Custom Loadout.");
            }
            if (ImGui.BeginPopup("##helperpu", ImGuiWindowFlags.NoResize))
            {
                GeneralSection.DrawHelper(this.mPlugin);
                ImGui.EndPopup();
            }
            // Section switch button
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsUpDown, defaultColor: this.mPlugin.Configuration.mIsAuxiFocused
                                                                                                         ? UtilsGUI.Colors.MycItemBoxOverlay_Red
                                                                                                         : null))
            {
                this.mPlugin.Configuration.mIsAuxiFocused = !this.mPlugin.Configuration.mIsAuxiFocused;
                this.mPlugin.MainWindow.RearrangeSection();
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem($"Alternative layout: {(this.mPlugin.Configuration.mIsAuxiFocused ? "ON" : "OFF")}\n- Bring the information viewer to the top.\n\n(can be toggled by pressing [Alt] while this plugin window is being focused)");
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
        private static void DrawHelper(Plugin pPlugin)
        {
            if (!ImGui.BeginTabBar("##helpertb")) return;
            
            // Tab: General
            if (ImGui.BeginTabItem("General"))
            {
                ImGui.BeginChild("##hw", new Vector2(500, 200));
                ImGui.PushTextWrapPos();
                ImGui.Text("1. Any website or communities mentioned in this plugin are not sponsored or affiliated with the author in anyway.");
                ImGui.Separator();
                ImGui.Text("2. Any issues related to ownership or validity of community-created content, such as Recommended loadouts; please let us know and we may edited/removed accordingly.");
                ImGui.Separator();
                ImGui.Text("3. The plugin is designed with convenience in mind. However, if anything feels intrusive to you, feel free to let us know.");
                ImGui.Separator();
                ImGui.Text("4. The plugin will not have any automatical or braindead functionality (e.g. solve mechanics you, etc.)");
                ImGui.Separator();
                ImGui.Text("5. Technical issues can be sent through normal feedback function. Suggestions/inquiries please forward to XIVLauncher's discord > Plugin-help-forum > Bozja-buddy.");
                ImGui.PopTextWrapPos();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
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
