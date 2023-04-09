using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.Utils;
using BozjaBuddy.Utils.UtilsAudio;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
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

    private float mGuiButtonsPadding = 32 * 3;

    public ConfigWindow(Plugin plugin) : base(
        "Config - BozjaBuddy")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 250),
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
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
            {
                this.mPlugin.Configuration.mAudioPath = this.mPlugin.DATA_PATHS["alarm_audio"];
                this.mPlugin.Configuration.Save();
            }
            Utils.UtilsGUI.SetTooltipForLastItem("Restore to default.");
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
        if (ImGui.BeginTabItem("UI Hints"))
        {
            if (ImGui.CollapsingHeader("[A] Mettle & Resistance rank"))
            {
                // Reminder: Recruitment window
                {
                    bool tField1 = this.mPlugin.Configuration.mOptionState[GUIAssistOption.MycInfoBox];
                    ImGuiComponents.ToggleButton("rewin", ref tField1);
                    ImGui.SameLine();
                    ImGui.PushTextWrapPos();
                    UtilsGUI.TextDescriptionForWidget("[1] Reminder: Keep recruitment window open for CE-related features. Only display when said features are being used.");
                    ImGui.SameLine();
                    UtilsGUI.ShowHelpMarker("Features like CE status report in Fate/CE table and CE alarm needs the Resistance Recruitment in-game window open to work, due to lack of better means.\nThis is understandably very cumbersome for users, and will be worked on later. Any suggestion appreciated!");
                    ImGui.PopTextWrapPos();
                    this.mPlugin.Configuration.mOptionState[GUIAssistOption.MycInfoBox] = tField1;
                    this.mPlugin.Configuration.Save();
                }
            }

            ImGui.EndTabItem();
            return true;
        }
        return false;
    }
}
