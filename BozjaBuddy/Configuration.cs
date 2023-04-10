using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using static BozjaBuddy.GUI.GUIAssist.GUIAssistManager;
using System.Collections.Generic;

namespace BozjaBuddy
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
        public float STYLE_ICON_SIZE { get; set; } = 20f;
        public float mAudioVolume = 1.0f;
        public string? mAudioPath = null;
        public Dictionary<GUIAssistOption, bool> mOptionState = new();

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
