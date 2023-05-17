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
using Dalamud.Game.Gui.PartyFinder.Types;
using System.ComponentModel.Design.Serialization;
using BozjaBuddy.GUI.GUIExtension;
using System.Security.Cryptography;
using static Lumina.Data.Parsing.Uld.NodeData;
using Dalamud.Utility;

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
            { GUIAssistOption.MycInfoBoxAlarm , true },
            { GUIAssistOption.MycItemBoxRoleFilter , true },
            { GUIAssistOption.MycInfoBox , true }
        };
        private DateTime _cycleOneSecond = DateTime.Now;
        private DateTime _cycle2 = DateTime.Now;
        private DateTime _cycle3 = DateTime.Now;
        private GUIAssistManager.GUIAssistStatusFlag mStatus = GUIAssistStatusFlag.None;

        private ExtGui_MycItemBox mExtGui_MycItemBox;
        private ExtGui_MycInfo mExtGui_MycInfo;
        private HashSet<int> _MycItemBagTrade_ValidNodeIds = new HashSet<int>(new int[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12});

        private GUIAssistManager() { }
        public GUIAssistManager(Plugin pPlugin) 
        {
            this.mPlugin = pPlugin;

            this.mOptionFunctions[GUIAssistOption.MycInfoBoxAlarm] = this.Draw_MycInfoAlarm;
            this.mOptionFunctions[GUIAssistOption.MycItemBoxRoleFilter] = this.Draw_MycItemBox;
            this.mOptionFunctions[GUIAssistOption.MycInfoBox] = this.Draw_MycInfo;

            this.mRestoreFunctions[GUIAssistOption.MycItemBoxRoleFilter] = this.Restore_MycItemBox;

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
            this.RequestOption(this.GetHashCode(), GUIAssistOption.MycInfoBox);

            // ExtGui
            this.mExtGui_MycItemBox = new(this.mPlugin);
            this.mExtGui_MycInfo = new(this.mPlugin);
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
            Configuration.GuiAssistConfig tGaConfig = this.mPlugin.Configuration.mGuiAssistConfig;

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
            // Restore req (only use when every is back to default, as a clean up measure)
            if (this.mRestoreRequests.Contains(GUIAssistOption.MycItemBoxRoleFilter))
            {
                this.mRestoreFunctions[GUIAssistOption.MycItemBoxRoleFilter]();
                this.mRestoreRequests.Remove(GUIAssistOption.MycItemBoxRoleFilter);
            }
        }
        private void Draw_MycInfo()
        {
            if (this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_All) return;
            // Extension GUI
            try
            {
                HelperUtil.DrawHelper(
                    this.mPlugin,
                    this.mExtGui_MycInfo,
                    new Vector2(0, ImGui.CalcTextSize("A").Y),
                    padding: new Vector2(0, -ImGui.CalcTextSize("A").Y),
                    isDisabled: (this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_WhenNotFocused && !UtilsGUI.IsAddonFocused("MYCInfo"))
                    );
            }
            catch (Exception e) { PluginLog.LogDebug(e.Message); }
        }
        private void Draw_MycInfoAlarm()
        {
            if (this.mPlugin.Configuration.mGuiAssistConfig.itemInfo.isDisabled_All) return;
            //PluginLog.LogDebug($"> GUIAssistMng: isMainFocus={this.mPlugin.WindowSystem.HasAnyFocus} hasInfoKey={this.mOptionRequests.ContainsKey(GUIAssistOption.MycInfoBox)} keys={(this.mOptionRequests.ContainsKey(GUIAssistOption.MycInfoBox) ? String.Join(", ", this.mOptionRequests[GUIAssistOption.MycInfoBox].ToList()) : 0)} keyAlmMng={this.mPlugin.AlarmManager.mHash}");
            if (!this.mPlugin.mIsMainWindowActive
                && this.mOptionRequests.ContainsKey(GUIAssistOption.MycInfoBoxAlarm)
                && !this.mOptionRequests[GUIAssistOption.MycInfoBoxAlarm].Contains(this.mPlugin.AlarmManager.mHash)
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
                        "BozjaBuddy: Keep this open for CE-related features.\n(To turn off: Config > UI hints > [A] > [2])"
                        );
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e.ToString());
                }
            }
        }
        private void Draw_MycItemBox()
        {
            if ((DateTime.Now - this._cycle2).TotalSeconds < this.mPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRate) return;
            unsafe
            {
                var tAddonMycItemBox = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCItemBox");

                // Update user's Cache and Holster saved info (only when the Box is opened, and every 1 sec)
                if (tAddonMycItemBox != null && (DateTime.Now - this._cycle3).TotalSeconds > this.mPlugin.Configuration.mGuiAssistConfig.itemBox.refreshRate)
                {
                    this.mPlugin.Configuration.UpdateCacheHolsterInfo(this.mPlugin);
                    this._cycle3 = DateTime.Now;
                }

                var tConfig = this.mPlugin.Configuration;
                // Extension GUI: 
                try
                {
                    HelperUtil.DrawHelper(
                        this.mPlugin,
                        this.mExtGui_MycItemBox,
                        new Vector2(0, ImGui.CalcTextSize("A").Y),
                        padding: new Vector2(0, -ImGui.CalcTextSize("A").Y));
                }
                catch (Exception e) { PluginLog.LogDebug(e.Message); }
                if (tConfig.mGuiAssistConfig.itemBox.isDisabled_All) return;

                // Applying filters to the GUI
                Loadout? tLoadout = tConfig.GetActiveOverlay(this.mPlugin);
                var tItemBoxCfg = tConfig.mGuiAssistConfig.itemBox;
                try
                {
                    if (tAddonMycItemBox == null) 
                    {
                        this._cycle2 = DateTime.Now;    // Set this on slower cycle if MYCItemBox is not active
                        return; 
                    }
                    this._cycle2 = DateTime.MinValue;
                    Role tRole = UtilsGameData.GetUserRole(this.mPlugin) ?? Role.None;

                    // Filtering. Overwriting priority: Overlay > Toolbar filter
                    int tTextFilterLevel = tConfig.mGuiAssistConfig.itemBox.isDisabled_FilterText ? 0 : tConfig.mGuiAssistConfig.itemBox.filterTextLevel;
                    int tLoadoutFilterLevel = tConfig.mGuiAssistConfig.itemBox.isDisabled_FilterLoadout ? 0 : tConfig.mGuiAssistConfig.itemBox.filterLoadoutLevel;
                    foreach (LostAction iLostAction in this.mPlugin.mBBDataManager.mLostActions.Values)
                    {
                        bool tIsFilteredIn = false;
                        if (iLostAction.mUINode == null) continue;
                        // Filters
                        if (this.mExtGui_MycItemBox.CanPassAllFilters(iLostAction))
                        {
                            tIsFilteredIn = true;
                            switch (tTextFilterLevel)
                            {
                                case 1: this.UnfadeNode(iLostAction.mUINode); break;
                                case 2: this.ShowNode(iLostAction.mUINode); break;
                                default: break;
                            }
                        }
                        else
                        {
                            switch (tTextFilterLevel)
                            {
                                case 1:
                                    this.FadeNode(iLostAction.mUINode); break;
                                case 2:
                                    this.HideNode(iLostAction.mUINode); break;
                                default: break;
                            }
                        }
                        // Overlay
                        Job? tUserJob = tConfig.mGuiAssistConfig.overlay.isUsingJobSpecific
                                        ? UtilsGameData.GetUserJob(this.mPlugin)        // use overlay of user's curr job
                                        : Job.ALL;                                      // use overlay of Job.ALL
                        int tAmountHolster = tItemBoxCfg.userHolsterData.ContainsKey(iLostAction.mId) 
                                             ? tItemBoxCfg.userHolsterData[iLostAction.mId]
                                             : 0;
                        if (tUserJob != null)      
                        {
                            if (tLoadout != null
                                && tLoadout.mActionIds.Count != 0)
                            {
                                int tAmountLoadout = tLoadout.mActionIds.ContainsKey(iLostAction.mId) ? tLoadout.mActionIds[iLostAction.mId] : 0;
                                if (tLoadout.mActionIds.ContainsKey(iLostAction.mId))
                                {
                                    switch (tLoadoutFilterLevel)
                                    {
                                        case 1: 
                                            if (tAmountHolster < tAmountLoadout)
                                            {
                                                this.HighlightNode(iLostAction.mUINode, pIsFlashing: true);
                                                this.DisplayTextOnNode(iLostAction.mUINode, (tAmountLoadout - tAmountHolster).ToString());
                                            }
                                            this.UnfadeNode(iLostAction.mUINode);
                                            this.ShowNode(iLostAction.mUINode); 
                                            break;
                                        case 2:
                                            if (tAmountHolster < tAmountLoadout)
                                            {
                                                this.HighlightNode(iLostAction.mUINode, pIsFlashing: true);
                                                this.DisplayTextOnNode(iLostAction.mUINode, (tAmountLoadout - tAmountHolster).ToString());
                                            }
                                            this.UnfadeNode(iLostAction.mUINode);
                                            this.ShowNode(iLostAction.mUINode); 
                                            break;
                                        default: break;
                                    }
                                }
                                else
                                {
                                    switch (tLoadoutFilterLevel)
                                    {
                                        case 0:
                                            if (tIsFilteredIn) 
                                            {
                                                this.ShowNode(iLostAction.mUINode);
                                                this.UnfadeNode(iLostAction.mUINode);
                                            }
                                            else
                                            {
                                                switch (tTextFilterLevel)
                                                {
                                                    case 0: this.UnfadeNode(iLostAction.mUINode); break;
                                                    case 1: this.ShowNode(iLostAction.mUINode); break;
                                                }
                                            }
                                            break;
                                        case 1:
                                            if (tIsFilteredIn)     // only the one filtered in
                                            {
                                                this.ShowNode(iLostAction.mUINode);
                                            }
                                            else
                                            {
                                                switch (tTextFilterLevel)
                                                {
                                                    case 0: this.UnfadeNode(iLostAction.mUINode); break;
                                                    case 1: this.ShowNode(iLostAction.mUINode); break;
                                                }
                                            }

                                            break;
                                        case 2:                    // ignore the one filtered in while EITHER T_filter having input OR T_filter being disabled
                                            if (tTextFilterLevel == 0
                                                || !(tIsFilteredIn && this.mExtGui_MycItemBox.IsAnyFilterActive(iLostAction)))
                                            {
                                                this.HideNode(iLostAction.mUINode);
                                            }
                                            else
                                            {
                                                this.ShowNode(iLostAction.mUINode);
                                            }
                                            break;
break;
                                        default: break;
                                    }
                                }
                            }
                            else if (tLoadout == null && tIsFilteredIn)
                            {
                                this.ShowNode(iLostAction.mUINode);
                                this.UnfadeNode(iLostAction.mUINode); 
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e.ToString());
                }

                this.Draw_MycItemBagTrade(tLoadout, tItemBoxCfg.userHolsterDataByName);
            }
        }
        private void Draw_MycItemBagTrade(Loadout? pLoadout, Dictionary<string, int> pUserHolsterDataByName)
        {
            //if (pLoadout == null) { return; }
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCItemBagTrade");
                if (tAddon == null) { return; }
                var tTreeList = (AtkComponentNode*)tAddon->GetNodeById(18);
                if (tTreeList == null) { return; }
                for (int i = 0; i < tTreeList->Component->UldManager.NodeListCount; i++)
                {
                    var n = tTreeList->Component->UldManager.NodeList[i];
                    if (n == null) { continue; }
                    var nCompNode = n->GetAsAtkComponentNode();
                    if (nCompNode == null) { continue; }
                    if (nCompNode->Component->UldManager.NodeListCount != 7 && nCompNode->Component->UldManager.NodeListCount != 8) { continue; }   // Only item node has 8 pr 7 child
                    int? tNodeActionId = null;      // this var is just a dud.
                    string tNodeActionName = "";

                    // If loadout == null, by default set this node visible and unfade
                    if (pLoadout == null)
                    {
                        if (nCompNode->Component == null) { continue; }
                        var ttNodeIcon = GetNodeByID<AtkResNode>(nCompNode->Component->UldManager, 2);
                        if (ttNodeIcon == null) { continue; }
                        ttNodeIcon->ToggleVisibility(true);
                        ttNodeIcon->Color.A = 255;
                        continue;
                    }
                    // Confirmed item node. Check item node for existence and is loadout-included.
                    if (n->NodeID == 4
                        || (n->NodeID > 100 && n->NodeID % 100 > 0 && n->NodeID % 100 < 13))        // Check range of node
                    {
                        if (nCompNode->Component == null) { continue; }
                        var tTextNode = GetNodeByID<AtkTextNode>(
                            nCompNode->Component->UldManager,
                            (uint)(nCompNode->Component->UldManager.NodeListCount == 7 ? 5 : 6),
                            NodeType.Text);
                        if (tTextNode == null) { continue; }
                        tNodeActionName = tTextNode->NodeText.ToString();
                        if (tNodeActionName.IsNullOrEmpty()) { continue; }

                        // Crosscheck with Agent (if the action is really exist in the holster)
                        if (!(pUserHolsterDataByName.ContainsKey(tNodeActionName)
                            && pUserHolsterDataByName[tNodeActionName] != 0))
                        {
                            continue;
                        }

                        // Crosscheck with Loadout (if the action is in loadout. We need the one that is not in loadout)
                        foreach (int iActionId in pLoadout.mActionIds.Keys)
                        {
                            if (this.mPlugin.mBBDataManager.mLostActions[iActionId].mName == tNodeActionName)
                            {
                                tNodeActionId = this.mPlugin.mBBDataManager.mLostActions[iActionId].mId;
                                break;
                            }
                        }
                    }
                    else continue;
                    // Apply fx
                    var tNodeIcon = GetNodeByID<AtkResNode>(nCompNode->Component->UldManager, 2);
                    if (tNodeIcon == null) { continue; }
                    if (!tNodeActionId.HasValue)                 // Item exists + is not in loadout
                    {
                        //PluginLog.LogDebug($"> node={tNodeActionName} ||||| Vis OFF");
                        switch (this.mPlugin.Configuration.mGuiAssistConfig.itemBox.filterLoadoutLevel)
                        {
                            case 0: tNodeIcon->Color.A = 255; tNodeIcon->ToggleVisibility(true); break;
                            case 1: tNodeIcon->ToggleVisibility(true); tNodeIcon->Color.A = 60; break;
                            case 2: tNodeIcon->ToggleVisibility(false); break;
                            default: break;
                        }
                    }
                    else
                    {
                        //PluginLog.LogDebug($"> node={tNodeActionName} ||||| Vis ON");
                        tNodeIcon->ToggleVisibility(true);
                        tNodeIcon->Color.A = 255;
                    }
                }
            }
        }
        private void Restore_MycItemBox()
        {
            Configuration.GuiAssistConfig tGaConfig = this.mPlugin.Configuration.mGuiAssistConfig;
            unsafe
            {
                foreach (LostAction iLostAction in this.mPlugin.mBBDataManager.mLostActions.Values)
                {
                    if (iLostAction.mUINode != null)
                    {
                        if (tGaConfig.itemBox.filterLoadoutLevel == 1 && tGaConfig.itemBox.filterTextLevel == 1)
                        {
                            this.ShowNode(iLostAction.mUINode);
                            this.UnfadeNode(iLostAction.mUINode);
                        }
                        else if (tGaConfig.itemBox.filterLoadoutLevel == 2 && tGaConfig.itemBox.filterTextLevel == 2)
                        {
                            this.UnfadeNode(iLostAction.mUINode);
                        }
                        else if(tGaConfig.itemBox.filterLoadoutLevel == 0 && tGaConfig.itemBox.filterTextLevel == 0)
                        {
                            this.ShowNode(iLostAction.mUINode); 
                            this.UnfadeNode(iLostAction.mUINode);
                        }
                    }
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
        private void FadeNode(UINode pUiNode, Vector4? pColor = null)
        {
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(pUiNode.mAddonName);
                if (tAddon == null) { return; }
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }
                var tIconNode = tNode_Button->GetComponent()->UldManager.SearchNodeById(5);
                if (tIconNode == null) { return; }
                tIconNode->Color.A = 60;

                //float tCoordX = tNode_Button->ScreenX;
                //float tCoordY = tNode_Button->ScreenY;
                //float tSizeX = tNode_Button->Width * tAddon->RootNode->GetScaleX();
                //float tSizeY = tNode_Button->Height * tAddon->RootNode->GetScaleY();

                //ImGui.GetBackgroundDrawList().AddRectFilled(
                //    new Vector2(tCoordX, tCoordY),
                //    new Vector2(tCoordX + tSizeX, tCoordY + tSizeY),
                //    ImGui.ColorConvertFloat4ToU32(pColor ?? UtilsGUI.Colors.MycItemBoxOverlay_Black),
                //    1,
                //    ImDrawFlags.None
                //    );
            }
        }
        private void UnfadeNode(UINode pUiNode, Vector4? pColor = null)
        {
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(pUiNode.mAddonName);
                if (tAddon == null) { return; }
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }
                var tIconNode = tNode_Button->GetComponent()->UldManager.SearchNodeById(5);
                if (tIconNode == null) { return; }
                tIconNode->Color.A = 255;
            }
        }
        private void HighlightNode(UINode pUiNode, bool pIsFlashing = false)
        {
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(pUiNode.mAddonName);
                if (tAddon == null) { return; }
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }

                float tCoordX = tNode_Button->ScreenX;
                float tCoordY = tNode_Button->ScreenY;
                float tSizeX = (tNode_Button->Width - 4) * tAddon->RootNode->GetScaleX();
                float tSizeY = (tNode_Button->Height - 4) * tAddon->RootNode->GetScaleY();

                ImGui.GetBackgroundDrawList().AddRect(
                    new Vector2(tCoordX, tCoordY) + new Vector2(-2),
                    new Vector2(tCoordX + tSizeX, tCoordY + tSizeY) + new Vector2(2.5f),
                    pIsFlashing
                        ? DateTime.Now.Second % 2 == 0 
                            ? ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.MycItemBoxOverlay_Gold)
                            : ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.ActivatedText_Green)
                        : ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.ActivatedText_Green),
                    2,
                    ImDrawFlags.None,
                    3
                    );
            }
        }
        private void DisplayTextOnNode(UINode pUiNode, string pText)
        {
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(pUiNode.mAddonName);
                if (tAddon == null) { return; }
                var tNode_Button = UtilsGUI.GetNodeByIdPath(this.mPlugin, pUiNode.mAddonName, new int[] { pUiNode.mNodePath[^1] });
                if (tNode_Button == null) { return; }

                float tCoordX = tNode_Button->ScreenX;
                float tCoordY = tNode_Button->ScreenY;
                float tSizeX = (tNode_Button->Width - 4) * tAddon->RootNode->GetScaleX();
                float tSizeY = (tNode_Button->Height - 4) * tAddon->RootNode->GetScaleY();
                float tScale = tAddon->Scale;

                if (UtilsGameData.kFont_Yuruka.NativePtr == null) { PluginLog.LogDebug("> FONT NULLL!"); return; }
                ImGui.GetBackgroundDrawList().AddText(
                        UtilsGameData.kFont_Yuruka,
                        23.5f * tScale,
                        new Vector2(tCoordX, tCoordY) + new Vector2(0, tSizeY / 2) + new Vector2(-1, 4),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalText_OrangeDark),
                        pText
                    );
                ImGui.GetBackgroundDrawList().AddText(
                        UtilsGameData.kFont_Yuruka,
                        16 * tScale,
                        new Vector2(tCoordX, tCoordY) + new Vector2(2, tSizeY / 2 + 2) + new Vector2(-1, 4),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalText_Orange),
                        pText
                    );
            }
        }

        /// <summary> Modified. From Simple Tweaks plugin, by Caraxi. https://discord.com/channels/581875019861328007/653504487352303619/943056251376533535 </summary>
        public unsafe static T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged
        {
            for (var i = 0; i < uldManager.NodeListCount; i++)
            {
                var n = uldManager.NodeList[i];
                if (n->NodeID != nodeId || type != null && n->Type != type.Value) continue;
                return (T*)n;
            }
            return null;
        }

        public enum GUIAssistOption
        {
            None = 0,
            MycInfoBoxAlarm = 1,
            MycItemBoxRoleFilter = 2,
            MycInfoBox = 3
        }
        [Flags]
        private enum GUIAssistStatusFlag
        {
            None = 0,
            InRaidCe = 1
        }
    }
}
