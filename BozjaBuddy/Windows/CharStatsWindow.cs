using BozjaBuddy.Data;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BozjaBuddy.Windows
{
    internal class CharStatsWindow : Window, IDisposable
    {
        public static string kHandle = "Character Stats - Bozja Buddy";

        private Plugin mPlugin;
        unsafe ImGuiTextFilterPtr mFilter_Cache = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
        unsafe ImGuiTextFilterPtr mFilter_CacheAlert1 = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
        unsafe ImGuiTextFilterPtr mFilter_CacheAlert2 = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

        public CharStatsWindow(Plugin plugin) : base("Character Stats - Bozja Buddy")
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 290),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
            this.SizeCondition = ImGuiCond.Once;

            this.mPlugin = plugin;
        }
        public void Dispose()
        {
            unsafe
            {
                ImGuiNative.ImGuiTextFilter_destroy(this.mFilter_Cache.NativePtr);
                ImGuiNative.ImGuiTextFilter_destroy(this.mFilter_CacheAlert1.NativePtr);
                ImGuiNative.ImGuiTextFilter_destroy(this.mFilter_CacheAlert2.NativePtr);
            }
        }

        public override void Draw()
        {
            this.Draw_CharStats();
        }

        public static void Draw_CharStatsCompact(Plugin pPlugin)
        {
            var tCharStats = pPlugin.Configuration.mGuiAssistConfig.charStats;

            if (tCharStats.noto != 0) tCharStats.isInit = true;
            if (!tCharStats.isInit)
            {
                ImGui.PushTextWrapPos();
                ImGui.TextColored(UtilsGUI.Colors.NormalText_Red, "<!> Stats not found! Requires character being in Bozja/Zadnor/Delubrum at least once.");
                ImGui.PopTextWrapPos();
            }

            ImGui.BeginGroup();
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Rank");
            ImGui.SameLine(); ImGui.Text($" \t\t{tCharStats.rank}");
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Noto");
            ImGui.SameLine(); ImGui.Text($" \t\t{tCharStats.noto}");
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 40);
            ImGui.BeginGroup();
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, $"Mettle ({tCharStats.proof} proof)");
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker("- 3 proofs of Honor exchanges for ~20 mil mettles.\n- Amount of proof required for a Ray stack: 1/1/2/2/3/3/4/4/5/5 (increase by 1 for every 2 stacks)\n- Recommended priority of ray: Valor (red) > Fortitude (blue) > Succor (green)\n\n- In total, maxing out a ray (10 stacks) costs ~200 mil mettles / 10 trade-ins. Maxing all costs ~600 mil mettles.\n- Max Succor does not negate Profane's 90% healing reduction.");
            ImGui.TextUnformatted($"{Utils.Utils.FormatThousand(tCharStats.mettle, pThreshold: 99999)} / {(tCharStats.mettleMax == 0 ? "25000k" : Utils.Utils.FormatThousand(tCharStats.mettleMax, pThreshold: 99999))} ({((float)tCharStats.mettle / (tCharStats.mettleMax == 0 ? 25000000 : tCharStats.mettleMax) * 100):0.00}%)");
            ImGui.EndGroup();

            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Cluster");
            ImGui.SameLine(); ImGui.Text($"\t{tCharStats.cluster} / 200");

            ImGui.Separator();

            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Fortitude");
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Blue);
            ImGui.SameLine(); ImGui.TextUnformatted($"{tCharStats.rayFortitude}\t\t\t\t+{5 * tCharStats.rayFortitude}% HP (ea. +5%)");
            ImGui.PopStyleColor();
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Valor");
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Red);
            ImGui.SameLine(); ImGui.TextUnformatted($"\t\t{tCharStats.rayValor}\t\t\t\t+{3 * tCharStats.rayFortitude}% DMG (ea. +3%)");
            ImGui.PopStyleColor();
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Succor");
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Green);
            ImGui.SameLine(); ImGui.TextUnformatted($" \t{tCharStats.raySuccor}\t\t\t\t+{10 * tCharStats.raySuccor}% HEAL (ea. +10%)");
            ImGui.PopStyleColor();
        }
        private void Draw_CharStats()
        {
            CharStatsWindow.Draw_CharStatsCompact(this.mPlugin);

            ImGui.Separator();

            UtilsGUI.TextDescriptionForWidget("Player's Cache:");
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker("Lost actions in your possession and their amount.\n\n- The number next to the Lost action is the amount.\n- Red text alerts you of an action's amount is running low under the threshold.\n- This threshold can be configured in Config > Misc > [A], or the button slider button on the right.");
            ImGui.SameLine(); AuxiliaryViewerSection.GUIAlignRight(32);
            UtilsGUI.WindowLinkedButton(mPlugin, "Config - BozjaBuddy", Dalamud.Interface.FontAwesomeIcon.Cog, "Open config window.");
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.SlidersH))
            {
                ImGui.OpenPopup("##alertConfig");
            }
            else { UtilsGUI.SetTooltipForLastItem("Configs for Action-Running-out Alert (also found in Config > Misc > [A])"); }
            if (ImGui.IsPopupOpen("##alertConfig")) ImGui.SetNextWindowSize(new Vector2(550, 450));
            if (ImGui.BeginPopup("##alertConfig"))
            {
                ConfigWindow.Draw_CacheAlertConfig(this.mPlugin, this.mPlugin.Configuration, this.mFilter_CacheAlert1, this.mFilter_CacheAlert2);
                ImGui.EndPopup();
            }

            ImGui.BeginChild("csw",
                new System.Numerics.Vector2(
                    ImGui.GetWindowWidth() - ImGui.GetStyle().FramePadding.X * 4,
                    ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - (ImGui.GetStyle().FramePadding.Y * 4)),
                true,
                ImGuiWindowFlags.NoScrollbar);
            this.mFilter_Cache.Draw("", ImGui.GetContentRegionAvail().X);
            ImGui.BeginChild("csb");
            if (this.mFilter_Cache.IsActive())
            {
                foreach (var iAction in this.mPlugin.mBBDataManager.mLostActions.Values)
                {
                    bool tIsMarked = false;

                    if (!this.mFilter_Cache.PassFilter(iAction.mName)) continue;

                    // Alert filtering
                    if (this.mPlugin.Configuration.mGuiAssistConfig.itemBox.userCacheData.TryGetValue(iAction.mId, out int tActionAmount)
                        && !(this.mPlugin.Configuration.mIsCacheAlertIgnoringActive
                             && this.mPlugin.Configuration.mCacheAlertIgnoreIds.Contains(iAction.mId)))
                    {
                        var tSpecThres = this.mPlugin.Configuration.GetCacheSpecificThresholds(iAction.mId);
                        // specific
                        if (this.mPlugin.Configuration.mIsCacheAlertSpecificActive
                            && tSpecThres >= tActionAmount)
                        {
                            tIsMarked = true;
                        }
                        // general
                        else if (this.mPlugin.Configuration.mIsCacheAlertGeneralActive
                            && this.mPlugin.Configuration.mCacheAlertGeneralThreshold >= tActionAmount)
                        {
                            tIsMarked = true;
                        }
                    }

                    UtilsGUI.SelectableLink_WithPopup(
                            this.mPlugin, 
                            iAction!.mName, 
                            iAction!.GetGenId(),
                            pColor: tIsMarked ? UtilsGUI.Colors.NormalText_Red : null,
                            pIsShowingCacheAmount: true
                        );
                }
            }
            else
            {
                HashSet<int> tMarkeds = new();

                // Alert filtering
                foreach (var u in this.mPlugin.Configuration.mGuiAssistConfig.itemBox.userCacheData)
                {
                    // ignore
                    if (this.mPlugin.Configuration.mIsCacheAlertIgnoringActive
                        && this.mPlugin.Configuration.mCacheAlertIgnoreIds.Contains(u.Key)) continue;
                    // specific
                    var tSpecThres = this.mPlugin.Configuration.GetCacheSpecificThresholds(u.Key);
                    if (this.mPlugin.Configuration.mIsCacheAlertSpecificActive
                        && tSpecThres >= u.Value)
                    {
                        if (!this.mPlugin.mBBDataManager.mLostActions.TryGetValue(u.Key, out LostAction? iAction)) continue;
                        tMarkeds.Add(iAction!.mId);
                    }
                    // general
                    else if (this.mPlugin.Configuration.mIsCacheAlertGeneralActive
                        && this.mPlugin.Configuration.mCacheAlertGeneralThreshold >= u.Value)
                    {
                        if (!this.mPlugin.mBBDataManager.mLostActions.TryGetValue(u.Key, out LostAction? iAction)) continue;
                        tMarkeds.Add(iAction!.mId);
                    }
                }

                // Drawing
                foreach (var iId in tMarkeds)
                {
                    if (!this.mPlugin.mBBDataManager.mLostActions.TryGetValue(iId, out LostAction? iAction)) continue;
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, iAction!.mName, iAction!.GetGenId(), pColor: UtilsGUI.Colors.NormalText_Red, pIsShowingCacheAmount: true);
                }
                foreach (var iAction in this.mPlugin.mBBDataManager.mLostActions.Values)
                {
                    if (tMarkeds.Contains(iAction!.mId)) continue;
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, iAction!.mName, iAction!.GetGenId(), pIsShowingCacheAmount: true);
                }
            }
            ImGui.EndChild();
            ImGui.EndChild();
        }
    }
}
