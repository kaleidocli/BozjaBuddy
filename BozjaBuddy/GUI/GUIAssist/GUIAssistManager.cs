using BozjaBuddy.Data;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using ImGuiNET;
using Dalamud.Logging;
using ImGuiScene;
using System.Numerics;
using System.Linq;
using Lumina.Excel.GeneratedSheets;
using BozjaBuddy.Utils;
using Lumina.Data.Parsing.Uld;

namespace BozjaBuddy.GUI.GUIAssist
{
    public class GUIAssistManager
    {
        private Plugin mPlugin;
        private Dictionary<GUIAssistOption, System.Action> mOptionFunctions = new();
        private Dictionary<GUIAssistOption, System.Action> mRestoreFunctions = new();
        private Dictionary<GUIAssistOption, HashSet<int>> mOptionRequests = new();
        private HashSet<GUIAssistOption> mRestoreRequests = new();
        private Dictionary<GUIAssistOption, bool> mOptionStateDefault = new()
        {
            { GUIAssistOption.MycInfoBox , true },
            { GUIAssistOption.MycItemBoxRoleFilter , true }
        };
        private DateTime _cycleOneSecond = DateTime.Now;
        private DateTime _cycle2 = DateTime.Now;
        private GUIAssistManager.GUIAssistStatusFlag mStatus = GUIAssistStatusFlag.None;

        private GUIAssistManager() { }
        public GUIAssistManager(Plugin pPlugin) 
        {
            this.mPlugin = pPlugin;

            this.mOptionFunctions[GUIAssistOption.MycInfoBox] = this.Draw_MycInfo;
            this.mOptionFunctions[GUIAssistOption.MycItemBoxRoleFilter] = this.Draw_IBFilterRole;

            this.mRestoreFunctions[GUIAssistOption.MycItemBoxRoleFilter] = this.Restore_IBFilterRole;

            // update new options to config (only the one that is bound with an action)
            foreach (GUIAssistOption iOption in this.mOptionStateDefault.Keys)
            {
                if (!this.mPlugin.Configuration.mOptionState.ContainsKey(iOption))
                {
                    this.SetOptionState(iOption, this.mOptionStateDefault[iOption]);
                }
            }

            // Init always-on GUIAssist
            this.RequestOption(this.GetHashCode(), GUIAssistOption.MycItemBoxRoleFilter);
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
        public void RequestRestore(GUIAssistOption pOption)
        {
            if (this.mRestoreRequests.Contains(pOption)) { return; }
            this.mRestoreRequests.Add(pOption);
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
            // Restore req
            //foreach (GUIAssistOption iOption in this.mRestoreFunctions.Keys)
            //{
            //    if (this.mRestoreRequests.Contains(iOption))
            //    {
            //        this.mRestoreFunctions[iOption]();
            //        this.mRestoreRequests.Remove(iOption);
            //    }
            //}
            if (this.mRestoreRequests.Contains(GUIAssistOption.MycItemBoxRoleFilter)
                && this.mPlugin.Configuration.mGUIAssist_IBFilterRoleLevel != 2)
            {
                this.mRestoreFunctions[GUIAssistOption.MycItemBoxRoleFilter]();
                this.mRestoreRequests.Remove(GUIAssistOption.MycItemBoxRoleFilter);
            }
        }
        private void Draw_MycInfo()
        {
            //PluginLog.LogDebug($"> GUIAssistMng: isMainFocus={this.mPlugin.WindowSystem.HasAnyFocus} hasInfoKey={this.mOptionRequests.ContainsKey(GUIAssistOption.MycInfoBox)} keys={(this.mOptionRequests.ContainsKey(GUIAssistOption.MycInfoBox) ? String.Join(", ", this.mOptionRequests[GUIAssistOption.MycInfoBox].ToList()) : 0)} keyAlmMng={this.mPlugin.AlarmManager.mHash}");
            if (!this.mPlugin.mIsMainWindowActive
                && this.mOptionRequests.ContainsKey(GUIAssistOption.MycInfoBox)
                && !this.mOptionRequests[GUIAssistOption.MycInfoBox].Contains(this.mPlugin.AlarmManager.mHash)
                    )
            {
                return;         // Abort when Main window is not active + AlarmManager is not requesting GUIAssist
            }
            // abort if user is in a raid, or a Fate/CE
            if ((DateTime.Now - this._cycleOneSecond).TotalSeconds > 1 && this.mPlugin.ClientState.LocalPlayer != null)
            {
                this._cycleOneSecond = DateTime.Now;
                var tStatusList = this.mPlugin.ClientState.LocalPlayer.StatusList.Select(s => s.StatusId);
                if (tStatusList.Contains((uint)StatusId.DutyAsAssigned))
                {
                    this.mStatus |= GUIAssistStatusFlag.InRaidCe;
                    return;
                }
                else
                {
                    this.mStatus &= ~GUIAssistStatusFlag.InRaidCe;
                }
            }
            if (this.mStatus.HasFlag(GUIAssistStatusFlag.InRaidCe)) { return; }

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
        private void Draw_IBFilterRole()
        {
            if ((DateTime.Now - this._cycle2).TotalSeconds < 0.5) return;
            unsafe
            {
                try
                {
                    var tAddonMycInfo = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCItemBox");
                    if (tAddonMycInfo == null) 
                    {
                        this._cycle2 = DateTime.Now;    // Set this on slower cycle if MYCItemBox is not active
                        return; 
                    }
                    this._cycle2 = DateTime.MinValue;
                    if (this.mPlugin.ClientState.LocalPlayer == null) return;
                    var tJob = this.mPlugin.ClientState.LocalPlayer!.ClassJob;
                    if (tJob == null) return;
                    Role tRole = Role.None;
                    tRole |= tJob.GameData!.Role switch
                    {
                        1 => Role.Tank,
                        2 => Role.Melee,
                        3 => tJob.GameData!.JobIndex == 2 ? Role.Range : Role.Caster,
                        4 => Role.Healer,
                        _ => ~tRole,
                    };

                    // Restore if possible
                    if (this.mPlugin.Configuration.mGUIAssist_IBFilterRoleLevel != 2)
                    {
                        this.RequestRestore(GUIAssistOption.MycItemBoxRoleFilter);
                    }

                    foreach (LostAction iLostAction in this.mPlugin.mBBDataManager.mLostActions.Values)
                    {
                        if (iLostAction.mUINode == null) continue;
                        if (iLostAction.mRole.mRoleFlagBit.HasFlag(tRole))
                        {
                            // lv2: Show node
                            if (this.mPlugin.Configuration.mGUIAssist_IBFilterRoleLevel == 2)
                            {
                                this.ShowNode(iLostAction.mUINode);
                            }
                        }
                        else
                        {
                            // lv1: Shadow over node
                            if (this.mPlugin.Configuration.mGUIAssist_IBFilterRoleLevel == 1)
                            {
                                this.ShadowNode(iLostAction.mUINode);
                            }
                            // lv2: Hide node
                            else if (this.mPlugin.Configuration.mGUIAssist_IBFilterRoleLevel == 2)
                            {
                                this.HideNode(iLostAction.mUINode);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e.ToString());
                }
            }
        }
        private void Restore_IBFilterRole()
        {
            unsafe
            {
                foreach (LostAction iLostAction in this.mPlugin.mBBDataManager.mLostActions.Values)
                {
                    if (iLostAction.mUINode != null) this.ShowNode(iLostAction.mUINode);
                }
            }
        }

        private void HideNode(UINode pUiNode)
        {
            unsafe
            {
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }

                tNode_Button->ToggleVisibility(false);
            }
        }
        private void ShowNode(UINode pUiNode)
        {
            unsafe
            {
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }

                tNode_Button->ToggleVisibility(true);
            }
        }
        private void ShadowNode(UINode pUiNode)
        {
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(pUiNode.mAddonName);
                if (tAddon == null) { return; }
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }

                float tCoordX = tNode_Button->ScreenX;
                float tCoordY = tNode_Button->ScreenY;
                float tSizeX = tNode_Button->Width * tAddon->RootNode->GetScaleX();
                float tSizeY = tNode_Button->Height * tAddon->RootNode->GetScaleY();

                ImGui.GetBackgroundDrawList().AddRectFilled(
                    new Vector2(tCoordX, tCoordY),
                    new Vector2(tCoordX + tSizeX, tCoordY + tSizeY),
                    ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.MycItemBoxOverlay_Black),
                    1,
                    ImDrawFlags.None
                    );
            }
        }

        public enum GUIAssistOption
        {
            None = 0,
            MycInfoBox = 1,
            MycItemBoxRoleFilter = 2
        }
        [Flags]
        private enum GUIAssistStatusFlag
        {
            None = 0,
            InRaidCe = 1
        }
    }
}
