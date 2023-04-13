using BozjaBuddy.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVWeather;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;
using BozjaBuddy.Data.Alarm;
using Dalamud.Logging;
using FFXIVWeather.Models;

namespace BozjaBuddy.GUI.Sections
{
    internal class WeatherBarSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }

        private TextureCollection mTextureCollection;
        public static Dictionary<string, string> _mTerritories = new Dictionary<string, string>() {
                                                                                            { "n4b4", "Bozja" },
                                                                                            { "n4b6", "Zadnor" } 
                                                                                                    };
        public static string _mCurrTerritory = "n4b4";
        public static Dictionary<int, uint> _mWeatherColor = new Dictionary<int, uint>()
        {
            { 7, 0xAAE4C468 },  // Rain
            { 9, 0xAAE923EF },   // Thunder
            { 11, 0xAA2367EF },  // Dust
            { 2, 0xAA4B4646 },  // Fair
            { 5, 0xAA4BEF23 },  // Wind
            { 15, 0xAAE3F2F3 }   // Snow
        };
        public static Dictionary<int, string> _mWeatherNames = new Dictionary<int, string>()
        {
            { 7, "Rain" },  // Rain
            { 9, "Thunder" },   // Thunder
            { 11, "Dust" },  // Dust
            { 2, "Fair" },  // Fair
            { 5, "Wind" },  // Wind
            { 15, "Snow" }   // Snow
        };

        public static FFXIVWeatherService _mWeatherService = new FFXIVWeatherService();
        public static Dictionary<string, List<(FFXIVWeather.Models.Weather, DateTime)>> _mForeCast = new Dictionary<string, List<(FFXIVWeather.Models.Weather, DateTime)>>();

        public WeatherBarSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.mTextureCollection = new TextureCollection(this.mPlugin);
        }

        public override bool DrawGUI()
        {
            DrawBar();

            return true;
        }

        private void DrawBar()
        {
            ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - ImGui.GetStyle().FramePadding.X * 2, 40), true, ImGuiWindowFlags.NoScrollbar);
            this.DrawWeatherTimeline(WeatherBarSection._mCurrTerritory);
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.BeginCombo("", WeatherBarSection._mTerritories[WeatherBarSection._mCurrTerritory]))
            {
                foreach (string iTerritoryId in WeatherBarSection._mTerritories.Keys)
                    if (ImGui.Selectable(WeatherBarSection._mTerritories[iTerritoryId]))
                        WeatherBarSection._mCurrTerritory = iTerritoryId;
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            ImGui.EndChild();
        }
        private void DrawWeatherTimeline(string pTerritoryId)
        {
            ImGui.BeginGroup();
            (FFXIVWeather.Models.Weather, DateTime) tWeatherCurr_bozja = WeatherBarSection.GetWeatherCurr(this.mPlugin, pTerritoryId);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, ImGui.GetStyle().ItemSpacing[1]));
            for (int i = 0; i < WeatherBarSection._mForeCast[pTerritoryId].Count; i++)
            {
                string tTempGUI_Key = $"weather{i}";
                (FFXIVWeather.Models.Weather, DateTime) tWeather = WeatherBarSection._mForeCast[pTerritoryId][i];
                DateTime tProcessedTime = Utils.Utils.ProcessToLocalTime(tWeather.Item2);

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, WeatherBarSection._mWeatherColor[tWeather.Item1.Id]);
                if (tWeather.Item2 != tWeatherCurr_bozja.Item2)
                {
                    if (ImGui.Button($"   ##{i}"))
                    {
                        GUIAlarm.CreateACPU(
                            tTempGUI_Key,
                            pNameSuggestion: $"{AlarmWeather.kReprString} {Alarm.GetIdCounter()} | {tWeather.Item1.GetName()} | {WeatherBarSection._mTerritories[pTerritoryId]}",
                            pDefaultDuration: this.mPlugin.Configuration.mDefaultAlarmDuration,
                            pDefaultOffset: this.mPlugin.Configuration.mDefaultAlarmOffset);
                    }
                    //else if (!ImGui.IsPopupOpen($"{GUIAlarm.GUI_ID}##{tTempGUI_Key}"))
                    //{
                    //    GUIAlarm.ResetPopup();
                    //}
                }
                else
                {
                    if (ImGui.Button(String.Format("{0} left ",
                                                TimeSpan
                                                .FromSeconds(
                                                    Math.Round(
                                                        (WeatherBarSection._mForeCast[pTerritoryId][i + (i + 1 < WeatherBarSection._mForeCast[pTerritoryId].Count ? 1 : 0)].Item2 - DateTime.UtcNow)
                                                        .TotalSeconds,
                                                        MidpointRounding.ToNegativeInfinity
                                                        )
                                                    )
                                                .ToString(@"mm\:ss"))))
                    {
                        GUIAlarm.CreateACPU(
                            tTempGUI_Key, 
                            pNameSuggestion: $"{AlarmWeather.kReprString} {Alarm.GetIdCounter()} | {tWeather.Item1.GetName()} | {WeatherBarSection._mTerritories[pTerritoryId]}",
                            pDefaultDuration: this.mPlugin.Configuration.mDefaultAlarmDuration,
                            pDefaultOffset: this.mPlugin.Configuration.mDefaultAlarmOffset);
                    }
                }
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                GUIAlarm.DrawACPU_Weather(
                                            this.mPlugin, 
                                            tTempGUI_Key, 
                                            tWeather.Item1.Id, 
                                            pTerritoryId,
                                            tProcessedTime);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, ImGui.GetStyle().ItemSpacing[1]));
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"{tWeather.Item1.GetName()}:\nin {Math.Round((tWeather.Item2 - DateTime.UtcNow).TotalMinutes)} mins\nat {tProcessedTime.ToLongTimeString()}");
                }
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.EndGroup();
        }

        public override void DrawGUIDebug()
        {

        }

        public static (FFXIVWeather.Models.Weather, DateTime) GetWeatherCurr(Plugin pPlugin, string pTerritoryId)
        {
            WeatherBarSection._updateWeatherCurr(pPlugin, pTerritoryId);
            return WeatherBarSection._mForeCast[pTerritoryId][0];
        }
        public static (FFXIVWeather.Models.Weather, DateTime) GetWeatherNext(Plugin? pPlugin, string pTerritoryId, bool pUpdateWeather = true, bool pUpdateWeatherAll = false)
        {
            if (pPlugin != null && pUpdateWeather) WeatherBarSection._updateWeatherCurr(pPlugin, pTerritoryId);
            return WeatherBarSection._mForeCast[pTerritoryId][1];
        }
        public static List<(FFXIVWeather.Models.Weather, DateTime)> GetForecast(Plugin pPlugin, string pTerritoryId)
        {
            WeatherBarSection._updateWeatherCurr(pPlugin, pTerritoryId);
            return WeatherBarSection._mForeCast[pTerritoryId];
        }
        public static void _updateForecast(Plugin pPlugin, string pTerritoryId)
        {
            WeatherBarSection._mForeCast[pTerritoryId] = WeatherBarSection._mWeatherService.GetForecast(
                                                    Convert.ToInt32(
                                                            pPlugin.DataManager.Excel.GetSheet<TerritoryType>()?
                                                                                .Select(o => o)
                                                                                .Where(o => o.Name.ToString() == pTerritoryId)
                                                                                .ToList()[0].RowId
                                                    ),
                                                    30
                                            ).ToList();
        }
        public static void _updateWeatherCurr(Plugin pPlugin, string pTerritoryId)
        {
            // first forecast
            if (!WeatherBarSection._mForeCast.ContainsKey(pTerritoryId))
            {
                WeatherBarSection._updateForecast(pPlugin, pTerritoryId);
                // Notify AlarmManager's listener
                WeatherBarSection._NotifyAlarmListener(pTerritoryId);
            }

            // refresh forecast only if the second weather has neg delta
            if (Math.Round((WeatherBarSection._mForeCast[pTerritoryId][1].Item2 - DateTime.UtcNow).TotalSeconds) < 0)
            {
                WeatherBarSection._updateForecast(pPlugin, pTerritoryId);
                // Notify AlarmManager's listener
                WeatherBarSection._NotifyAlarmListener(pTerritoryId);
            }
        }
        public static void _updateWeatherCurr(Plugin pPlugin)
        {
            foreach (string iTerritoryId in WeatherBarSection._mTerritories.Keys)
            {
                WeatherBarSection._updateWeatherCurr(pPlugin, iTerritoryId);
            }
        }
        private static void _NotifyAlarmListener(string pTerritoryId)
        {
            if (WeatherBarSection._mForeCast[pTerritoryId].Count < 2) return;
            AlarmManager.NotifyListener(
                                    "weather",
                                    new MsgAlarmWeather(pTerritoryId, WeatherBarSection._mForeCast[pTerritoryId][0].Item1.Id),
                                    pCapacityToReachAndClearChannel: 2);

            // weatherSequel listener is for peeking into the next weather
            AlarmManager.NotifyListener(
                                    "weatherSequel",
                                    new MsgAlarmWeather(pTerritoryId, WeatherBarSection._mForeCast[pTerritoryId][1].Item1.Id),
                                    pCapacityToReachAndClearChannel: 2);
        }

        public override void Dispose()
        {
            this.mTextureCollection.Dispose();
        }
    }
}
