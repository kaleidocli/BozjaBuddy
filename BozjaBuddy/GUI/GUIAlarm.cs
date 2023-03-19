using BozjaBuddy.Data.Alarm;
using BozjaBuddy.Utils;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.GUI
{
    internal class GUIAlarm
    {
        public static string GUI_ID = "puCreateAlarm";
        private static bool _kIsResetting = false;
        private static Dictionary<int, string> _kErrors = new Dictionary<int, string>();
        private static string kFieldName = "Alarm Name";
        private static DateTime kDateTime = new DateTime();
        private static string kFieldDateDD = "31";
        private static string kFieldDateMM = "12";
        private static string kFieldDateYYYY = "1999";
        private static string kFieldDateHh = "24";
        private static string kFieldDateMm = "59";
        private static string kFieldDateSs = "59";
        private static int kFieldDuration = 0;
        private static bool kFieldIsRevivable = false;

        public static void CreatePopupCreateAlarm(string pGUI_Key)
        {
            ImGui.OpenPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}");
        }
        public static void DrawPopupCreateAlarm<T>(Plugin pPlugin, string pGUI_Key, int pTriggerInt, string? pNameSuggestion = null) where T : Alarm, new()
        {
            if (!GUIAlarm._kIsResetting)
            {
                GUIAlarm.ResetFields();
            }
            if (ImGui.BeginPopup($"{GUIAlarm.GUI_ID}##{pGUI_Key}"))
            {
                // Text inputs
                GUIAlarm._kIsResetting = true;
                GUIAlarm.kFieldName = pNameSuggestion ?? GUIAlarm.kFieldName;
                ImGui.BeginGroup();
                ImGui.PushItemWidth(ImGui.GetFontSize() * 30);
                ImGui.InputText("##name", ref GUIAlarm.kFieldName, 120);
                ImGui.PopItemWidth();
                if (typeof(T) == typeof(AlarmTime))
                {
                    ImGui.Text("at ");
                    ImGui.PushItemWidth(ImGui.GetFontSize() * 2);
                    ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
                    ImGui.SameLine(); ImGui.InputTextWithHint("##yyyy", "YYYY", ref GUIAlarm.kFieldDateYYYY, 4, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.PopItemWidth();
                    ImGui.SameLine(); ImGui.InputTextWithHint("##mm", "MM", ref GUIAlarm.kFieldDateMM, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.SameLine(); ImGui.InputTextWithHint("##dd", "DD", ref GUIAlarm.kFieldDateDD, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.SameLine(); ImGui.InputTextWithHint("##Hh", "hh", ref GUIAlarm.kFieldDateHh, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.SameLine(); ImGui.InputTextWithHint("##Mm", "mm", ref GUIAlarm.kFieldDateMm, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.SameLine(); ImGui.InputTextWithHint("##Ss", "ss", ref GUIAlarm.kFieldDateSs, 2, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.Text("for ");
                    ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
                    ImGui.InputInt("##duration", ref GUIAlarm.kFieldDuration, 1, 1, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.AllowTabInput);
                    ImGui.PopItemWidth();
                    ImGui.Text("s");
                    ImGui.PopItemWidth();
                }
                ImGui.EndGroup();

                ImGui.SameLine();

                // Buttons
                ImGui.BeginGroup();
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin))
                {
                    GUIAlarm.kFieldIsRevivable = !GUIAlarm.kFieldIsRevivable;
                }
                ImGui.SameLine(); ImGui.Text(GUIAlarm.kFieldIsRevivable ? "Repeat" : "Once");
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save))
                {
                    if (DateTime.TryParse($"{GUIAlarm.kFieldDateYYYY}-{GUIAlarm.kFieldDateMM}-{GUIAlarm.kFieldDateDD} {GUIAlarm.kFieldDateHh}:{GUIAlarm.kFieldDateMm}:{GUIAlarm.kFieldDateSs}", out GUIAlarm.kDateTime))
                    {
                        T tTempAlarm = new T();
                        tTempAlarm.Init(typeof(T) == typeof(AlarmTime) ? GUIAlarm.kDateTime : null,
                                        pTriggerInt,
                                        GUIAlarm.kFieldName,
                                        GUIAlarm.kFieldDuration,
                                        true,
                                        GUIAlarm.kFieldIsRevivable);
                        pPlugin.AlarmManager.AddAlarm(tTempAlarm);

                        ImGui.CloseCurrentPopup();
                    }
                    else if (!GUIAlarm._kErrors.ContainsKey(0))
                    {
                        GUIAlarm._kErrors.Add(0, "Incorrect date value.");
                    }
                }
                UtilsGUI.ShowHelpMarker(AlarmWeather.kToolTip);
                ImGui.EndGroup();

                // Error
                foreach (string iErrString in GUIAlarm._kErrors.Values)
                {
                    ImGui.TextColored(ImGuiColors.DPSRed, iErrString);
                }

                ImGui.EndPopup();
            }
        }
        public static void ResetPopup()
        {
            GUIAlarm._kIsResetting = true;
        }
        private static void ResetFields()
        {
            GUIAlarm._kIsResetting = false;
            GUIAlarm._kErrors.Clear();
            GUIAlarm.kFieldName = "Alarm Name";
            GUIAlarm.kDateTime = new DateTime();
            GUIAlarm.kFieldDateDD = "31";
            GUIAlarm.kFieldDateMM = "12";
            GUIAlarm.kFieldDateYYYY = "1999";
            GUIAlarm.kFieldDateHh = "24";
            GUIAlarm.kFieldDateMm = "59";
            GUIAlarm.kFieldDateSs = "59";
            GUIAlarm.kFieldDuration = 0;
            GUIAlarm.kFieldIsRevivable = false;
        }
    }
}
