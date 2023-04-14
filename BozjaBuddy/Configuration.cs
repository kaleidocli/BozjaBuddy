using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using static BozjaBuddy.GUI.GUIAssist.GUIAssistManager;
using System.Collections.Generic;
using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;

namespace BozjaBuddy
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        // User data
        public LoadoutListJson? UserLoadouts = null;
        public List<List<Alarm>>? UserAlarms = null;

        public int Version { get; set; } = 0;
        public const int kDefaultAlarmDuration = 30;
        public const int kDefaultAlarmOffset = 30;
        public const float kDefaultVolume = 1.0f;

        public float STYLE_ICON_SIZE { get; set; } = 20f;
        public float mAudioVolume = Configuration.kDefaultVolume;
        public string? mAudioPath = null;
        public int mDefaultAlarmDuration = Configuration.kDefaultAlarmDuration;
        public int mDefaultAlarmOffset = Configuration.kDefaultAlarmOffset;
        public Dictionary<GUIAssistOption, bool> mOptionState = new();
        public bool mMuteAAudioOnGameFocused = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
