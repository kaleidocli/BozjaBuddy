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
using ImGuiNET;
using ImGuiScene;
using NAudio.Wave;
using static BozjaBuddy.GUI.GUIAssist.GUIAssistManager;

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

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.BeginTabBar("config");

        if (this.DrawTabAlarm()) { this.mCurrActiveTab = 0; }
        if (this.DrawTabUiHint()) { this.mCurrActiveTab = 1; }

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
            UtilsGUI.TextDescriptionForWidget("Audio path ");
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
                catch (Exception e)
                {
                    if (!this.mErrors.Contains("audio_path_err"))
                    {
                        this.mErrors.Remove("audio_path_ok");
                        this.mErrors.Add("audio_path_err");
                    }
                    PluginLog.LogError($"Path might be invalid: {this.mFieldAudioPath}\n{e.Message}");
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
            UtilsGUI.TextDescriptionForWidget("Audio volume ");
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
            UtilsGUI.TextDescriptionForWidget("Default duration ");
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
            UtilsGUI.TextDescriptionForWidget("Default offset ");
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
            if (ImGui.CollapsingHeader("[A] Mettle & Resistance Rank window"))
            {
                // Reminder: Recruitment window
                {
                    bool tField1 = this.mPlugin.Configuration.mOptionState[GUIAssistOption.MycInfoBox];
                    ImGuiComponents.ToggleButton("rewin", ref tField1);
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
                    ImGui.PushTextWrapPos();
                    UtilsGUI.TextDescriptionForWidget("1. Draw a rectangle as a reminder to keep Recruting window open.");
                    ImGui.SameLine();
                    UtilsGUI.ShowHelpMarker("Only visible when Fate/CE features are active, or when user is not in any CE or raids.\nFeatures like CE status report in Fate/CE table and CE alarm needs the Resistance Recruitment in-game window open to work, due to lack of better means.\nThis is understandably cumbersome for users, and will be worked on later. Any suggestion appreciated!");
                    ImGui.PopTextWrapPos();
                    this.mPlugin.Configuration.mOptionState[GUIAssistOption.MycInfoBox] = tField1;
                    this.mPlugin.Configuration.Save();
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
        UtilsGUI.TextDescriptionForWidget("1. Most UI features for this section.");

        // Toolbar
        bool tTemp2 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar;
        if (ImGuiComponents.ToggleButton("tg_2", ref tTemp2))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar = !tTemp2;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.TextDescriptionForWidget("2. Toolbar.");

        // Loadout Miniview
        bool tTemp3 = !pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_LoadoutMiniview;
        if (ImGuiComponents.ToggleButton("tg_3", ref tTemp3))
        {
            pPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_LoadoutMiniview = !tTemp3;
            pPlugin.Configuration.Save();
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + tInnerSpacing);
        UtilsGUI.TextDescriptionForWidget("3. Loadout miniview.");

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
        UtilsGUI.TextDescriptionForWidget("4. Text filters ");
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
        UtilsGUI.TextDescriptionForWidget("5. Loadout filters ");
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
        UtilsGUI.TextDescriptionForWidget("6. Refresh rate ");
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
        UtilsGUI.ShowHelpMarker("Refresh rate is the rate at which the UI is updated.\nLower value yields better user experience at the cost of potential game stuttering. And vice versa.\n The default value (0.6) is recommended.");
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
                TextureWrap? tJobIcon = UtilsGameData.GetJobIcon(iJob);
                // Job icon
                if (tJobIcon != null) { ImGui.Image(tJobIcon.ImGuiHandle, Utils.Utils.ResizeToIcon(pPlugin, tJobIcon)); }
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
