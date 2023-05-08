using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.GUI;
using BozjaBuddy.GUI.Sections;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Utils
{
    public class UtilsGUI
    {
        private const float FRAME_ROUNDING = 0;
        private static readonly Vector2 FRAME_PADDING = new(10f, 2f);
        private static DateTime kTimeSinceLastClipboardCopied = DateTime.Now;
        
        // https://www.programcreek.com/cpp/?code=kswaldemar%2Frewind-viewer%2Frewind-viewer-master%2Fsrc%2Fimgui_impl%2Fimgui_widgets.cpp
        public static void ShowHelpMarker(string desc, string markerText = "(?)", bool disabled = true)
        {
            if (disabled)
                ImGui.TextDisabled(markerText);
            else
                ImGui.TextUnformatted(markerText);
            UtilsGUI.SetTooltipForLastItem(desc);
        }
        public static void SetTooltipForLastItem(string tDesc, float tSize = 450.0f)
        {
            if (!ImGui.IsItemHovered()) return;

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(tSize);
            ImGui.TextUnformatted(tDesc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
        public static void TextWithHelpMarker(string pText, string pHelpMarkerText = "", Vector4? pColor = null)
        {
            if (pColor != null)
                ImGui.TextColored(pColor.Value, pText);
            else
                ImGui.Text(pText);
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker(pHelpMarkerText);
        }
        public static void TextDescriptionForWidget(string pText)
        {
            ImGui.TextColored(BozjaBuddy.Utils.UtilsGUI.Colors.BackgroundText_Grey, pText);
        }
        public static bool SelectableLink(Plugin pPlugin, string pContent, int pTargetGenId, bool pIsWrappedToText = true, bool pIsClosingPUOnClick = true)
        {
            bool tRes = false;
            ImGui.PushID(pTargetGenId);
            var tTextSize = ImGui.CalcTextSize(pContent);
            if (pIsWrappedToText)
            {
                if (ImGui.Selectable(pContent,
                                        false,
                                        pIsClosingPUOnClick ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.DontClosePopups, 
                                        new System.Numerics.Vector2(tTextSize.X + 0.5f, tTextSize.Y + 0.25f) 
                                        ))
                {
                    tRes = true;
                    if (!AuxiliaryViewerSection.mTabGenIds[pTargetGenId])
                    {
                        AuxiliaryViewerSection.AddTab(pPlugin, pTargetGenId);
                    }
                    else
                    {
                        AuxiliaryViewerSection._mGenIdToTabFocus = pTargetGenId;
                    }
                }
            }
            else if (ImGui.Selectable(pContent, false, pIsClosingPUOnClick ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.DontClosePopups))
            {
                tRes = true;
                if (!AuxiliaryViewerSection.mTabGenIds[pTargetGenId])
                {
                    AuxiliaryViewerSection.AddTab(pPlugin, pTargetGenId);
                }
                else
                {
                    AuxiliaryViewerSection._mGenIdToTabFocus = pTargetGenId;
                }
            }
            ImGui.PopID();
            return tRes;
        }
        /// <summary> Return true if link is clicked with LMB or RMB </summary>
        public static bool SelectableLink_WithPopup(Plugin pPlugin, string pContent, int pTargetGenId, bool pIsWrappedToText = true, bool pIsClosingPUOnClick = true)
        {
            bool tRes = UtilsGUI.SelectableLink(pPlugin, pContent + "  »", pTargetGenId, pIsWrappedToText, pIsClosingPUOnClick: pIsClosingPUOnClick);
            if (!pPlugin.mBBDataManager.mGeneralObjects.ContainsKey(pTargetGenId))
            {
                ImGui.Text("<unrecognizable obj>");
                return tRes;
            }
            GeneralObject tObj = pPlugin.mBBDataManager.mGeneralObjects[pTargetGenId];
            UtilsGUI.SetTooltipForLastItem($"[LMB] Show details\t\t[RMB] Show options\n===================================\n{tObj.GetReprUiTooltip()}");

            if (ImGui.BeginPopupContextItem(pContent, ImGuiPopupFlags.MouseButtonRight))
            {
                tRes = true;
                ImGui.BeginGroup();
                // Item link to Clipboard + Chat
                UtilsGUI.ItemLinkButton(pPlugin, pReprName: tObj.GetReprName(), pReprItemLink: tObj.GetReprItemLink());
                ImGui.Separator();
                // Clipboard sypnosis
                UtilsGUI.SypnosisClipboardButton(tObj.GetReprClipboardTooltip());
                ImGui.Separator();
                // Map_link to Clipboard + Chat
                var tLocation = tObj.GetReprLocation();
                UtilsGUI.LocationLinkButton(pPlugin, tLocation!, pDesc: "Link position", pIsDisabled: tLocation == null ? true : false);
                ImGui.Separator();
                // Invoke: Marketboard
                int tFragId = tObj.mId;
                if (tObj.GetSalt() == GeneralObject.GeneralObjectSalt.LostAction
                    && tObj.mLinkFragments.Count != 0)
                {
                    if (pPlugin.mBBDataManager.mFragments.ContainsKey(tObj.mLinkFragments[0]))
                    {
                        tFragId = pPlugin.mBBDataManager.mFragments[tObj.mLinkFragments[0]].mId;
                    }
                }
                UtilsGUI.MarketboardButton(
                    pPlugin, 
                    tFragId,
                    pIsDisabled: tObj.GetSalt() == GeneralObject.GeneralObjectSalt.Fragment
                                 || tObj.GetSalt() == GeneralObject.GeneralObjectSalt.LostAction
                                    ? false 
                                    : true);
                ImGui.EndGroup();

                ImGui.SameLine();
                // ACPU button
                UtilsGUI.ACPUFateCeButton(pPlugin, tObj.mId, tObj.mName, pIsDisabled: tObj.GetSalt() == GeneralObject.GeneralObjectSalt.Fate ? false : true);

                ImGui.EndPopup();
            }
            else if (tRes)
            {
                pPlugin.WindowSystem.GetWindow("Bozja Buddy")!.IsOpen = true;
            }
            return tRes;
        }
        public static void SypnosisClipboardButton(string pSypnosis)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, UtilsGUI.FRAME_ROUNDING);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, UtilsGUI.FRAME_PADDING);
            if (ImGui.Button("Copy quick info"))
            {
                ImGui.SetClipboardText(pSypnosis);
                UtilsGUI.kTimeSinceLastClipboardCopied = DateTime.Now;
            }
            if ((DateTime.Now - UtilsGUI.kTimeSinceLastClipboardCopied).TotalSeconds < 5)
                UtilsGUI.SetTooltipForLastItem("Copied to clipboard");
            else
                UtilsGUI.SetTooltipForLastItem("Copy to clipboard general info\ne.g. name, location, mettle & exp reward, etc.");
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
        public static void ItemLinkButton(Plugin pPlugin, string pReprName, SeString? pReprItemLink = null)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, UtilsGUI.FRAME_ROUNDING);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, UtilsGUI.FRAME_PADDING);
            if (ImGui.Button("Link item"))
            {
                UtilsGUI.kTimeSinceLastClipboardCopied = DateTime.Now;
                ImGui.SetClipboardText(pReprName);

                if (pReprItemLink == null)
                {
                    ImGui.PopStyleVar();
                    ImGui.PopStyleVar();
                    return;
                }
                try
                {
                    pPlugin.ChatGui.PrintChat(new Dalamud.Game.Text.XivChatEntry { Message = pReprItemLink });
                }
                catch (Exception e) { PluginLog.LogError(e.Message); }
            }
            if ((DateTime.Now - UtilsGUI.kTimeSinceLastClipboardCopied).TotalSeconds < 5)
                UtilsGUI.SetTooltipForLastItem("Copied to clipboard");
            else
                UtilsGUI.SetTooltipForLastItem("Link the item to chat. Also copy the item name to clipboard.");
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
        public static void LocationLinkButton(Plugin pPlugin, Location pLocation, bool rightAlign = false, bool pUseIcon = false, string? pDesc = null, bool pIsDisabled = false, float pRightAlignOffset = 0f)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, UtilsGUI.FRAME_ROUNDING);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, UtilsGUI.FRAME_PADDING);
            string tButtonText = pDesc ?? $"{pLocation.mAreaFlag} ({pLocation.mMapCoordX}, {pLocation.mMapCoordY})";
            if (rightAlign)
            {
                AuxiliaryViewerSection.GUIAlignRight(ImGui.CalcTextSize(tButtonText).X + pRightAlignOffset);
            }
            if (pIsDisabled)
            {
                ImGui.BeginDisabled();
                if (pUseIcon)
                    ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.MapMarkerAlt);
                else 
                    ImGui.Button(tButtonText);
                ImGui.EndDisabled();
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                return;
            }
            if (pUseIcon
                ? ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.MapMarkerAlt)
                : ImGui.Button(tButtonText)
                )
            {
                pPlugin.GameGui.OpenMapWithMapLink(
                    new Dalamud.Game.Text.SeStringHandling.Payloads.MapLinkPayload(
                        pLocation.mTerritoryID,
                        pLocation.mMapID,
                        (float)pLocation.mMapCoordX,
                        (float)pLocation.mMapCoordY)
                    );
                // link to chat
                pPlugin.ChatGui.PrintChat(new Dalamud.Game.Text.XivChatEntry
                {
                    Message = SeString.CreateMapLink(pLocation.mTerritoryID,
                            pLocation.mMapID,
                            (float)pLocation.mMapCoordX,
                            (float)pLocation.mMapCoordY)
                });
                //PluginLog.LogInformation($"Showing map: {pLocation.mTerritoryID} - {pLocation.mMapID} - {(float)pLocation.mMapCoordX} - {(float)pLocation.mMapCoordY}");
            }
            UtilsGUI.SetTooltipForLastItem("Mark position on map + Link location to Chat (if available)");
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
        public static void MarketboardButton(Plugin pPlugin, int pItemId, bool pIsDisabled = false)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, UtilsGUI.FRAME_PADDING);
            if (pIsDisabled)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Marketboard");
                ImGui.EndDisabled();
                ImGui.PopStyleVar();
                return;
            }
            if (ImGui.Button("Marketboard"))
            {
                pPlugin.CommandManager.ProcessCommand($"/pmb {pItemId}");
            }
            UtilsGUI.SetTooltipForLastItem("Look up marketboard. (Requires plugin [Market board] by <fmauNeko>)");
            ImGui.PopStyleVar();
        }
        public static void WindowLinkedButton(Plugin pPlugin, string pWinHandle, FontAwesomeIcon pButtonIcon, string? pHoveredText = null)
        {
            if (ImGuiComponents.IconButton(pButtonIcon))
            {
                pPlugin.WindowSystem.GetWindow(pWinHandle)!.IsOpen = !(pPlugin.WindowSystem.GetWindow(pWinHandle)!.IsOpen);
            }
            if (pHoveredText != null && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(pHoveredText);
                ImGui.EndTooltip();
            }
        }
        public static void WindowLinkedButton(Plugin pPlugin, string pWinHandle, string pButtonText, string? pHoveredText = null)
        {
            if (ImGui.Button(pButtonText))
            {
                pPlugin.WindowSystem.GetWindow(pWinHandle)!.IsOpen = !(pPlugin.WindowSystem.GetWindow(pWinHandle)!.IsOpen);
            }
            if (pHoveredText != null && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(pHoveredText);
                ImGui.EndTooltip();
            }
        }
        public static void ACPUFateCeButton(Plugin pPlugin, int pId, string pName, bool pIsDisabled = false)
        {
            if (pIsDisabled)
            {
                ImGui.BeginDisabled();
                ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Bell);
                ImGui.EndDisabled();
                return;
            }
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Bell))
            {
                GUIAlarm.CreateACPU(
                    pId.ToString(),
                    pNameSuggestion: $"Fate/CE {Alarm.GetIdCounter()}: {pName} (fateId={pId})",
                    pDefaultDuration: pPlugin.Configuration.mDefaultAlarmDuration,
                    pDefaultOffset: pPlugin.Configuration.mDefaultAlarmOffset);
            }
            GUIAlarm.DrawACPU_FateCe(
                pPlugin,
                pId.ToString(),
                pId
                );
            UtilsGUI.SetTooltipForLastItem("Set an alarm for this Fate/CE");
        }
        public static bool Checkbox(string pDesc, ref bool tValue, int? pGuiId = null)
        {
            bool tRes = false;
            if (pGuiId != null) ImGui.PushID(pGuiId.Value);
            tRes = ImGui.Checkbox("##cb", ref tValue);
            ImGui.SameLine();
            ImGui.TextColored(Colors.BackgroundText_Grey, pDesc);
            if (pGuiId != null) ImGui.PopID();
            return tRes;
        }
        public static void DrawRoleFlagAsIconString(Plugin pPlugin, RoleFlag pRoleFlag)
        {
            List<TextureWrap?> tRoleIcons = RoleFlag.FlagToIcon(pRoleFlag.mRoleFlagBit);
            int tDrawCounter = 0;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
            foreach (TextureWrap? iIcon in tRoleIcons)
            {
                if (iIcon == null) continue;
                if (tDrawCounter != 0) ImGui.SameLine();
                ImGui.Image(iIcon.ImGuiHandle, Utils.ResizeToIcon(pPlugin, ImGui.GetTextLineHeight() / 2, ImGui.GetTextLineHeight() / 2));
                UtilsGUI.SetTooltipForLastItem(tDrawCounter switch
                {
                    0 => "Tank",
                    1 => "Healer",
                    2 => "Melee",
                    3 => "Range",
                    4 => "Caster",
                    _ => "None"
                });
                tDrawCounter++;
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
        public static void DrawRoleFlagAsString(RoleFlag pRole) => ImGui.Text(RoleFlag.FlagToString(pRole.mRoleFlagBit));
        public static Vector4 AdjustTransparency(Vector4 pColor, float pTransparency) => new(pColor.X, pColor.Y, pColor.Z, pTransparency);

        public unsafe static AtkResNode* GetNodeByIdPath(AtkUnitBase* pAddonBase, int[] pNoteIdPath)
        {
            return UtilsGUI.GetNodeByIdPath(pAddonBase, new Queue(pNoteIdPath));
        }
        public unsafe static AtkResNode* GetNodeByIdPath(Plugin pPlugin, string pAddonName, int[] pNoteIdPath)
        {
            return UtilsGUI.GetNodeByIdPath(pPlugin, pAddonName, new Queue(pNoteIdPath));
        }
        public unsafe static AtkResNode* GetNodeByIdPath(Plugin pPlugin, string pAddonName, List<int> pNoteIdPath)
        {
            return UtilsGUI.GetNodeByIdPath(pPlugin, pAddonName, new Queue(pNoteIdPath));
        }
        private unsafe static AtkResNode* GetNodeByIdPath(Plugin pPlugin, string pAddonName, Queue pNoteIdPath)
        {
            AtkUnitBase* tAddon = (AtkUnitBase*)pPlugin.GameGui.GetAddonByName(pAddonName);

            return UtilsGUI.GetNodeByIdPath(tAddon, pNoteIdPath);
        }
        private unsafe static AtkResNode* GetNodeByIdPath(AtkUnitBase* pAddonBase, Queue pNoteIdPath)
        {
            //PluginLog.LogDebug($"> Checking pNoteIdPath.Count={pNoteIdPath.Count}");
            if (pAddonBase == null || !pAddonBase->IsVisible) return null;
            int? tFirstNodeId = (int?)pNoteIdPath.Dequeue();
            if (!tFirstNodeId.HasValue) return null;
            var tFirstNode = pAddonBase->UldManager.SearchNodeById((uint)tFirstNodeId.Value);

            return UtilsGUI.WalkNodeByIDs(pNoteIdPath, tFirstNode);
        }
        private unsafe static AtkResNode* WalkNodeByIDs(Queue pNoteIdPath, AtkResNode* pCurrNode)
        {
            //PluginLog.LogDebug(String.Format("> Checking 0x{0:x}. pNoteIdPath.Count={1}", new IntPtr(pCurrNode), pNoteIdPath.Count));
            if (pCurrNode == null) return pCurrNode;    // only for first call from driver
            if (pNoteIdPath.Count == 0) return pCurrNode;

            int? pNextNodeId = (int?)pNoteIdPath.Dequeue();
            if (!pNextNodeId.HasValue) return pCurrNode;

            var tNextNode = pCurrNode->GetComponent()->UldManager.SearchNodeById((uint)pNextNodeId.Value);
            if (tNextNode == null)
            {
                return pCurrNode;
            }
            else
            {
                return WalkNodeByIDs(pNoteIdPath, tNextNode);
            }
        }

        internal class Colors
        {
            public readonly static Vector4 NormalText_White = ImGuiColors.DalamudWhite2;
            public readonly static Vector4 NormalText_Latte = Utils.RGBAtoVec4(192, 180, 158, 255);
            public readonly static Vector4 NormalText_OrangeDark = Utils.RGBAtoVec4(232, 159, 91, 255);
            public readonly static Vector4 NormalText_Orange = Utils.RGBAtoVec4(251, 225, 202, 255);
            public readonly static Vector4 BackgroundText_Grey = ImGuiColors.ParsedGrey;
            public readonly static Vector4 ActivatedText_Green = ImGuiColors.ParsedGreen;
            public readonly static Vector4 NormalBar_Grey = Utils.RGBAtoVec4(165, 165, 165, 80);
            public readonly static Vector4 ActivatedBar_Green = Utils.RGBAtoVec4(176, 240, 6, 80);
            public readonly static Vector4 NormalText_Red = ImGuiColors.DalamudRed;
            public readonly static Vector4 TableCell_Green = new Vector4(0.67f, 1, 0.59f, 0.2f);
            public readonly static Vector4 TableCell_Yellow = new Vector4(0.93f, 0.93f, 0.35f, 0.2f);
            public readonly static Vector4 MycItemBoxOverlay_Black = Utils.RGBAtoVec4(0, 0, 0, 150);
            public readonly static Vector4 MycItemBoxOverlay_Green = Utils.RGBAtoVec4(106, 240, 44, 122);
            public readonly static Vector4 MycItemBoxOverlay_White = Utils.RGBAtoVec4(255, 255, 255, 122);
            public readonly static Vector4 MycItemBoxOverlay_Red = Utils.RGBAtoVec4(255, 0, 0, 122);
            public readonly static Vector4 MycItemBoxOverlay_RedDark = Utils.RGBAtoVec4(30, 5, 4, 255);
            public readonly static Vector4 MycItemBoxOverlay_RedDarkBright = Utils.RGBAtoVec4(92, 63, 70, 255);
            public readonly static Vector4 MycItemBoxOverlay_Latte = Utils.RGBAtoVec4(109, 99, 102, 255);
            public readonly static Vector4 MycItemBoxOverlay_Gold = Utils.RGBAtoVec4(254, 247, 202, 255);
            public readonly static Vector4 MycItemBoxOverlay_GoldDark = Utils.RGBAtoVec4(255, 124, 170, 255);
            public readonly static Vector4 MycItemBoxOverlay_NeonRedWhite = Utils.RGBAtoVec4(172, 125, 136, 255);
            public readonly static Vector4 MycItemBoxOverlay_ItemBorderGold = Utils.RGBAtoVec4(210, 172, 113, 255);

            public readonly static Vector4 GenObj_YellowFragment = new(0.89f, 0.92f, 0.61f, 0.4f);
            public readonly static Vector4 GenObj_BlueAction = new(0.61f, 0.79f, 0.92f, 0.4f);
            public readonly static Vector4 GenObj_PinkFate = new(0.9f, 0.61f, 0.9f, 0.4f);
            public readonly static Vector4 GenObj_GreenMob = new(0.61f, 0.92f, 0.77f, 0.4f);
            public readonly static Vector4 GenObj_RedLoadout = Utils.RGBAtoVec4(158, 41, 16, 122);
        }
    }
}
