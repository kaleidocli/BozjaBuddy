﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using static BozjaBuddy.GUI.GUIAssist.GUIAssistManager;
using System.Collections.Generic;
using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.Utils;
using System.Linq;
using Dalamud.Logging;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Runtime.CompilerServices;
using BozjaBuddy.GUI.Sections;

namespace BozjaBuddy
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        // Sys
        public int Version { get; set; } = 0;
        public Dalamud.Interface.Windowing.Window.WindowSizeConstraints SizeConstraints = new()
        {
            MinimumSize = new Vector2(675, 509),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        public const int kDefaultAlarmDuration = 30;
        public const int kDefaultAlarmOffset = 30;
        public const float kDefaultVolume = 1.0f;
        private static bool _isLocked = false;

        public float STYLE_ICON_SIZE { get; set; } = 20f;
        public float mAudioVolume = Configuration.kDefaultVolume;
        public string? mAudioPath = null;
        public int mDefaultAlarmDuration = Configuration.kDefaultAlarmDuration;
        public int mDefaultAlarmOffset = Configuration.kDefaultAlarmOffset;
        public Dictionary<GUIAssistOption, bool> mOptionState = new();
        public Dictionary<int, int> _userCacheData = new();
        public Dictionary<int, int> _userHolsterData = new();
        /// <summary> for when LostAction id is not available (e.g. crawling Add on). Not recommended. </summary>
        public Dictionary<string, int> _userHolsterDataByName = new();
        public bool mMuteAAudioOnGameFocused = true;
        public bool mIsCacheAlertGeneralActive = true;
        public int mCacheAlertGeneralThreshold = 10;
        public bool mIsCacheAlertSpecificActive = true;
        public Dictionary<int, int> _cacheAlertSpecificThresholds = new();
        public bool mIsCacheAlertIgnoringActive = false;
        public bool mIsShowingRecLoadout = true;
        public bool mIsInGridMode_FieldNoteTableSection = true;
        public bool mIsInGridMode_LostActionTableSection = true;
        public bool mIsAroVisible_LostActionTableSection = true;
        public int isAuxiVisible = 1;       // 0:hidden    1:half-window   2:full-window
        public HashSet<int> mCacheAlertIgnoreIds = new();
        public HashSet<int> mUserFieldNotes = new();
        public bool mIsAuxiUsingNGV = true;
        public string? mAuxiNGVSaveData = null;
        public Dictionary<Job, RelicSection.RelicStep> mRelicProgress = UtilsGameData.kRelicValidJobs.ToDictionary(o => o, o => RelicSection.RelicStep.None);

        public bool mIsRelicFirstTime = true;
        public int mRelicOTG2Path = 0;
        public Job mRelicCurrJob = RelicSection.kDefaultCurrJob;

        public GuiAssistConfig mGuiAssistConfig = new();

        // User data
        public LoadoutListJson? UserLoadouts = null;
        public List<List<Alarm>>? UserAlarms = null;

        public Dictionary<Job, List<int?>> _autoLoadout = (new List<Job>((Job[])Enum.GetValues(typeof(Job))))       // public so that it'd save into the config
                                                        .ToDictionary(o => o, o => new List<int?>() { null, null });

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
            this.mGuiAssistConfig.itemBox.userCacheData = this._userCacheData;
            this.mGuiAssistConfig.itemBox.userHolsterData = this._userHolsterData;
            this.mGuiAssistConfig.itemBox.userHolsterDataByName = this._userHolsterDataByName;
        }

        public void Save([CallerMemberName] string pCaller = "")
        {
            //PluginLog.LogDebug("> Save() is being called!");
            if (Configuration._isLocked) 
            { 
                //PluginLog.LogDebug("> CONFIG FILE LOCKED! Aborting save..."); 
                return; 
            }
            //PluginLog.LogDebug($"> CONFIG SAVED! (fromt {pCaller})");
            Configuration._isLocked = true;
            this.PluginInterface!.SavePluginConfig(this);
            Configuration._isLocked = false;
        }
        public void SetOverlay(Job pJob, int? pLoadoutId, int pSlotIndex = 0)
        {
            if (!UtilsGameData.kValidJobs.Contains(pJob)) return;

            pSlotIndex = System.Math.Abs(pSlotIndex > this._autoLoadout.Count ? this._autoLoadout.Count - 1 : pSlotIndex);
            if (!this._autoLoadout.ContainsKey(pJob)) { this._autoLoadout.Add(pJob, new List<int?>(this._autoLoadout.Count)); }
            else { this._autoLoadout[pJob][pSlotIndex] = pLoadoutId; }
            this.Save();
        }
        /// <summary>
        /// Get Loadout's ID from autoloadout
        /// </summary>
        public int? GetOverlay(Job pJob, int pSlotIndex = 0) => this._autoLoadout[pJob][System.Math.Abs(pSlotIndex > this._autoLoadout.Count ? this._autoLoadout.Count - 1 : pSlotIndex)];
        /// <summary>
        /// Get Loadout from autoloadout
        /// </summary>
        public Loadout? GetOverlay(Plugin pPlugin, Job pJob, int pSlotIndex = 0)
        {
            int? tLoadoutId = this.GetOverlay(pJob, pSlotIndex: pSlotIndex);
            if (tLoadoutId == null) return null;
            if (!pPlugin.mBBDataManager.mLoadouts.ContainsKey(tLoadoutId!.Value))
            {
                this.SetOverlay(pJob, null);
                return null;
            }
            return pPlugin.mBBDataManager.mLoadouts[tLoadoutId!.Value];
        }
        public Loadout? GetActiveOverlay(Plugin pPlugin)
        {
            var tUserJob = this.mGuiAssistConfig.overlay.isUsingJobSpecific
                            ? UtilsGameData.GetUserJob(pPlugin)
                            : Job.ALL;
            if (tUserJob == null) return null;
            return this.GetOverlay(pPlugin, tUserJob.Value, this.mGuiAssistConfig.overlay.currentSlotIndex);
        }
        public int GetCacheSpecificThresholds(int pActionId)
        {
            this._cacheAlertSpecificThresholds.TryGetValue(pActionId, out int tRes);
            return tRes;
        }
        public void SetCacheSpecificThresholds(int pActionId, int pThreshold)
        {
            if (!this._cacheAlertSpecificThresholds.TryAdd(pActionId, pThreshold))
            {
                this._cacheAlertSpecificThresholds[pActionId] = pThreshold;
            }
        }
        /// <summary> O(2n^2). Use sparingly. </summary>
        public void UpdateCacheHolsterInfo(Plugin pPlugin)
        {
            var a = this.mGuiAssistConfig.itemBox.userHolsterData.Count;
            // Getting agent data
            unsafe
            {
                AgentMycItemBox* tAgent = null;
                tAgent = BBDataManager.GetAgentMycItemBox();
                if (tAgent == null) { return; }
                var tData = tAgent->ItemBoxData;
                if (tData == null) { return; }

                // Clear data. Build from scratch.
                this.mGuiAssistConfig.itemBox.userHolsterData.Clear();
                this.mGuiAssistConfig.itemBox.userCacheData.Clear();
                this.mGuiAssistConfig.itemBox.userHolsterDataByName.Clear();

                foreach (MycItemCategory iHolster in tData->ItemHolsters)
                {
                    foreach (MycItem iItem in iHolster.Items)
                    {
                        if (iItem.ActionId == 0) continue;
                        var tActionId = UtilsGameData.ConvertGameIdToInternalId_LostAction(iItem.ActionId);
                        var tActionName = pPlugin.mBBDataManager.mLostActions[tActionId].mName;
                        if (this.mGuiAssistConfig.itemBox.userHolsterData.ContainsKey(tActionId))
                        {
                            this.mGuiAssistConfig.itemBox.userHolsterData[tActionId] = iItem.Count;
                        }
                        else
                        {
                            this.mGuiAssistConfig.itemBox.userHolsterData.Add(tActionId, iItem.Count);
                        }
                        if (this.mGuiAssistConfig.itemBox.userHolsterDataByName.ContainsKey(tActionName))
                        {
                            this.mGuiAssistConfig.itemBox.userHolsterDataByName[tActionName] = iItem.Count;
                        }
                        else
                        {
                            this.mGuiAssistConfig.itemBox.userHolsterDataByName.Add(tActionName, iItem.Count);
                        }
                    }
                }
                foreach (MycItemCategory iCache in tData->ItemCaches)
                {
                    foreach (MycItem iItem in iCache.Items)
                    {
                        var tActionId = UtilsGameData.ConvertGameIdToInternalId_LostAction(iItem.ActionId);
                        if (this.mGuiAssistConfig.itemBox.userCacheData.ContainsKey(tActionId))
                        {
                            this.mGuiAssistConfig.itemBox.userCacheData[tActionId] = iItem.Count;
                        }
                        else
                        {
                            this.mGuiAssistConfig.itemBox.userCacheData.Add(tActionId, iItem.Count);
                        }
                    }
                }
                this.Save();
            }
        }

        public struct GuiAssistConfig
        {
            public ItemBox itemBox = new();
            public Overlay overlay = new();
            public ItemInfo itemInfo = new();
            public CharacterStats charStats = new();

            public GuiAssistConfig() { }
            public struct ItemInfo
            {
                public bool isDisabled_All = false;
                public bool isDisabled_WhenNotFocused = true;

                public ItemInfo() { }
            }
            public struct ItemBox
            {
                public bool isDisabled_All = false;
                public bool isDisabled_Toolbar = false;
                public bool isDisabled_LoadoutMiniview = false;
                public bool isDisabled_FilterText = false;
                public bool isDisabled_FilterLoadout = false;
                public bool isDisabled_AutoRoleFilter = false;

                public float refreshRate = 0.3f;
                public int filterTextLevel = 1;
                public int filterLoadoutLevel = 0;

                public float refreshRateDefault = 0.3f;

                public Dictionary<int, int> userCacheData = new();
                public Dictionary<int, int> userHolsterData = new();
                /// <summary> for when LostAction id is not available (e.g. crawling Add on). Not recommended. </summary>
                public Dictionary<string, int> userHolsterDataByName = new();

                public ItemBox() { }
            }
            public struct Overlay
            {
                public bool isUsingJobSpecific = false;
                public int currentSlotIndex = 0;

                public Overlay() { }
            }
            public struct CharacterStats
            {
                public bool isInit = false;

                public int rank = 0;
                public int mettle = 0;
                public int mettleMax = 0;
                public int proof = 0;
                public int cluster = 0;
                public int noto = 0;

                public int rayFortitude = 0;
                public int rayValor = 0;
                public int raySuccor = 0;

                public CharacterStats() { }
            }
        }
    }
}
