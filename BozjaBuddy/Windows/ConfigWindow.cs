using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.Utils.UtilsAudio;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using NAudio.Wave;
using Newtonsoft.Json.Linq;

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

        // Alarm config
        this.DrawTabAlarm();

        ImGui.EndTabBar();
    }

    private void DrawTabAlarm()
    {
        if (ImGui.BeginTabItem("Alarm"))
        {
            // Audio path
            ImGui.TextColored(BozjaBuddy.Utils.UtilsGUI.Colors.BackgroundText_Grey, "Audio path ");
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
                    return;
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
            ImGui.TextColored(BozjaBuddy.Utils.UtilsGUI.Colors.BackgroundText_Grey, "Audio volume ");
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
        }
        else
        {
            this.mErrors.Clear();
        }
    }
}
