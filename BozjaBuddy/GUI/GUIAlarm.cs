using BozjaBuddy.Data.Alarm;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BozjaBuddy.GUI
{
    internal class GUIAlarm
    {
        public static string GUI_ID = "puCreateAlarm";
        private static Dictionary<int, string> _kErrors = new Dictionary<int, string>();
        private static string kFieldName = "Alarm Name";
        private static DateTime? kDateTime = null;
        private static string kFieldDateDD = "31";
        private static string kFieldDateMM = "12";
        private static string kFieldDateYYYY = "1999";
        private static string kFieldDateHh = "23";
        private static string kFieldDateMm = "59";
        private static string kFieldDateSs = "59";
        private static int kFieldDuration = 0;
        private static int kFieldOffset = 0;
        private static bool kFieldIsRevivable = false;
        private static string? kFieldTriggerString = "";
        private static int? kFieldTriggerInt = 0;
        private static bool kFieldAcceptAll = false;

        private static string[] kAlarmLabels = { "Time", "Weather", "FateCE" };
        private static List<string> kAlarmTerritories = WeatherBarSection._mTerritories.Keys.ToList();
        private static List<int> kAlarmWeathers = WeatherBarSection._mWeatherNames.Keys.ToList();
        private static int kComboCurrLabel = 0;
        private static string kComboCurrTerritoryId = "";
        private static int kComboCurrWeatherId = 0;
        unsafe private static ImGuiTextFilterPtr kFilter = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
        private static int kComboCurrFateId = 0;
        
        /// <summary>Do nothing if pop-up has already been opened.</summary>
        public static void CreateACPU(string pGUI_Key, bool pUseDateTimeNow = true, string? pNameSuggestion = null, int? pDefaultDuration = null, int? pDefaultOffset = null)
        {
            if (ImGui.IsPopupOpen($"{GUIAlarm.GUI_ID}##{pGUI_Key}")) return;
            GUIAlarm.ResetFields(
                pUseDateTimeNow: pUseDateTimeNow, 
                pNameSuggestion:pNameSuggestion, 
                pDefaultDuration: pDefaultDuration, 
                pDefaultOffset: pDefaultOffset);
            ImGui.OpenPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}");
        }
        public static void CreateACPU(string pGUI_Key, Alarm pAlarm)
        {
            if (ImGui.IsPopupOpen($"{GUIAlarm.GUI_ID}##{pGUI_Key}")) return;
            GUIAlarm.ResetFields(pAlarm);
            ImGui.OpenPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}");
        }
        private static void DrawComboTerritory(float tWidth = 200, Alarm? pAlarmToEdit = null)
        {
            if (GUIAlarm.kComboCurrTerritoryId == "")
                GUIAlarm.kComboCurrTerritoryId = pAlarmToEdit == null 
                                                 ? WeatherBarSection._mTerritories.Keys.ToList()[0]
                                                 : (pAlarmToEdit.mTriggerString ?? WeatherBarSection._mTerritories.Keys.ToList()[0]);
            ImGui.PushItemWidth(tWidth);
            if (ImGui.BeginCombo(
                "##aTerritory", 
                WeatherBarSection._mTerritories[GUIAlarm.kComboCurrTerritoryId]))
            {
                foreach (string iTerritoryId in WeatherBarSection._mTerritories.Keys)
                {
                    if (ImGui.Selectable(WeatherBarSection._mTerritories[iTerritoryId]))
                    {
                        GUIAlarm.kComboCurrTerritoryId = iTerritoryId;
                        GUIAlarm.kFieldTriggerString = GUIAlarm.kComboCurrTerritoryId;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }
        private static void DrawComboWeather(float tWidth = 200, Alarm? pAlarmToEdit = null)
        {
            if (GUIAlarm.kComboCurrWeatherId == 0)
                GUIAlarm.kComboCurrWeatherId = pAlarmToEdit == null
                                               ? WeatherBarSection._mWeatherNames.Keys.ToList()[0]
                                               : (pAlarmToEdit.mTriggerInt ?? WeatherBarSection._mWeatherNames.Keys.ToList()[0]);
            ImGui.PushItemWidth(tWidth);
            if (ImGui.BeginCombo("##aWeather", WeatherBarSection._mWeatherNames[GUIAlarm.kComboCurrWeatherId]))
            {
                foreach (int iWeatherId in WeatherBarSection._mWeatherNames.Keys)
                {
                    if (ImGui.Selectable(WeatherBarSection._mWeatherNames[iWeatherId]))
                    {
                        GUIAlarm.kComboCurrWeatherId = iWeatherId;
                        GUIAlarm.kFieldTriggerInt = GUIAlarm.kComboCurrWeatherId;
                    }
                }
                ImGui.EndCombo();
            }            
            ImGui.PopItemWidth();
        }
        private static void DrawComboFateCe(Plugin pPlugin, float tWidth = 200, Alarm? pAlarmToEdit = null)
        {
            Dictionary<int, Data.Fate> tFates = pPlugin.mBBDataManager.mFates;
            if (GUIAlarm.kComboCurrFateId == 0)
                GUIAlarm.kComboCurrFateId = pAlarmToEdit == null
                                            ? tFates.Values.ToList()[0].mId
                                            : (pAlarmToEdit.mTriggerInt ?? tFates.Values.ToList()[0].mId);
            ImGui.PushItemWidth(tWidth);
            if (ImGui.BeginCombo("##aFce", tFates[GUIAlarm.kComboCurrFateId].mName))
            {
                unsafe
                {
                    GUIAlarm.kFilter.Draw("");
                    foreach (Data.Fate iFate in tFates.Values)
                    {
                        if (GUIAlarm.kFilter.PassFilter(iFate.mName) 
                            && ImGui.Selectable($"{iFate.mId} {iFate.mName}"))
                        {
                            GUIAlarm.kComboCurrFateId = iFate.mId;
                            GUIAlarm.kFieldTriggerInt = GUIAlarm.kComboCurrFateId;
                        }
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }
        private static void DrawACPULabel(string? pReprString, string? pTooltip, bool pIsEditable = false)
        {
            if (pIsEditable)
            {
                GUIAlarm.DrawACPULabel_Editable();
            }
            else
            {
                GUIAlarm.DrawACPULabel_Default(pReprString, pTooltip);
            }
        }
        private static void DrawACPULabel_Default(string? pReprString, string? pTooltip)
        {
            ImGui.Text(pReprString ?? Alarm.kReprString);
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker(pTooltip ?? Alarm.kToolTip);
        }
        private static void DrawACPULabel_Editable()
        {
            ImGui.PushItemWidth(80);
            if (ImGui.BeginCombo("##aType", GUIAlarm.kAlarmLabels[GUIAlarm.kComboCurrLabel]))
            {
                for (int i = 0; i < kAlarmLabels.Length; i++)
                {
                    if (ImGui.Selectable(GUIAlarm.kAlarmLabels[i]))
                    {
                        GUIAlarm.kComboCurrLabel = i;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushTextWrapPos(450.0f);
                        switch (i)
                        {
                            case 0:
                                ImGui.TextUnformatted(AlarmTime.kToolTip);
                                break;
                            case 1:
                                ImGui.TextUnformatted(AlarmWeather.kToolTip);
                                break;
                            case 2:
                                ImGui.TextUnformatted(AlarmFateCe.kToolTip);
                                break;
                            default:
                                ImGui.TextUnformatted(Alarm.kToolTip);
                                break;
                        }
                        ImGui.PopTextWrapPos();
                        ImGui.EndTooltip();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }
        private static void DrawACPUTextInput_Default()
        {
            // Text inputs
            ImGui.PushItemWidth(ImGui.GetFontSize() * 30);
            ImGui.InputText("##name", ref GUIAlarm.kFieldName, 120);
            ImGui.PopItemWidth();
            ImGui.PushItemWidth(ImGui.GetFontSize() * 6);
            ImGui.InputInt("##duration", ref GUIAlarm.kFieldDuration, 10, 10, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
            ImGui.SameLine(); UtilsGUI.TextWithHelpMarker(
                                                "Audio duration [s]",
                                                $"The duration that the alarm's audio will play.\nRange from {Alarm.kDurationMin} to {Alarm.kDurationMax}",
                                                UtilsGUI.Colors.BackgroundText_Grey);
            ImGui.InputInt("##offset", ref GUIAlarm.kFieldOffset, 10, 10, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
            ImGui.SameLine(); UtilsGUI.TextWithHelpMarker(
                                                "Offset [s]",
                                                $"Offset to alarm.\ne.g. An alarm for Raining will trigger X seconds before it actually rain. X is its offset.\nRange from {Alarm.kOffsetMin} to {Alarm.kOffsetMax}.",
                                                UtilsGUI.Colors.BackgroundText_Grey);
            ImGui.PopItemWidth();
        }
        private static void DrawACPUButton_Default<T>(Plugin pPlugin, DateTime? pTriggerTime = null) where T : Alarm, new()
        {
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save))
            {
                DateTime tTempDateTime = DateTime.Now;
                if (DateTime.TryParse(
                        $"{GUIAlarm.kFieldDateYYYY}-{GUIAlarm.kFieldDateMM}-{GUIAlarm.kFieldDateDD} {GUIAlarm.kFieldDateHh}:{GUIAlarm.kFieldDateMm}:{GUIAlarm.kFieldDateSs}",
                        out tTempDateTime))
                {
                    GUIAlarm.kDateTime = tTempDateTime;
                }
                else if (!GUIAlarm._kErrors.ContainsKey(0))
                {
                    GUIAlarm._kErrors.Add(0, "Incorrect date value.");
                }
                T tTempAlarm = new T();
                tTempAlarm.Init(pTriggerTime ?? GUIAlarm.kDateTime,
                                GUIAlarm.kFieldTriggerInt,
                                GUIAlarm.kFieldName,
                                GUIAlarm.kFieldDuration,
                                true,
                                GUIAlarm.kFieldIsRevivable,
                                GUIAlarm.kFieldTriggerString,
                                GUIAlarm.kFieldOffset);
                pPlugin.AlarmManager.AddAlarm(tTempAlarm);

                ImGui.CloseCurrentPopup();
                pPlugin.WindowSystem.GetWindow("Alarm - BozjaBuddy")!.IsOpen = true;
            }
        }
        private static void DrawACPUButton_Edit<T>(Plugin pPlugin, Alarm pAlarm) where T : Alarm
        {
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save))
            {
                DateTime tTempDateTime = DateTime.Now;
                if (DateTime.TryParse(
                        $"{GUIAlarm.kFieldDateYYYY}-{GUIAlarm.kFieldDateMM}-{GUIAlarm.kFieldDateDD} {GUIAlarm.kFieldDateHh}:{GUIAlarm.kFieldDateMm}:{GUIAlarm.kFieldDateSs}",
                        out tTempDateTime))
                {
                    GUIAlarm.kDateTime = tTempDateTime;
                }
                else if (!GUIAlarm._kErrors.ContainsKey(0))
                {
                    GUIAlarm._kErrors.Add(0, "Incorrect date value.");
                }

                pAlarm.mTriggerTime = GUIAlarm.kDateTime;
                pAlarm.mTriggerInt = GUIAlarm.kFieldTriggerInt;
                pAlarm.mTriggerString = GUIAlarm.kFieldTriggerString;
                pAlarm.mName = GUIAlarm.kFieldName;
                pAlarm.mDuration = GUIAlarm.kFieldDuration;
                pAlarm.mIsRevivable = GUIAlarm.kFieldIsRevivable;
                pAlarm.mOffset = GUIAlarm.kFieldOffset;
                pPlugin.AlarmManager.SaveAlarmListsToDisk();

                ImGui.CloseCurrentPopup();
                pPlugin.WindowSystem.GetWindow("Alarm - BozjaBuddy")!.IsOpen = true;
            }            
        }
        public static void DrawACPU_Weather(Plugin pPlugin,
                                    string pGUI_Key,
                                    int? pTriggerInt = null,
                                    string? pTriggerString = null,
                                    DateTime? pTriggerTime = null,
                                    bool pIsLabelEditable = false,
                                    Alarm? pAlarmToEdit = null)
        {
            if (ImGui.BeginPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}"))
            {
                if (pAlarmToEdit == null)
                {
                    GUIAlarm.kFieldTriggerString = pTriggerString ?? GUIAlarm.kFieldTriggerString ?? WeatherBarSection._mTerritories.Keys.ToList()[0];
                    GUIAlarm.kFieldTriggerInt = pTriggerInt ?? GUIAlarm.kFieldTriggerInt ?? WeatherBarSection._mWeatherNames.Keys.ToList()[0];
                }
                else
                {
                    GUIAlarm.kFieldTriggerString = GUIAlarm.kComboCurrTerritoryId;
                    GUIAlarm.kFieldTriggerInt = GUIAlarm.kComboCurrWeatherId;
                }

                // Text input
                ImGui.BeginGroup();
                GUIAlarm.DrawACPUTextInput_Default();
                if (pIsLabelEditable || pAlarmToEdit != null)
                {
                    GUIAlarm.DrawComboTerritory();
                    ImGui.SameLine(); GUIAlarm.DrawComboWeather(pAlarmToEdit: pAlarmToEdit);
                }
                ImGui.EndGroup();

                ImGui.SameLine();

                // Labels and button
                ImGui.BeginGroup();
                GUIAlarm.DrawACPULabel(AlarmWeather.kReprString, AlarmWeather.kToolTip, pIsLabelEditable);
                if (pAlarmToEdit == null)
                    GUIAlarm.DrawACPUButton_Default<AlarmWeather>(pPlugin, pTriggerTime);
                else
                    GUIAlarm.DrawACPUButton_Edit<AlarmWeather>(pPlugin, pAlarmToEdit);
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
                {
                    GUIAlarm.kFieldIsRevivable = !GUIAlarm.kFieldIsRevivable;
                }
                ImGui.SameLine(); ImGui.Text(GUIAlarm.kFieldIsRevivable ? "Repeat" : "Once");
                ImGui.EndGroup();

                ImGui.EndPopup();
            }
        }
        public static void DrawACPU_Time(Plugin pPlugin,
                                    string pGUI_Key,
                                    DateTime? pTriggerTime = null,
                                    bool pIsLabelEditable = false,
                                    bool pIsInEditMode = false,
                                    Alarm? pAlarmToEdit = null)
        {
            if (ImGui.BeginPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}"))
            {
                // Text input
                ImGui.BeginGroup();
                GUIAlarm.DrawACPUTextInput_Default();
                ImGui.Text("Time [YYYY/MM/DD hh/mm/ss]");
                ImGui.PushItemWidth(ImGui.GetFontSize() * 2);
                ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
                ImGui.SameLine(); ImGui.InputTextWithHint("##yyyy", "YYYY", ref GUIAlarm.kFieldDateYYYY, 4, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.PopItemWidth();
                ImGui.SameLine(); ImGui.Text("/");
                ImGui.SameLine(); ImGui.InputTextWithHint("##mm", "MM", ref GUIAlarm.kFieldDateMM, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.SameLine(); ImGui.Text("/");
                ImGui.SameLine(); ImGui.InputTextWithHint("##dd", "DD", ref GUIAlarm.kFieldDateDD, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.SameLine(); ImGui.Spacing();
                ImGui.SameLine(); ImGui.InputTextWithHint("##Hh", "hh", ref GUIAlarm.kFieldDateHh, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.SameLine(); ImGui.Text(":");
                ImGui.SameLine(); ImGui.InputTextWithHint("##Mm", "mm", ref GUIAlarm.kFieldDateMm, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.SameLine(); ImGui.Text(":");
                ImGui.SameLine(); ImGui.InputTextWithHint("##Ss", "ss", ref GUIAlarm.kFieldDateSs, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.PopItemWidth();
                ImGui.EndGroup();

                ImGui.SameLine();

                // Labels and button
                ImGui.BeginGroup();
                GUIAlarm.DrawACPULabel(AlarmTime.kReprString, AlarmTime.kToolTip, pIsLabelEditable);
                if (pAlarmToEdit == null)
                    GUIAlarm.DrawACPUButton_Default<AlarmTime>(pPlugin, pTriggerTime);
                else
                    GUIAlarm.DrawACPUButton_Edit<AlarmTime>(pPlugin, pAlarmToEdit);
                ImGui.EndGroup();

                ImGui.EndPopup();
            }
        }
        public static void DrawACPU_FateCe(Plugin pPlugin,
                                    string pGUI_Key,
                                    int? pTriggerInt = null,
                                    DateTime? pTriggerTime = null,
                                    bool pIsLabelEditable = false,
                                    bool pIsInEditMode = false,
                                    Alarm? pAlarmToEdit = null)
        {
            if (ImGui.BeginPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}"))
            {
                if (pAlarmToEdit == null)
                {
                    GUIAlarm.kFieldTriggerInt =
                        GUIAlarm.kFieldAcceptAll 
                        ? AlarmFateCe.kTriggerInt_AcceptAllCe
                        : pTriggerInt ?? GUIAlarm.kFieldTriggerInt ?? pPlugin.mBBDataManager.mFates.Keys.ToList()[0];
                }
                else
                {
                    GUIAlarm.kComboCurrFateId = pAlarmToEdit.mTriggerInt.HasValue ? pAlarmToEdit.mTriggerInt.Value : GUIAlarm.kComboCurrFateId;
                    GUIAlarm.kFieldTriggerString = GUIAlarm.kComboCurrTerritoryId;
                    GUIAlarm.kFieldTriggerInt = GUIAlarm.kFieldAcceptAll ? AlarmFateCe.kTriggerInt_AcceptAllCe : GUIAlarm.kComboCurrFateId;
                }

                // Text input
                ImGui.BeginGroup();
                GUIAlarm.DrawACPUTextInput_Default();

                ImGui.Checkbox("##aa", ref GUIAlarm.kFieldAcceptAll);
                ImGui.SameLine(); UtilsGUI.TextDescriptionForWidget("Any CE (FATEs excluded)");

                if (pIsLabelEditable || pAlarmToEdit != null)
                {
                    if (GUIAlarm.kFieldAcceptAll)
                    {
                        ImGui.BeginDisabled();
                        GUIAlarm.DrawComboFateCe(pPlugin, pAlarmToEdit: pAlarmToEdit);
                        ImGui.EndDisabled();
                    }
                    else { GUIAlarm.DrawComboFateCe(pPlugin, pAlarmToEdit: pAlarmToEdit); }

                    GUIAlarm.kFieldTriggerInt = GUIAlarm.kFieldAcceptAll ? AlarmFateCe.kTriggerInt_AcceptAllCe : pPlugin.mBBDataManager.mFates[GUIAlarm.kComboCurrFateId].mId;
                }

                ImGui.EndGroup();

                ImGui.SameLine();

                // Labels and button
                ImGui.BeginGroup();
                GUIAlarm.DrawACPULabel(AlarmFateCe.kReprString, AlarmFateCe.kToolTip, pIsLabelEditable);
                if (pAlarmToEdit == null)
                    GUIAlarm.DrawACPUButton_Default<AlarmFateCe>(pPlugin, pTriggerTime);
                else
                    GUIAlarm.DrawACPUButton_Edit<AlarmFateCe>(pPlugin, pAlarmToEdit);
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
                {
                    GUIAlarm.kFieldIsRevivable = !GUIAlarm.kFieldIsRevivable;
                }
                ImGui.SameLine(); ImGui.Text(GUIAlarm.kFieldIsRevivable ? "Repeat" : "Once");
                ImGui.EndGroup();

                ImGui.EndPopup();
            }
        }
        public static void DrawACPU_All(Plugin pPlugin,
                                    string pGUI_Key,
                                    int? pTriggerInt = null,
                                    string? pTriggerString = null,
                                    DateTime? pTriggerTime = null)
        {
            switch (GUIAlarm.kComboCurrLabel)
            {
                case 0:
                    GUIAlarm.DrawACPU_Time(
                                        pPlugin,
                                        pGUI_Key,
                                        pTriggerTime,
                                        true);
                    break;
                case 1:
                    GUIAlarm.DrawACPU_Weather(
                                        pPlugin,
                                        pGUI_Key,
                                        pTriggerInt,
                                        pTriggerString,
                                        pTriggerTime,
                                        true);
                    break;
                case 2:
                    GUIAlarm.DrawACPU_FateCe(
                                        pPlugin,
                                        pGUI_Key,
                                        pTriggerInt,
                                        pTriggerTime,
                                        true);
                    break;
                default:
                    GUIAlarm.DrawACPU_Time(
                                        pPlugin,
                                        pGUI_Key,
                                        pTriggerTime,
                                        true);
                    break;
            }
        }

        private static void ResetFields(bool pUseDateTimeNow = false, string? pNameSuggestion = null, int? pDefaultDuration = null, int? pDefaultOffset = null)
        {
            GUIAlarm._kErrors.Clear();

            GUIAlarm.kFieldName = pNameSuggestion ?? "Alarm Name";
            GUIAlarm.kDateTime = DateTime.Now;
            if (pUseDateTimeNow)
            {
                GUIAlarm.kFieldDateDD = DateTime.Now.Day.ToString();
                GUIAlarm.kFieldDateMM = DateTime.Now.Month.ToString();
                GUIAlarm.kFieldDateYYYY = DateTime.Now.Year.ToString();
                GUIAlarm.kFieldDateHh = DateTime.Now.Hour.ToString();
                GUIAlarm.kFieldDateMm = DateTime.Now.Minute.ToString();
                GUIAlarm.kFieldDateSs = DateTime.Now.Second.ToString();
            }
            else
            {
                GUIAlarm.kFieldDateDD = "31";
                GUIAlarm.kFieldDateMM = "12";
                GUIAlarm.kFieldDateYYYY = "1999";
                GUIAlarm.kFieldDateHh = "24";
                GUIAlarm.kFieldDateMm = "59";
                GUIAlarm.kFieldDateSs = "59";
            }
            GUIAlarm.kFieldDuration = pDefaultDuration ?? Alarm.kDurationMin;
            GUIAlarm.kFieldOffset = pDefaultOffset ?? Alarm.kOffsetMin;
            GUIAlarm.kFieldIsRevivable = false;
            GUIAlarm.kFieldTriggerInt = null;
            GUIAlarm.kFieldTriggerString = null;
            GUIAlarm.kFieldAcceptAll = false;

            GUIAlarm.kComboCurrTerritoryId = "";
            GUIAlarm.kComboCurrWeatherId = 0;
        }
        private static void ResetFields(Alarm pAlarm)
        {
            GUIAlarm._kErrors.Clear();

            GUIAlarm.kFieldName = pAlarm.mName;
            GUIAlarm.kDateTime = pAlarm.mTriggerTime;
            GUIAlarm.kFieldDuration = pAlarm.mDuration;
            GUIAlarm.kFieldOffset = pAlarm.mOffset;
            GUIAlarm.kFieldIsRevivable = pAlarm.mIsRevivable;
            GUIAlarm.kFieldTriggerInt = pAlarm.mTriggerInt ?? null;
            GUIAlarm.kFieldTriggerString = pAlarm.mTriggerString ?? null;
            GUIAlarm.kFieldAcceptAll = false;

            GUIAlarm.kFieldDateDD = pAlarm.mTriggerTime!.Value.Day.ToString();
            GUIAlarm.kFieldDateMM = pAlarm.mTriggerTime!.Value.Month.ToString();
            GUIAlarm.kFieldDateYYYY = pAlarm.mTriggerTime!.Value.Year.ToString();
            GUIAlarm.kFieldDateHh = pAlarm.mTriggerTime!.Value.Hour.ToString();
            GUIAlarm.kFieldDateMm = pAlarm.mTriggerTime!.Value.Minute.ToString();
            GUIAlarm.kFieldDateSs = pAlarm.mTriggerTime!.Value.Second.ToString();

            //foreach (WeatherBarSection._mTerritories[GUIAlarm.kAlarmTerritories[GUIAlarm.kComboCurrTerritory]])
        }
    }
}
