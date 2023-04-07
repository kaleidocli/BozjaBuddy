using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Xml.Serialization;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.GUI;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;

namespace BozjaBuddy.Windows
{
    internal class AlarmWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private bool mIsCollapseHeaderTopActive = false;
        private bool mIsCollapseHeaderBottomActive = false;

        public AlarmWindow(Plugin plugin) : base("Alarm - BozjaBuddy", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(211, 220),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
            this.Plugin = plugin;
        }

        public override void Draw()
        {
            // Menu bar
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog))
            {
                this.Plugin.WindowSystem.GetWindow("Config - BozjaBuddy")!.IsOpen = true;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Open config window.");
                ImGui.EndTooltip();
            }
            ImGui.SameLine(); AlarmWindow.DrawAlarmNotificationBar(this.Plugin, "alarmWindow", pPadding: (float)3);
            ImGui.SameLine(); AuxiliaryViewerSection.GUIAlignRight(1);
            ImGui.SameLine();
            string tTempGUI_Key = "alarmWin";
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus))
            {
                GUIAlarm.CreateACPU(tTempGUI_Key, pNameSuggestion: Alarm.kReprString);
            }
            UtilsGUI.SetTooltipForLastItem($"Add an alarm.\r\n+ Time-based: alarms which trigger at a specific time. Can only be created in Alarm window.\r\n+ Weather-based: alarms which trigger at a specific weather at a specific time (ONCE), or every time the weather occurs (REPEAT). Can be created in Alarm window, or clicking on Weather bar.\r\n+ FATE-based: alarms which trigger every time a FATE occurs (CEs not yet supported). Can be created in Alarm window, or click on Alarm column in Fate/CE table.\r\n- Alarm can be turned off, edited, deleted, or recycled once expire.");
            GUIAlarm.DrawACPU_All(
                            this.Plugin,
                            tTempGUI_Key);

            // Alarm List: Alive
            if (ImGui.CollapsingHeader($"Alarms ({this.Plugin.AlarmManager.GetAlarms().Count})", ImGuiTreeNodeFlags.DefaultOpen))
            {
                this.mIsCollapseHeaderTopActive = true;
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
                ImGui.BeginChild("alarmList_alive",
                    new System.Numerics.Vector2(
                        ImGui.GetWindowWidth() - ImGui.GetStyle().FramePadding.X * 4, 
                        ImGui.GetWindowHeight() / (this.mIsCollapseHeaderBottomActive ? 2 : 1) - ImGui.GetCursorPosY() - (this.mIsCollapseHeaderBottomActive ? 0 : ImGui.GetStyle().FramePadding.Y * 11)),
                    true);

                List<Alarm> tAlarmsToRemove = new();
                foreach (Alarm iAlarm in this.Plugin.AlarmManager.GetAlarms())
                {
                    // BUTTON
                    ImGui.PushID(iAlarm.mId);
                    ImGui.BeginGroup();
                    // power
                    ImGui.PushStyleColor(ImGuiCol.Text, iAlarm.mIsAwake
                                                        ? ImGuiColors.DalamudWhite2
                                                        : Utils.Utils.RGBAtoVec4(0, 0, 0, 100));
                    if (ImGuiComponents.IconButton(
                            Dalamud.Interface.FontAwesomeIcon.PowerOff, 
                            defaultColor: Utils.Utils.RGBAtoVec4(165, 165, 165, 80)
                            ))
                    {
                        if (iAlarm.mIsAwake)
                        {
                            this.Plugin.AlarmManager.SleepAlarm(iAlarm);
                        }
                        else
                        {
                            this.Plugin.AlarmManager.WakeAlarm(iAlarm);
                        }
                    }
                    UtilsGUI.SetTooltipForLastItem("Turn alarm on/off. Turned-off alarms will not be triggered until they're on again.");
                    ImGui.PopStyleColor();
                    // edit
                    string tTempGUI_Key2 = "aEdit";
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.PenSquare))
                    {
                        GUIAlarm.CreateACPU(tTempGUI_Key2, iAlarm);
                    }
                    switch (iAlarm)
                    {
                        case AlarmTime:
                            GUIAlarm.DrawACPU_Time(
                                this.Plugin, 
                                tTempGUI_Key2, 
                                pTriggerTime: iAlarm.mTriggerTime,
                                pAlarmToEdit: iAlarm);
                            break;
                        case AlarmWeather:
                            GUIAlarm.DrawACPU_Weather(
                                this.Plugin,
                                tTempGUI_Key2,
                                iAlarm.mTriggerInt ?? 0,
                                iAlarm.mTriggerString,
                                iAlarm.mTriggerTime,
                                pAlarmToEdit: iAlarm);
                            break;
                        case AlarmFateCe:
                            GUIAlarm.DrawACPU_FateCe(
                                this.Plugin,
                                tTempGUI_Key2,
                                iAlarm.mTriggerInt ?? 0,
                                pAlarmToEdit: iAlarm);
                            break;
                    }
                    // delete
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash))
                    {
                        tAlarmsToRemove.Add(iAlarm);
                    }
                    UtilsGUI.SetTooltipForLastItem("Permanently delete alarm. (NOT moving to expired list)");
                    // location
                    if (iAlarm.GetType() == typeof(AlarmFateCe)
                        && iAlarm.mTriggerInt.HasValue
                        && this.Plugin.mBBDataManager.mFates[iAlarm.mTriggerInt!.Value].mLocation != null)
                    {
                        AuxiliaryViewerSection.GUIButtonLocation(
                            this.Plugin,
                            this.Plugin.mBBDataManager.mFates[iAlarm.mTriggerInt!.Value].mLocation!,
                            pUseIcon: true
                            );
                    }
                    ImGui.EndGroup();
                    ImGui.PopID();

                    ImGui.SameLine();

                    // TEXT
                    ImGui.BeginGroup();
                    this.DrawAlarmInfo(iAlarm);
                    ImGui.EndGroup();

                    ImGui.Separator();
                }
                foreach (Alarm iAlarm in tAlarmsToRemove)
                {
                    this.Plugin.AlarmManager.RemoveAlarm(iAlarm);
                }

                ImGui.EndChild();
                ImGui.PopStyleVar();
            }
            else
            {
                this.mIsCollapseHeaderTopActive = false;
            }

            // Alarm List: Disposed
            if (ImGui.CollapsingHeader($"Expired Alarms ({this.Plugin.AlarmManager.GetDisposedAlarms().Count})"))
            {
                this.mIsCollapseHeaderBottomActive = true;
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
                ImGui.BeginChild("alarmList_disposed",
                    new System.Numerics.Vector2(
                        ImGui.GetWindowWidth() - ImGui.GetStyle().FramePadding.X * 4,
                        ImGui.GetWindowHeight() / (this.mIsCollapseHeaderTopActive ? 2 : 1) - ImGui.GetCursorPosY() - (this.mIsCollapseHeaderTopActive ? 0 : ImGui.GetStyle().FramePadding.Y * 11)),
                    true);

                List<Alarm> tAlarmsToRemove = new();
                List<Alarm> tAlarmsToUnexpire = new();
                foreach (Alarm iAlarm in this.Plugin.AlarmManager.GetDisposedAlarms())
                {
                    // BUTTON
                    ImGui.PushID(iAlarm.mId);
                    ImGui.BeginGroup();

                    if (iAlarm.GetType() == typeof(AlarmTime)) ImGui.BeginDisabled();
                    // recycle
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Recycle))
                    {
                        tAlarmsToUnexpire.Add(iAlarm);
                    }
                    if (iAlarm.GetType() == typeof(AlarmTime)) ImGui.EndDisabled();
                    UtilsGUI.SetTooltipForLastItem("Un-expired this alarm with the same setting. (not applicable for Time Alarm)");
                    // trash
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash))
                    {
                        tAlarmsToRemove.Add(iAlarm);
                    }
                    UtilsGUI.SetTooltipForLastItem("Permanently delete alarm.");
                    // location
                    if (iAlarm.GetType() == typeof(AlarmFateCe) 
                        && iAlarm.mTriggerInt.HasValue
                        && this.Plugin.mBBDataManager.mFates[iAlarm.mTriggerInt!.Value].mLocation != null)
                    {
                        AuxiliaryViewerSection.GUIButtonLocation(
                            this.Plugin,
                            this.Plugin.mBBDataManager.mFates[iAlarm.mTriggerInt!.Value].mLocation!,
                            pUseIcon: true
                            );
                    }
                    ImGui.EndGroup();
                    ImGui.PopID();

                    ImGui.SameLine();

                    // TEXT
                    ImGui.BeginGroup();
                    this.DrawAlarmInfo(iAlarm);
                    ImGui.EndGroup();

                    ImGui.Separator();
                }
                foreach (Alarm iAlarm in tAlarmsToRemove)
                {
                    this.Plugin.AlarmManager.RemoveAlarm(iAlarm);
                }
                foreach (Alarm iAlarm in tAlarmsToUnexpire)
                {
                    this.Plugin.AlarmManager.UnexpireAlarm(iAlarm);
                }

                ImGui.EndChild();
                ImGui.PopStyleVar();
            }
            else
            {
                this.mIsCollapseHeaderBottomActive = false;
            }
        }
        private void DrawAlarmInfo(Alarm pAlarm, bool pIsExpired = false)
        {
            ImGui.PushTextWrapPos();
            // Name
            if (pIsExpired)
            {
                ImGui.Text(pAlarm.mName);
            }
            else
            {
                ImGui.TextColored(!pAlarm.mIsAlive
                        ? UtilsGUI.Colors.ActivatedText_Green
                        : UtilsGUI.Colors.NormalText_White,
                   pAlarm.mName);
            }
            // Desc
            try
            {
                ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, String.Format("{0}",
                                                            pAlarm._mJsonId switch
                                                            {
                                                                AlarmTime._kJsonid => $"Time",
                                                                AlarmWeather._kJsonid => $"Weather | {WeatherBarSection._mTerritories[pAlarm.mTriggerString!]} | {WeatherBarSection._mWeatherNames[pAlarm.mTriggerInt!.Value]}",
                                                                AlarmFateCe._kJsonid => $"Fate/Ce | {this.Plugin.mBBDataManager.mFates[pAlarm.mTriggerInt!.Value].mName}",
                                                                _ => "unknown"
                                                            }));
                ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, String.Format("{0}{1}{2}",
                                                            pAlarm.mIsRevivable ? "Repeat" : "Once",
                                                            $" | ±{pAlarm.mOffset}s",
                                                            !pAlarm.mIsRevivable && pAlarm.mTriggerTime.HasValue
                                                                ? $" | {pAlarm.mTriggerTime}"
                                                                : ""
                                                            ));
            }
            catch (KeyNotFoundException)
            {
                ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, $"Corrupted alarm data. (trgStr={pAlarm.mTriggerString} trgInt={pAlarm.mTriggerInt})");
            }
            ImGui.PopTextWrapPos();
        }
        public static void DrawAlarmNotificationBar(Plugin pPlugin, string pGUIId, bool pIsStretching = true, float pWidth = 300, float pPadding = 1)
        {
            ImGui.PushID(pGUIId);
            ImGui.PushStyleColor(
                ImGuiCol.Button,
                pPlugin.AlarmManager.GetDurationLeft() > 0
                    ? UtilsGUI.Colors.ActivatedBar_Green
                    : UtilsGUI.Colors.NormalBar_Grey
                );
            if (ImGui.Button(
                pPlugin.AlarmManager.GetDurationLeft() > 0
                    ? pPlugin.AlarmManager.GetMuteStatus()
                        ? $"Alarm! Sound muted. ({pPlugin.AlarmManager.GetDurationLeft()}s)"
                        : $"Alarm! Click to mute. ({pPlugin.AlarmManager.GetDurationLeft()}s)"
                    : pPlugin.AlarmManager.GetAAAAlarmCount() > 0
                        ? $"{pPlugin.AlarmManager.GetAAAAlarmCount()} alarms running..."
                        : "---------",
                new Vector2(
                    (pIsStretching
                            ? ImGui.GetWindowWidth()
                            : pWidth)
                        - ImGui.GetStyle().WindowPadding.X * 2 - (ImGui.GetFontSize() * pPadding + ImGui.GetStyle().FramePadding.X * 2) - ImGui.GetStyle().ItemInnerSpacing.X,
                    ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 - ImGui.GetStyle().ItemInnerSpacing.Y / 2))
                && pPlugin.AlarmManager.GetTriggerStatus())
            {
                pPlugin.AlarmManager.MuteSound();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(200);
                ImGui.TextUnformatted("Alarm notification bar.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
            ImGui.PopStyleColor();
            ImGui.PopID();
        }
        private void DrawAlarmGUI(Alarm pAlarm)
        {

        }

        public void Dispose() { }
    }
}
