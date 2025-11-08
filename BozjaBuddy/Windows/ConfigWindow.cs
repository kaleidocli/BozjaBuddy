using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.GUI.GUIAssist;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Interface;
using BozjaBuddy.Utils;
using BozjaBuddy.Utils.UtilsAudio;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Bindings.ImGui;
using ImGuiScene;
using NAudio.Wave;
using static BozjaBuddy.GUI.GUIAssist.GUIAssistManager;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.Windows;

public class ConfigWindow : Window, IDisposable
{
    private static readonly Dictionary<string, string> kErrors = new() {
        { "audio_path_err", "Unable to use this audio path." },
        { "audio_path_ok", "Audio path saved." }
    };

    private Configuration Configuration;
    private Plugin mPlugin;

    private string mFieldAudioPath = "";
    private HashSet<string> mErrors = new HashSet<string>();
    private AudioPlayer mTestAudioPlayer = new();
    private int mCurrActiveTab = 0;
    private int mLastActiveTab = 0;
    unsafe private Dictionary<string, ImGuiTextFilterPtr> mTextFilters = new();

    unsafe ImGuiTextFilterPtr mFilter_CacheAlert1 = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter(null));
    unsafe ImGuiTextFilterPtr mFilter_CacheAlert2 = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter(null));

    private float mGuiButtonsPadding = 32 * 3;

    public ConfigWindow(Plugin plugin) : base(
        "Config - BozjaBuddy")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(490, 250),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.SizeCondition = ImGuiCond.Once;

        this.Configuration = plugin.Configuration;
        this.mPlugin = plugin;
    }

    public void Dispose()
    {
        unsafe
        {
            this.mFilter_CacheAlert1.Destroy();
            this.mFilter_CacheAlert2.Destroy();
        }
    }

    public override void Draw()
    {
        ImGui.BeginTabBar("config");

        if (this.DrawTabAlarm()) { this.mCurrActiveTab = 0; }
        if (this.DrawTabUiHint()) { this.mCurrActiveTab = 1; }
        if (this.DrawTabMisc()) { this.mCurrActiveTab = 2; }

        // clear up kErrs when switching to another tab
        if (this.mCurrActiveTab != this.mLastActiveTab)
        {
            this.mErrors.Clear();
            this.mLastActiveTab = this.mCurrActiveTab;
        }

        ImGui.EndTabBar();

        //if (this.mPlugin.ClientState.LocalPlayer != null)
        //{
        //    PluginLog.LogDebug(String.Join(
        //        ", ",
        //        this.mPlugin.ClientState.LocalPlayer.StatusList.Select(s => s.StatusId)));
        //}
    }

    private bool DrawTabAlarm()
    {
        if (ImGui.BeginTabItem("Alarm"))
        {
            // Audio path
            UtilsGUI.GreyText("Audio path ");
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - this.mGuiButtonsPadding);
            ImGui.InputText("##path", ref this.mFieldAudioPath, 1000);
            ImGui.PopItemWidth();
            if (!ImGui.IsItemActive() && this.mFieldAudioPath == "")
            {
                this.mFieldAudioPath = this.mPlugin.Configuration.mAudioPath ?? "";
            }
            Utils.UtilsGUI.SetTooltipForLastItem(this.mFieldAudioPath);
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
            {
                this.mPlugin.Configuration.mAudioPath = this.mPlugin.DATA_PATHS["alarm_audio"];
                this.mPlugin.Configuration.Save();
            }
            Utils.UtilsGUI.SetTooltipForLastItem("Restore to default. (song: Epic Sax Guy)");
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save))
            {
                try
                {
                    var tAudioReader = new LoopStream(new MediaFoundationReader(this.mFieldAudioPath));
                    this.mPlugin.Configuration.mAudioPath = this.mFieldAudioPath;
                    this.mPlugin.Configuration.Save();
                    if (!this.mErrors.Contains("audio_path_ok"))
                    {
                        this.mErrors.Remove("audio_path_err");
                        this.mErrors.Add("audio_path_ok");
                    }
                }
                catch (Exception)
                {
                    if (!this.mErrors.Contains("audio_path_err"))
                    {
                        this.mErrors.Remove("audio_path_ok");
                        this.mErrors.Add("audio_path_err");
                    }
                }
            }
            Utils.UtilsGUI.SetTooltipForLastItem("Save the audio path.\nThe audio path setting only applies after pressing save.\nClear the audio path to restore to the previous one.");
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Play))
            {
                this.mTestAudioPlayer.StartAudio(
                    this.mPlugin.Configuration.mAudioPath ?? "", 
                    this.mPlugin.Configuration.mAudioVolume, 
                    pIsInterrupting: true, 
                    pIsLooping: false);
            }
            Utils.UtilsGUI.SetTooltipForLastItem("Test the audio.");
            // Volume slider
            UtilsGUI.GreyText("Audio volume ");
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - this.mGuiButtonsPadding);
            if (ImGui.SliderFloat("##vol", ref this.mPlugin.Configuration.mAudioVolume, 0.0f, 3.0f))
            {
                this.mPlugin.Configuration.Save();
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("##vol", Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
            {
                this.mPlugin.Configuration.mAudioVolume = Configuration.kDefaultVolume;
                this.mPlugin.Configuration.Save();
            }
            Utils.UtilsGUI.SetTooltipForLastItem($"Restore to default. ({Configuration.kDefaultVolume})");
            // Duration slider
            UtilsGUI.GreyText("Default duration ");
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - this.mGuiButtonsPadding);
            if (ImGui.SliderInt("##dura", ref this.mPlugin.Configuration.mDefaultAlarmDuration, Alarm.kDurationMin, Alarm.kDurationMax))
            {
                this.mPlugin.Configuration.Save();
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("##dura", Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
            {
                this.mPlugin.Configuration.mDefaultAlarmDuration = Configuration.kDefaultAlarmDuration;
                this.mPlugin.Configuration.Save();
            }
            Utils.UtilsGUI.SetTooltipForLastItem($"Restore to default. ({Configuration.kDefaultAlarmDuration}s)");
            // Offset slider
            UtilsGUI.GreyText("Default offset ");
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - this.mGuiButtonsPadding);
            if (ImGui.SliderInt("##off", ref this.mPlugin.Configuration.mDefaultAlarmOffset, Alarm.kOffsetMin, Alarm.kOffsetMax))
            {
                this.mPlugin.Configuration.Save();
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("##off", Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
            {
                this.mPlugin.Configuration.mDefaultAlarmOffset = Configuration.kDefaultAlarmOffset;
                this.mPlugin.Configuration.Save();
            }
            Utils.UtilsGUI.SetTooltipForLastItem($"Restore to default. ({Configuration.kDefaultAlarmOffset}s)");
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker($"Offset to alarm.\ne.g. An alarm for Raining will trigger X seconds before it actually rain. X is its offset.\nRange from {Alarm.kOffsetMin} to {Alarm.kOffsetMax}.");

            // CheckBox: MuteOnGameFocused
            if (UtilsGUI.Checkbox("Mute alarm sound when game window is focused.", ref this.mPlugin.Configuration.mMuteAAudioOnGameFocused))
            {
                this.mPlugin.Configuration.Save();
            }

            // Errors
            foreach (string tErrKey in this.mErrors)
            {
                ImGui.Text($"<!> {ConfigWindow.kErrors[tErrKey]}");
            }

            ImGui.EndTabItem();
            return true;
        }
        return false;
    }

    private bool DrawTabUiHint()
    {
        float tInnerSpacing = 12;
        if (ImGui.BeginTabItem("UI Assist"))
        {
            if (ImGui.CollapsingHeader("[A] Mettle & Resistance Rank (M&R) window"))
            {
                // All
                bool tField2 = !this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_All;
                if (ImGuiComponents.ToggleButton("allwin", ref tField2)) this.mPlugin.Configuration.Save();
                this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_All = !tField2;
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
                ImGui.PushTextWrapPos();
                UtilsGUI.GreyText("1. All UI features for this section.");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Enable/Disable all UI features for this section such as Search all bar, Alarm reminder, etc.");
                ImGui.PopTextWrapPos();
                // 1.1 - is disabled when focused
                bool tField3 = this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_WhenNotFocused;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 55f);
                if (ImGuiComponents.ToggleButton("allwin", ref tField3)) this.mPlugin.Configuration.Save();
                this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_WhenNotFocused = tField3;
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
                ImGui.PushTextWrapPos();
                UtilsGUI.GreyText("1.1 Only click-able if the M&R window is focused.");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Search all bar and buttons overlay on top of M&R window can be a nuisance when accidentally clicked. This option will make those widgets only click-able when the M&R window is focused, otherwise it'll be greyed out and untargetable.");
                ImGui.PopTextWrapPos();

                // Reminder: Recruitment window
                {
                    bool tField1 = this.mPlugin.Configuration.mOptionState[GUIAssistOption.MycInfoBoxAlarm];
                    if (ImGuiComponents.ToggleButton("rewin", ref tField1)) this.mPlugin.Configuration.Save();
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
                    ImGui.PushTextWrapPos();
                    UtilsGUI.GreyText("2. Draw a rectangle as a reminder to keep Recruting window open.");
                    ImGui.SameLine();
                    UtilsGUI.ShowHelpMarker("Only visible when Fate/CE features are active, or when user is not in any CE or raids.\nFeatures like CE status report in Fate/CE table and CE alarm needs the Resistance Recruitment in-game window open to work, due to lack of better means.\nThis is understandably cumbersome for users, and will be worked on later. Any suggestion appreciated!");
                    ImGui.PopTextWrapPos();
                    this.mPlugin.Configuration.mOptionState[GUIAssistOption.MycInfoBoxAlarm] = tField1;
                }
            }
            if (ImGui.CollapsingHeader("[B] Lost Find Cache window & Lost Find Hoslter window"))
            {
                // Loadout filter configs
                ConfigWindow.DrawTabUiHint_LostFindCacheSection(this.mPlugin, pFixedSize: 400);
            }

            ImGui.EndTabItem();
            return true;
        }
        return false;
    }
    private bool DrawTabMisc()
    {
        if (ImGui.BeginTabItem("Misc"))
        {
            if (ImGui.CollapsingHeader("[A] General configs"))
            {
                ConfigWindow.Draw_GeneralConfig(this.mPlugin, this.mPlugin.Configuration);
            }
            if (ImGui.CollapsingHeader("[B] Action-Running-out Alert (in Character Stats window)"))
                ConfigWindow.Draw_CacheAlertConfig(this.mPlugin, this.mPlugin.Configuration, this.mFilter_CacheAlert1, this.mFilter_CacheAlert2);
            ImGui.EndTabItem();
            return true;
        }
        return false;
    }
    public static void Draw_GeneralConfig(Plugin pPlugin, Configuration pConfig)
    {
        if (ImGuiComponents.ToggleButton("##tglbInfoViewer", ref pConfig.mIsAuxiUsingNGV))
        {
            pConfig.Save();
        }
        ImGui.SameLine();
        UtilsGUI.GreyText($"1. Info viewer type: {(pConfig.mIsAuxiUsingNGV ? "Node graph" : "Tab items")}");
        ImGui.SameLine();
        UtilsGUI.ShowHelpMarker(
            """
            Switch between info viewer modes.
            - Tab items: pages of info organized into tabs. Simplest and quickest way to view info.
            - Node graph: movable nodes containing info. Provides better coherence and allows to view multiple topics at once.
            """
            );
    }
    public static void Draw_CacheAlertConfig(Plugin pPlugin, Configuration pConfig, ImGuiTextFilterPtr pFilter_CacheAlert1, ImGuiTextFilterPtr pFilter_CacheAlert2)
    {
        // General alert
        if (ImGuiComponents.ToggleButton("##ag", ref pConfig.mIsCacheAlertGeneralActive))
        {
            pConfig.Save();
        }
        ImGui.SameLine(); UtilsGUI.GreyText("[A] Alert for all actions");
        ImGui.SameLine(); UtilsGUI.ShowHelpMarker("This option [A] applies to all actions. Can be overwritten by [B] and [C].");
        UtilsGUI.GreyText("\t\t\t  Threshold: ");
        ImGui.SameLine(); ImGui.SetNextItemWidth(150);
        ImGui.InputInt("##agi", ref pPlugin.Configuration.mCacheAlertGeneralThreshold, 5);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            pConfig.Save();
        }

        // Specific alert
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
        ImGui.Spacing();
        ImGui.BeginChild("as",
            new System.Numerics.Vector2(
                ImGui.GetWindowWidth() / 10 * 6 - ImGui.GetStyle().FramePadding.X,
                ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - (ImGui.GetStyle().FramePadding.Y * 4)),
            true,
            ImGuiWindowFlags.NoScrollbar);
        if (ImGuiComponents.ToggleButton("##as", ref pConfig.mIsCacheAlertSpecificActive))
        {
            pConfig.Save();
        }
        ImGui.SameLine(); UtilsGUI.GreyText("[B] Alert for specific actions");
        ImGui.SameLine(); UtilsGUI.ShowHelpMarker("This option [B] applies to specific actions.\n\n- If set, the threshold of this option [B] will be used instead of [A]'s.\n- If set to zero (0), this option will be disabled for selected action.");
        ImGui.Spacing();
        pFilter_CacheAlert1.Draw("", ImGui.GetContentRegionAvail().X);
        ImGui.BeginChild("asb");
        foreach (var i in pConfig.mGuiAssistConfig.itemBox.userCacheData)
        {
            if (!pPlugin.mBBDataManager.mLostActions.TryGetValue(i.Key, out var iAction)) continue;
            if (!pFilter_CacheAlert1.PassFilter(iAction.mName)) continue;
            ImGui.Text(iAction.mName);
            var tThreshold = pConfig.GetCacheSpecificThresholds(i.Key);
            ImGui.SameLine(); ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputInt($"##{iAction.mId}", ref tThreshold, 5);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                pConfig.SetCacheSpecificThresholds(i.Key, tThreshold);
                pConfig.Save();
            }
        }
        ImGui.EndChild();
        ImGui.EndChild();
        ImGui.PopStyleVar();

        // Ignoring alert
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
        ImGui.SameLine();
        ImGui.BeginChild("ai",
            new System.Numerics.Vector2(
                ImGui.GetWindowWidth() / 10 * 4 - ImGui.GetStyle().FramePadding.X - ImGui.GetStyle().ItemSpacing.X * 2,
                ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - (ImGui.GetStyle().FramePadding.Y * 4)),
            true,
            ImGuiWindowFlags.NoScrollbar);
        if (ImGuiComponents.ToggleButton("##ai", ref pConfig.mIsCacheAlertIgnoringActive))
        {
            pConfig.Save();
        }
        ImGui.SameLine(); UtilsGUI.GreyText("[C] Actions to ignore");
        ImGui.SameLine(); UtilsGUI.ShowHelpMarker("This option [C] will disable option [A] & [B] for selected actions.");
        ImGui.Spacing();
        pFilter_CacheAlert2.Draw("", ImGui.GetContentRegionAvail().X);
        ImGui.BeginChild("aib");
        if (pFilter_CacheAlert2.IsActive())
        {
            foreach (LostAction iAction in pPlugin.mBBDataManager.mLostActions.Values)
            {
                if (!pFilter_CacheAlert2.PassFilter(iAction.mName)) continue;

                if (pConfig.mCacheAlertIgnoreIds.Contains(iAction.mId))
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.MycItemBoxOverlay_RedDarkBright));
                    if (ImGui.Button($" X ##{iAction.mId}"))
                    {
                        pConfig.mCacheAlertIgnoreIds.Remove(iAction.mId);
                        pConfig.Save();
                    }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.GenObj_GreenMob));
                    ImGui.PushID(iAction.mId);
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus))
                    {
                        pConfig.mCacheAlertIgnoreIds.Add(iAction.mId);
                        pConfig.Save();
                    }
                    ImGui.PopID();
                }
                ImGui.PopStyleColor();
                ImGui.SameLine(); ImGui.Text(iAction.mName);
            }
        }
        else
        {
            List<int> tGarbo = new();
            foreach (int iId in pConfig.mCacheAlertIgnoreIds)
            {
                pPlugin.mBBDataManager.mLostActions.TryGetValue(iId, out var iAction);
                if (iAction == null)
                {
                    tGarbo.Add(iId);
                    continue;
                }
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.MycItemBoxOverlay_RedDarkBright));
                if (ImGui.Button($" X ##{iAction.mId}"))
                {
                    tGarbo.Add(iAction.mId);
                }
                ImGui.SameLine(); ImGui.Text(iAction.mName);
                ImGui.PopStyleColor();
            }
            foreach (var id in tGarbo) pConfig.mCacheAlertIgnoreIds.Remove(id);
            if (tGarbo.Count != 0) { pConfig.Save(); }
        }
        ImGui.EndChild();
        ImGui.EndChild();
        ImGui.PopStyleVar();
    }
    /// <summary>
    /// Use pFixedSize when encounter weird bugs with GetContentRegionAvail()
    /// </summary>
    public static void DrawTabUiHint_LostFindCacheSection(Plugin pPlugin, float? pFixedSize = null, float pGuiButtonsPadding = 32 * 3)
    {
        float tInnerSpacing = 12;

        // ALL
        bool tTemp = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All;
        if (ImGuiComponents.ToggleButton("tg_1", ref tTemp))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All = !tTemp;
            if (!pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_All)
            {
                pPlugin.GUIAssistManager.RequestRestore(GUIAssistOption.MycItemBoxRoleFilter);
            }
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("1. Most UI features for this section.");

        // Toolbar
        bool tTemp2 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar;
        if (ImGuiComponents.ToggleButton("tg_2", ref tTemp2))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar = !tTemp2;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("2. Toolbar.");

        // Loadout Miniview
        bool tTemp3 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_LoadoutMiniview;
        if (ImGuiComponents.ToggleButton("tg_3", ref tTemp3))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_LoadoutMiniview = !tTemp3;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("3. Loadout miniview.");

        // Text filter
        bool tTemp4 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterText;
        if (ImGuiComponents.ToggleButton("tg_4", ref tTemp4))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterText = !tTemp4;
            if (!pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterText)
            {
                pPlugin.GUIAssistManager.RequestRestore(GUIAssistOption.MycItemBoxRoleFilter);
            }
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("4. Text filters ");
        ImGui.SameLine();
        ImGui.PushItemWidth((pFixedSize == null ? ImGui.GetContentRegionAvail().X : (pFixedSize!.Value - ImGui.GetCursorPosX())));
        if (ImGui.SliderInt("##ibfl", ref pPlugin.Configuration.mGuiAssistConfig.itemBox.filterTextLevel, 0, 2))
        {
            pPlugin.Configuration.Save();
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.ShowHelpMarker("0: Disabled.\n1: Filtered-out actions are darkened.\n2: Filtered-out actions are hidden.");

        // Loadout filter
        bool tTemp5 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterLoadout;
        if (ImGuiComponents.ToggleButton("tg_5", ref tTemp5))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterLoadout = !tTemp5;
            if (!pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_FilterLoadout)
            {
                pPlugin.GUIAssistManager.RequestRestore(GUIAssistOption.MycItemBoxRoleFilter);
            }
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("5. Loadout filters ");
        ImGui.SameLine();
        ImGui.PushItemWidth((pFixedSize == null ? ImGui.GetContentRegionAvail().X : (pFixedSize!.Value - ImGui.GetCursorPosX())));
        if (ImGui.SliderInt("##ibll", ref pPlugin.Configuration.mGuiAssistConfig.itemBox.filterLoadoutLevel, 0, 2))
        {
            pPlugin.Configuration.Save();
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.ShowHelpMarker("0: Disabled.\n1: Actions in the loadout are highlighted.\n2: Actions in the loadout are highlighted. Otherwise disabled.");

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + tInnerSpacing);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + tInnerSpacing);

        // Refresh rate
        UtilsGUI.GreyText("6. Refresh rate ");
        ImGui.SameLine();
        ImGui.PushItemWidth((pFixedSize == null ? ImGui.GetContentRegionAvail().X : (pFixedSize!.Value - ImGui.GetCursorPosX() - pGuiButtonsPadding)));
        if (ImGui.SliderFloat("##ibrf", ref pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRate, 0.1f, 1))
        {
            pPlugin.Configuration.Save();
        }
        ImGui.PopItemWidth();
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        if (ImGuiComponents.IconButton("##ibrf_bar", Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRate = pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRateDefault;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.ShowHelpMarker($"Refresh rate is the rate at which the UI is updated.\nLower value yields better user experience at the cost of potential game stuttering. And vice versa.\n The default value ({pPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRateDefault}) is recommended.");

        // Auto-role Filter
        bool tTemp7 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_AutoRoleFilter;
        if (ImGuiComponents.ToggleButton("tg_7", ref tTemp7))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_AutoRoleFilter = !tTemp7;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.GreyText("7. Auto Role-filter.");
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.ShowHelpMarker($"Automatically apply your current role to the Lost Find Cahe every time you open it.\nDoes not clear the filter when disabled.");
    }
    public static void Draw_LoadoutPairingButton(Plugin pPlugin, Dictionary<string, ImGuiTextFilterPtr> pGuiVar_TextFilters)
    {
        // header
        ImGui.Text("JOB\t\t\t\tLoadout I\t\t\t\t\t\t\t\tLoadout II");
        ImGui.Separator();

        ImGui.Text("Any");
        ImGui.SameLine();
        ImGui.PushItemWidth(300 / 2);
        LoadoutTableSection.DrawOverlayCombo(pPlugin, pGuiVar_TextFilters, Job.ALL, "aljob_pu.any");
        ImGui.SameLine();
        LoadoutTableSection.DrawOverlayCombo(pPlugin, pGuiVar_TextFilters, Job.ALL, "aljob_pu.any", pOverlaySlot: 1);
        ImGui.PopItemWidth();
        foreach (Role iRole in new[] { Role.Tank, Role.Healer, Role.Caster, Role.Melee, Role.Range })
        {
            foreach (Job iJob in UtilsGameData.kJobToRole[iRole])
            {
                if (!UtilsGameData.kValidJobs.Contains(iJob)) continue;
                IDalamudTextureWrap? tJobIcon = UtilsGameData.GetJobIcon(iJob);
                // Job icon
                if (tJobIcon != null) { ImGui.Image(tJobIcon.Handle, Utils.Utils.ResizeToIcon(pPlugin, tJobIcon)); }
                else { ImGui.Text(iJob.ToString()); }   // Job abbv
                                                        // Combo
                ImGui.SameLine();
                ImGui.PushItemWidth(300 / 2);
                LoadoutTableSection.DrawOverlayCombo(pPlugin, pGuiVar_TextFilters, iJob, $"aljob_pu.{iJob}");
                ImGui.SameLine();
                LoadoutTableSection.DrawOverlayCombo(pPlugin, pGuiVar_TextFilters, iJob, $"aljob_pu.{iJob}", pOverlaySlot: 1);
                ImGui.PopItemWidth();
            }
        }
    }
}
