using BozjaBuddy.Data;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using ImGuiNET;
using Dalamud.Logging;
using ImGuiScene;
using System.Numerics;
using System.Linq;

namespace BozjaBuddy.GUI.GUIAssist
{
    public class GUIAssistManager
    {
        private Plugin mPlugin;
        private Dictionary<GUIAssistOption, Action> mOptionFunctions = new();
        private Dictionary<GUIAssistOption, bool> mOptionStateDefault = new()
        {
            { GUIAssistOption.MycInfoBox , true }
        };
        private Dictionary<GUIAssistOption, HashSet<int>> mOptionRequests = new();

        private GUIAssistManager() { }
        public GUIAssistManager(Plugin pPlugin) 
        {
            this.mPlugin = pPlugin;

            this.mOptionFunctions[GUIAssistOption.MycInfoBox] = this.Draw_MycInfo;

            // update new options to config (only the one that is bound with an action)
            foreach (GUIAssistOption iOption in this.mOptionStateDefault.Keys)
            {
                if (!this.mPlugin.Configuration.mOptionState.ContainsKey(iOption))
                {
                    this.SetOptionState(iOption, this.mOptionStateDefault[iOption]);
                }
            }
        }

        /// <summary> Set if the GUI option should be enabled, regardless of visibility. Return false if option is not found </summary>
        private bool SetOptionState(GUIAssistOption pOption, bool pIsEnabled)
        {
            if (!this.mOptionFunctions.ContainsKey(pOption)) { return false; }
            if (!this.mPlugin.Configuration.mOptionState.ContainsKey(pOption))
            {
                this.mPlugin.Configuration.mOptionState.Add(pOption, pIsEnabled);
                return true;
            }
            this.mPlugin.Configuration.mOptionState[pOption] = pIsEnabled;
            return true;
        }
        /// <summary> Send a request to make the GUI option visible. </summary>
        public void RequestOption(int pSenderObjHash, GUIAssistOption pOption)
        {
            if (!this.mOptionRequests.ContainsKey(pOption))
            {
                this.mOptionRequests.Add(pOption, new HashSet<int>(new int[] { pSenderObjHash }));
                return;
            }
            if (!this.mOptionRequests[pOption].Contains(pSenderObjHash))
            {
                this.mOptionRequests[pOption].Add(pSenderObjHash);
            }
        }
        /// <summary> Send a request to make the GUI option invisible. </summary>
        public void UnrequestOption(int pSenderObjHash, GUIAssistOption pOption)
        {
            if (!this.mOptionRequests.ContainsKey(pOption)) { return; }
            this.mOptionRequests[pOption].Remove(pSenderObjHash);
        }

        public void Draw()
        {
            foreach (GUIAssistOption iOption in this.mPlugin.Configuration.mOptionState.Keys)
            {
                if (!this.mOptionRequests.ContainsKey(iOption))
                {
                    this.mOptionRequests.Add(iOption, new HashSet<int>());
                }
                if (this.mPlugin.Configuration.mOptionState[iOption] && this.mOptionRequests[iOption].Count > 0)
                {
                    this.mOptionFunctions[iOption]();
                }
            }
        }
        private void Draw_MycInfo()
        {
            unsafe
            {
                try
                {
                    var tAddonMycInfo = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCInfo");
                    var tAddonMycBattleAreaInfo = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCBattleAreaInfo");
                    if (tAddonMycInfo == null || tAddonMycBattleAreaInfo != null) return;

                    var tNode_Button = Utils.UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new[] { 36 });
                    if (tNode_Button == null) { return; }

                    float tCoordX = tNode_Button->ScreenX;
                    float tCoordY = tNode_Button->ScreenY;
                    float tSizeX = tNode_Button->Width * tAddonMycInfo->RootNode->GetScaleX();
                    float tSizeY = tNode_Button->Height * tAddonMycInfo->RootNode->GetScaleY();

                    //PluginLog.LogDebug($"> 1 :: coordX={tCoordX} coordY={tCoordY} sizeX={tSizeX} sizeY={tSizeY}");

                    ImGui.GetBackgroundDrawList().AddRect(
                        new Vector2(tCoordX, tCoordY),
                        new Vector2(tCoordX + tSizeX, tCoordY + tSizeY),
                        DateTime.Now.Second % 2 == 0
                            ? ImGui.ColorConvertFloat4ToU32(Utils.UtilsGUI.Colors.ActivatedText_Green)
                            : ImGui.ColorConvertFloat4ToU32(Utils.UtilsGUI.Colors.NormalText_Red),
                        1,
                        ImDrawFlags.None,
                        5
                        );
                    ImGui.GetBackgroundDrawList().AddText(
                        new Vector2(tCoordX, tCoordY + (float)(tSizeY * 1.2)),
                        ImGui.ColorConvertFloat4ToU32(Utils.UtilsGUI.Colors.ActivatedText_Green),
                        "BozjaBuddy: Keep this open for CE-related features.\n(To turn off: Config > UI hints > [A] > [1])"
                        );
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e.ToString());
                }
            }
        }

        public enum GUIAssistOption
        {
            None = 0,
            MycInfoBox = 1
        }
    }
}
