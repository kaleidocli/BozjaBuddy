﻿using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.GUI;
using BozjaBuddy.GUI.NodeGraphViewer;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using BozjaBuddy.GUI.Sections;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.Sheets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO.Pipes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace BozjaBuddy.Utils
{
    public class UtilsGUI
    {
        private const float FRAME_ROUNDING = 0;
        private static Vector2 FRAME_PADDING { get; } = new(10f, 2f);
        private static DateTime kTimeSinceLastClipboardCopied = DateTime.Now;
        private unsafe static readonly AtkStage* stage = AtkStage.Instance();

        private static Dictionary<int, int> _cachedInventoryItemAndCount = new();
        private static Dictionary<int, DateTime> _inventoryItemLastCacheTime = new();
        private const float _inventoryCacheInterval = 1000;

        // https://www.programcreek.com/cpp/?code=kswaldemar%2Frewind-viewer%2Frewind-viewer-master%2Fsrc%2Fimgui_impl%2Fimgui_widgets.cpp
        public static void ShowHelpMarker(string desc, string markerText = "(?)", bool disabled = true)
        {
            if (disabled)
                ImGui.TextDisabled(markerText);
            else
                ImGui.TextUnformatted(markerText);
            UtilsGUI.SetTooltipForLastItem(desc);
        }
        public static bool SetTooltipForLastItem(string tDesc, float tSize = 450.0f)
        {
            if (!ImGui.IsItemHovered()) return false;

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(tSize);
            ImGui.TextUnformatted(tDesc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            return true;
        }
        public static void TextWithHelpMarker(string pText, string pHelpMarkerText = "", Vector4? pColor = null)
        {
            if (pColor != null)
                ImGui.TextColored(pColor.Value, pText);
            else
                ImGui.Text(pText);
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker(pHelpMarkerText);
        }
        public static void GreyText(string pText)
        {
            ImGui.TextColored(BozjaBuddy.Utils.UtilsGUI.Colors.BackgroundText_Grey, pText);
        }
        /// <summary>https://discord.com/channels/581875019861328007/653504487352303619/1095768356201705623</summary>
        public static bool IconTextButton(FontAwesomeIcon icon, string text)
        {
            var buttonClicked = false;

            var iconSize = UtilsGUI.GetIconSize(icon);
            var textSize = ImGui.CalcTextSize(text);
            var padding = ImGui.GetStyle().FramePadding;
            var spacing = ImGui.GetStyle().ItemSpacing;

            var buttonSizeX = iconSize.X + textSize.X + padding.X * 2 + spacing.X;
            var buttonSizeY = (iconSize.Y > textSize.Y ? iconSize.Y : textSize.Y) + padding.Y * 2;
            var buttonSize = new Vector2(buttonSizeX, buttonSizeY);

            if (ImGui.Button("###" + icon.ToIconString() + text, buttonSize))
            {
                buttonClicked = true;
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - buttonSize.X - padding.X);
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(icon.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text(text);

            return buttonClicked;
        }
        public static Vector2 GetIconSize(FontAwesomeIcon icon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            var iconSize = ImGui.CalcTextSize(icon.ToIconString());
            ImGui.PopFont();
            return iconSize;
        }
        /// <summary>
        /// <para>pIsLink:              Whether the Link is a Link or just a Selectable. If false, genId doesn't matter. </para>
        /// <para>If pSize is not given, the size will be calculated from the size of pContent.</para>
        /// </summary>
        public static bool SelectableLink(
            Plugin pPlugin, 
            string pContent, 
            int pTargetGenId, 
            bool pIsWrappedToText = true, 
            bool pIsClosingPUOnClick = true,
            bool pIsAuxiLinked = true,
            Vector4? pTextColor = null,
            Vector2? pSize = null,
            InputPayload? pInputPayload = null)
        {
            bool tRes = false;
            Vector2? tTextSize = pSize.HasValue ? null : ImGui.CalcTextSize(pContent);
            pInputPayload ??= new();

            ImGui.PushID(pTargetGenId);
            pInputPayload.CaptureInput();
            if (pTextColor.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, pTextColor.Value);
            if (pIsWrappedToText)
            {
                if (ImGui.Selectable(pContent,
                                        false,
                                        pIsClosingPUOnClick ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.DontClosePopups, 
                                        pSize ?? new System.Numerics.Vector2(tTextSize!.Value.X + 0.5f, tTextSize!.Value.Y + 0.25f)
                                        ))
                {
                    tRes = true;
                    if (pIsAuxiLinked)
                    {
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
                else
                {
                    pInputPayload.mIsHovered = ImGui.IsItemHovered();
                }
            }
            else
            {
                if (ImGui.Selectable(pContent, false, pIsClosingPUOnClick ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.DontClosePopups))
                {
                    tRes = true;
                    if (pIsAuxiLinked)
                    {
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
                else        // keep this in this scope to focus only on the above Selectable
                {
                    pInputPayload.mIsHovered = ImGui.IsItemHovered();
                }
            }
            if (pTextColor.HasValue) ImGui.PopStyleColor();
            ImGui.PopID();
            return tRes;
        }
        /// <summary>
        /// <para>If pSize is not given, the size will be calculated from the size of pContent.</para>
        /// <para>Return true if link is clicked with LMB</para>
        /// </summary>
        public static bool SelectableLink_WithPopup(
            Plugin pPlugin, 
            string pContent, 
            int pTargetGenId, 
            bool pIsWrappedToText = true, 
            bool pIsClosingPUOnClick = true, 
            Vector4? pTextColor = null, 
            Vector2? pSize = null,
            bool pIsShowingCacheAmount = false,
            bool pIsShowingLinkIcon = true,
            string pAdditionalHoverText = "",
            bool pIsAuxiLinked = true,
            InputPayload? pInputPayload = null,
            NodeGraphViewer? pNGViewer = null,          // the viewer to hook this link to
            bool pBlockNGViewer = false,
            AuxNode? pAuxNode = null)
        {
            pInputPayload ??= new InputPayload();
            bool tRes = UtilsGUI.SelectableLink(
                pPlugin, 
                pContent + (pIsShowingLinkIcon ? "  »" : ""), 
                pTargetGenId, 
                pIsWrappedToText, 
                pIsClosingPUOnClick: pIsClosingPUOnClick, 
                pTextColor: pTextColor,
                pSize: pSize,
                pIsAuxiLinked: pAuxNode == null ? pIsAuxiLinked : false,
                pInputPayload: pInputPayload);
            if (!pPlugin.mBBDataManager.mGeneralObjects.ContainsKey(pTargetGenId))
            {
                ImGui.Text("<unrecognizable obj>");
                return tRes;
            }
            GeneralObject tObj = pPlugin.mBBDataManager.mGeneralObjects[pTargetGenId];
            if (!ImGui.GetIO().KeyShift) UtilsGUI.SetTooltipForLastItem($"{pAdditionalHoverText}[LMB] Show details\t\t[RMB] Options (marketboard, item link, etc.)\n────────────────────────────────\n{tObj.GetReprUiTooltip()}");

            ImGui.PushID(pTargetGenId);
            if (!pInputPayload.mIsKeyShift && ImGui.BeginPopupContextItem(pContent, ImGuiPopupFlags.MouseButtonRight))
            {
                ImGui.BeginGroup();
                // Item link to Clipboard + Chat
                UtilsGUI.ItemLinkButton(pPlugin, pReprName: tObj.GetReprName(), pReprItemLink: tObj.GetReprItemLink());
                ImGui.Separator();
                // Clipboard sypnosis
                UtilsGUI.SypnosisClipboardButton(tObj.GetReprClipboardTooltip());
                ImGui.Separator();
                // Map_link to Clipboard + Chat
                var tLocation = tObj.GetSalt() == GeneralObject.GeneralObjectSalt.Quest
                                ? ((BozjaBuddy.Data.Quest)tObj).mIssuerLocation
                                : tObj.GetReprLocation();
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
                if (!pBlockNGViewer)
                {
                    if (pAuxNode != null)
                    {
                        pAuxNode.SetSeed(
                            new(
                                AuxNode.nodeType,
                                new BBNodeContent(pPlugin, pTargetGenId, tObj.mName),
                                ofsToPrevNode: new Vector2(40, 20)
                                ));
                    }
                    else if (pNGViewer != null)
                    {
                        pNGViewer.AddNodeToActiveCanvas<AuxNode>(new BBNodeContent(pPlugin, pTargetGenId, tObj.mName));
                    }
                    else if (pPlugin.Configuration.mIsAuxiUsingNGV)
                    {
                        pPlugin.NodeGraphViewer_Auxi.AddNodeToActiveCanvas<AuxNode>(new BBNodeContent(pPlugin, pTargetGenId, tObj.mName));
                    }
                    else Plugin.GetWindow("Bozja Buddy")!.IsOpen = true;
                }
                else Plugin.GetWindow("Bozja Buddy")!.IsOpen = true;
            }
            ImGui.PopID();
            // Lost action's cache amount
            if (pIsShowingCacheAmount)
            {
                int[] tTemp = GeneralObject.GenIdToIdAndSalt(pTargetGenId);
                if (tTemp[1] == (int)GeneralObject.GeneralObjectSalt.LostAction)
                {
                    if (pPlugin.Configuration.mGuiAssistConfig.itemBox.userCacheData.TryGetValue(tTemp[0], out int tAmount))
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, $"({tAmount})");
                    }
                }
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
        public static void ItemLinkButton(Plugin pPlugin, string pReprName, SeString? pReprItemLink = null, bool pUseIcon = false, Vector2? pFramePadding = null)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, UtilsGUI.FRAME_ROUNDING);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, pFramePadding ?? UtilsGUI.FRAME_PADDING);
            if (!pUseIcon && ImGui.Button("Link item"))
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
                    pPlugin.ChatGui.Print(new Dalamud.Game.Text.XivChatEntry { Message = pReprItemLink });
                }
                catch (Exception e) { pPlugin.PLog.Error(e.Message); }
            }
            else if (pUseIcon && ImGuiComponents.IconButton(FontAwesomeIcon.Comment))
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
                    pPlugin.ChatGui.Print(new Dalamud.Game.Text.XivChatEntry { Message = pReprItemLink });
                }
                catch (Exception e) { pPlugin.PLog.Error(e.Message); }
            }
            if ((DateTime.Now - UtilsGUI.kTimeSinceLastClipboardCopied).TotalSeconds < 5)
                UtilsGUI.SetTooltipForLastItem("Copied to clipboard");
            else
                UtilsGUI.SetTooltipForLastItem("Link the item to chat. Also copy the item name to clipboard.");
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
        /// <summary> 
        /// <para> pDesc:       Text that appears on the button. If not given, the location's text will appear instead. </para>
        /// </summary>
        public static void LocationLinkButton(Plugin pPlugin, Location pLocation, bool rightAlign = false, bool pUseIcon = false, string? pDesc = null, bool pIsDisabled = false, float pRightAlignOffset = 0f, float pScaling = 1, Vector2? pFramePadding = null, bool pIsTeleporting = false)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, UtilsGUI.FRAME_ROUNDING);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, (pFramePadding ?? UtilsGUI.FRAME_PADDING) * pScaling);
            string tButtonText = pDesc ?? (pLocation.mAreaFlag == Location.Area.None
                                           ? pLocation.ToStringFull()
                                           : $"{pLocation.mAreaFlag} ({pLocation.mMapCoordX}, {pLocation.mMapCoordY})");            // use area flag for bozja areas since they're too long. Otherwise, use normal location string.
            if (rightAlign)
            {
                AuxiliaryViewerSection.GUIAlignRight(ImGui.CalcTextSize(tButtonText).X + pRightAlignOffset);
            }
            if (pIsDisabled)
            {
                ImGui.BeginDisabled();
                if (pUseIcon)
                    ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt);
                else 
                    ImGui.Button(tButtonText);
                ImGui.EndDisabled();
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                return;
            }
            if (pUseIcon
                ? ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt)
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
                pPlugin.ChatGui.Print(new Dalamud.Game.Text.XivChatEntry
                {
                    Message = SeString.CreateMapLink(pLocation.mTerritoryID, pLocation.mMapID, (float)pLocation.mMapCoordX, (float)pLocation.mMapCoordY)
                });
                // teleport
                if (pIsTeleporting)
                {
                    var tAetheryteId = pPlugin.DataManager.GetExcelSheet<TerritoryType>()!.GetRowOrDefault(pLocation.mTerritoryID)?.Aetheryte.ValueNullable?.RowId;
                    if (tAetheryteId != null)
                    {
                        unsafe
                        {
                            Utils.Teleport(pPlugin, tAetheryteId.Value);
                        }
                    }
                }
            }
            UtilsGUI.SetTooltipForLastItem($"Mark on map + Link to Chat {(pIsTeleporting ? "+ Teleport" : "")}\n\nat: {pLocation.ToStringFull()}");
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
        public static void MarketboardButton(Plugin pPlugin, int pItemId, bool pIsDisabled = false, bool pUseIcon = false, Vector2? pFramePadding = null)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, pFramePadding ?? UtilsGUI.FRAME_PADDING);
            if (pIsDisabled)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Marketboard");
                ImGui.EndDisabled();
                ImGui.PopStyleVar();
                return;
            }
            if (!pUseIcon && ImGui.Button("Marketboard"))
            {
                pPlugin.CommandManager.ProcessCommand($"/pmb {pItemId}");
            }
            else if (pUseIcon && ImGuiComponents.IconButton(FontAwesomeIcon.ArrowTrendUp))
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
                Plugin.GetWindow(pWinHandle)!.IsOpen = !(Plugin.GetWindow(pWinHandle)!.IsOpen);
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
                Plugin.GetWindow(pWinHandle)!.IsOpen = !(Plugin.GetWindow(pWinHandle)!.IsOpen);
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
        /// <summary>
        /// <para>A SelectableLink_WithPopup but in form of an image. Can be configured to be a normal Selectable.</para>
        /// <para>pIsLink:              Whether the Link is a Link or just behaves like an ImGui.Selectable (If false, genId doesn't matter).</para>
        /// <para>pIsAuxiLinked:        Whether the Link will pop up an Auxiliary tab</para>
        /// <para>pContent:             Not advised to use.</para>
        /// <para>pLinkPadding:         Link padding from four sides of the image.</para>
        /// <para>pCustomLinkSize:      If set, use the custom size and ignoring padding. Otherwise, use image's size + padding.</para>
        /// <para>pImageOverlayRGBA:    If set, a color tint will overlay the image. Can be used to ajust image's transparency.</para>
        /// <para>pIamgeBorderColor:    If set, a border with given color will be rendered around the image (not the link).</para>
        /// </summary>
        public static bool SelectableLink_Image(
            Plugin pPlugin,
            int pTargetGenId,
            IDalamudTextureWrap pImage,
            string pContent = "",
            bool pIsLink = true,
            bool pIsAuxiLinked = true,
            bool pIsClosingPUOnClick = true,
            bool pIsShowingCacheAmount = false,
            Vector2? pLinkPadding = null,
            float pImageScaling = 1, 
            Vector2? pCustomLinkSize = null,
            Vector4? pTextColor = null,
            Vector4? pImageOverlayRGBA = null,
            Vector4? pImageBorderColor = null,
            string pAdditionalHoverText = "",
            InputPayload? pInputPayload = null,
            AuxNode? pAuxNode = null)
        {
            bool tRes;
            var tAnchor = ImGui.GetCursorPos();
            // Draw link
            Vector2 tLinkSize = (pCustomLinkSize ?? new Vector2(pImage.Width, pImage.Height) * pImageScaling);
            if (pLinkPadding.HasValue && !pCustomLinkSize.HasValue) tLinkSize += pLinkPadding.Value * 2;     // padding
            if (pIsLink)
            {
                tRes = UtilsGUI.SelectableLink_WithPopup(
                    pPlugin,
                    pContent,
                    pTargetGenId,
                    pIsClosingPUOnClick: pIsClosingPUOnClick,
                    pTextColor: pTextColor,
                    pSize: tLinkSize,
                    pIsShowingCacheAmount: pIsShowingCacheAmount,
                    pIsShowingLinkIcon: false,
                    pAdditionalHoverText: pAdditionalHoverText,
                    pIsAuxiLinked: pIsAuxiLinked,
                    pInputPayload: pInputPayload,
                    pAuxNode: pAuxNode);
            }
            else
            {
                tRes = UtilsGUI.SelectableLink(
                    pPlugin,
                    pContent,
                    pTargetGenId,
                    pIsAuxiLinked: false,
                    pIsClosingPUOnClick: pIsClosingPUOnClick,
                    pTextColor: pTextColor,
                    pSize: tLinkSize,
                    pInputPayload: pInputPayload);
            }
            // Set link pos
            ImGui.SetCursorPos(pLinkPadding == null ? tAnchor : tAnchor - pLinkPadding.Value);
            // Draw image
            ImGui.Image(
                pImage.ImGuiHandle, 
                new Vector2(pImage.Width * pImageScaling, pImage.Height * pImageScaling),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                pImageOverlayRGBA ?? new Vector4(1, 1, 1, 1),
                pImageBorderColor ?? Vector4.Zero
                );
            return tRes;
        }
        /// <summary> 
        /// NGV only. Specifically for quest chain. Quest chain linking should be using this.
        /// <para> Add a new canvas containing quest chain graph. If a canvas with the same quest chain already exists, set focus on that canvas instead. </para>
        /// </summary>
        public static bool SelectableLink_QuestChain(
            Plugin pPlugin,
            string pContent,
            QuestChain pQuestChain,
            NodeGraphViewer? pNGVToOpenQuestChainIn = null,
            InputPayload? pInputPayload = null
            )
        {
            bool tRes = UtilsGUI.SelectableLink_WithPopup(
                    pPlugin,
                    pContent,
                    pQuestChain.GetGenId(),
                    pIsAuxiLinked: false,
                    pBlockNGViewer: true,
                    pInputPayload: pInputPayload
                );
            if (tRes && pPlugin.Configuration.mIsAuxiUsingNGV)
            {
                NodeGraphViewer tNGV = pNGVToOpenQuestChainIn ?? pPlugin.NodeGraphViewer_Auxi;
                // Try to find and set focus on a canvas with the same quest chain tag
                if (!tNGV.SetCanvasActive(pQuestChain.GetGenId().ToString()))
                {
                    tNGV.ImportCanvas(pQuestChain.GetCanvasData(), pQuestChain.GetGenId().ToString());
                    tNGV.SetCanvasActive(pQuestChain.GetGenId().ToString());
                }
            }
            return tRes;
        }
        /// <summary> 
        /// NGV only. Specifically for quest.
        /// <para> If the quest does not belong to a quest chain, it will behave like normal SelectableLink_PU </para>
        /// <para> 
        /// If the quest belongs to any quest chain, 
        /// it will add/focus the tagged canvasses of all quest chains it belongs to, 
        /// then focus on the node of the quest on that canvas.
        /// </para>
        /// </summary>
        public static bool SelectableLink_Quest(
            Plugin pPlugin,
            string pContent,
            BozjaBuddy.Data.Quest pQuest,
            NodeGraphViewer? pNGVToOpenQuestChainIn = null,
            InputPayload? pInputPayload = null
            )
        {
            // case: normal SLPU
            if (pQuest.mQuestChains.Count == 0)
            {
                return UtilsGUI.SelectableLink_WithPopup(pPlugin, pQuest.mName, pQuest.GetGenId());
            }

            bool tRes = UtilsGUI.SelectableLink_WithPopup(
                    pPlugin,
                    pContent,
                    pQuest.GetGenId(),
                    pIsAuxiLinked: false,
                    pBlockNGViewer: true,
                    pInputPayload: pInputPayload
                );
            if (tRes && pPlugin.Configuration.mIsAuxiUsingNGV)
            {
                NodeGraphViewer tNGV = pNGVToOpenQuestChainIn ?? pPlugin.NodeGraphViewer_Auxi;

                foreach (var chainId in pQuest.mQuestChains)
                {
                    // Get chain
                    if (!pPlugin.mBBDataManager.mQuestChains.TryGetValue(chainId, out var qChain) || qChain == null) continue;
                    // Try to find and set focus on a canvas with the same quest chain tag
                    if (!tNGV.SetCanvasActive(qChain.GetGenId().ToString()))
                    {
                        tNGV.ImportCanvas(qChain.GetCanvasData(), qChain.GetGenId().ToString(), noInitOfs: true);
                        tNGV.SetCanvasActive(qChain.GetGenId().ToString());
                    }
                    // Focus on the node of the quest
                    tNGV.FocusOnNodeTag_ActiveCanvas($"{Utils.NodeTagPrefix.SYS}{pQuest.GetGenId()}");
                }
            }
            return tRes;
        }
        public static bool SelectableLink_Quest(
            Plugin pPlugin,
            int pQuestId,
            string? pContent = null,
            NodeGraphViewer? pNGVToOpenQuestChainIn = null,
            InputPayload? pInputPayload = null
            )
        {
            pPlugin.mBBDataManager.mQuests.TryGetValue(pQuestId, out var tQuest);
            if (tQuest == null)
            {
                return ImGui.Selectable(pContent ?? "<unknown quest>");
            }
            return UtilsGUI.SelectableLink_Quest(
                    pPlugin,
                    pContent ?? tQuest.mName,
                    tQuest,
                    pNGVToOpenQuestChainIn,
                    pInputPayload
                );
        }
        /// <summary>
        /// By default, display URL on hovering.
        /// </summary>
        public static void UrlButton(string pUrl, string? pHoveredText = null, FontAwesomeIcon pIcon = FontAwesomeIcon.Globe, string? pGuiId = null)
        {
            UtilsGUI.InputPayload tInput = new();
            tInput.CaptureInput();
            ImGui.PushID(pGuiId ?? pUrl);
            if (ImGuiComponents.IconButton(pIcon) && !pUrl.IsNullOrEmpty())
            {
                try
                {
                    Process.Start(new ProcessStartInfo(pUrl)
                    {
                        UseShellExecute = true,
                    });
                }
                catch (Exception e) { }
            }
            else if (ImGui.IsItemHovered() && tInput.mIsMouseRmb)
            {
                ImGui.SetClipboardText(pUrl);
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem($"[{pUrl}]\n\n[RMB] Copy URL to clipboard\n" + (pHoveredText ?? ""));
            }
            ImGui.PopID();
        }
        /// <summary> Item that are in lumina Item sheet only! Other BB stuff won't work i.e. Fate, Fragment, etc. </summary>
        public static void ItemLinkButton_Image(Plugin pPlugin, int pItemId, IDalamudTextureWrap pItemTexture, float pImageScaling = 1)
        {
            var tItem = pPlugin.mBBDataManager.GetItem(pItemId);
            if (UtilsGUI.SelectableLink_Image(pPlugin, -1, pItemTexture, pIsLink: false, pIsAuxiLinked: false, pImageScaling: pImageScaling, pCustomLinkSize: new(pItemTexture.Height * pImageScaling * 0.9f))
                && tItem != null)
            {
                var tItemLink = tItem.GetReprItemLink();
                if (tItemLink != null)
                    pPlugin.ChatGui.Print(new Dalamud.Game.Text.XivChatEntry { Message = tItemLink });
            }
            else UtilsGUI.SetTooltipForLastItem("Click to link item to chat.");
        }
        /// <summary>
        /// <para> luminaItemId:            id of the item in the Item lumina sheet. If BBItem is not found, a text will be displayed instead of a SL_PU. </para>
        /// </summary>
        public static void InventoryItemWidget(
            Plugin pPlugin, 
            int pLuminaItemId, 
            float pUpdateRate = UtilsGUI._inventoryCacheInterval, 
            int? pMaxCount = null,
            UtilsGUI.InventoryItemWidgetFlag pFlag = InventoryItemWidgetFlag.None)
        {
            // Retrieve data
            uint tLuminaItemId = Convert.ToUInt32(pLuminaItemId);
            var tItem = pPlugin.mBBDataManager.mSheetItem?.GetRowOrDefault(tLuminaItemId);
            if (tItem == null)
            {
                ImGui.Text($"<InvItemWdgt_err: lid={pLuminaItemId}>");
                return;
            }
            IDalamudTextureWrap? tItemTexture = pFlag.HasFlag(InventoryItemWidgetFlag.NoIcon)
                ? null
                : UtilsGameData.kTextureCollection?.GetTextureFromItemId(tLuminaItemId, TextureCollection.Sheet.Item, pTryLoadTexIfFailed: true);
            int tCount = 0;
            UtilsGUI._inventoryItemLastCacheTime.TryGetValue(pLuminaItemId, out DateTime tLastCacheTime);
            // Check item validity in cache. If not, update cache.
            if (!(UtilsGUI._cachedInventoryItemAndCount.TryGetValue(pLuminaItemId, out tCount)
                && (DateTime.Now - tLastCacheTime).TotalMilliseconds < pUpdateRate)
                )
            {
                tCount = 0;
                unsafe 
                {
                    var ins = InventoryManager.Instance();
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.Inventory1);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.Inventory2);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.Inventory3);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.Inventory4);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.SaddleBag1);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.SaddleBag2);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.PremiumSaddleBag1);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.PremiumSaddleBag2);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage1);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage2);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage3);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage4);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage5);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage6);
                    tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.RetainerPage7);
                    // Gears
                    if (tItem?.EquipSlotCategory.ValueNullable != null)
                    {
                        if (tItem?.EquipSlotCategory.Value.Body == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryBody);
                        if (tItem?.EquipSlotCategory.Value.Ears == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryEar);
                        if (tItem?.EquipSlotCategory.Value.Feet == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryFeets);
                        if (tItem?.EquipSlotCategory.Value.Gloves == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryHands);
                        if (tItem?.EquipSlotCategory.Value.Head == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryHead);
                        if (tItem?.EquipSlotCategory.Value.Legs == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryLegs);
                        if (tItem?.EquipSlotCategory.Value.MainHand == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryMainHand);
                        if (tItem?.EquipSlotCategory.Value.Neck == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryNeck);
                        if (tItem?.EquipSlotCategory.Value.OffHand == (sbyte)1) tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryOffHand);
                        if (tItem?.EquipSlotCategory.Value.FingerL == (sbyte)1 || tItem?.EquipSlotCategory.Value.FingerR == (sbyte)1) 
                            tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.ArmoryRings);
                        tCount += ins->GetItemCountInContainer(tLuminaItemId, InventoryType.EquippedItems);
                    }
                }
                if (!UtilsGUI._cachedInventoryItemAndCount.TryAdd(pLuminaItemId, tCount))
                {
                    UtilsGUI._cachedInventoryItemAndCount[pLuminaItemId] = tCount;
                }
                if (!UtilsGUI._inventoryItemLastCacheTime.TryAdd(pLuminaItemId, DateTime.Now))
                {
                    UtilsGUI._inventoryItemLastCacheTime[pLuminaItemId] = DateTime.Now;
                }
            }

            // Draw
            // Item icon
            if (tItemTexture != null && !pFlag.HasFlag(InventoryItemWidgetFlag.NoIcon))
            {
                UtilsGUI.ItemLinkButton_Image(pPlugin, pLuminaItemId, tItemTexture, pImageScaling: ImGui.CalcTextSize("W").Y * 1.3f / tItemTexture.Height);
                ImGui.SameLine();
            }
            // Item count
            if (!pFlag.HasFlag(InventoryItemWidgetFlag.NoCount))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, tCount < (pMaxCount ?? -1) ? UtilsGUI.Colors.NormalText_White : UtilsGUI.Colors.BackgroundText_Green);
                ImGui.Text($"[{tCount}{(pMaxCount != null ? $"/{pMaxCount}" : "")}]");
                ImGui.PopStyleColor();
                ImGui.SameLine();
            }
            // Item link
            if (!pFlag.HasFlag(InventoryItemWidgetFlag.NoName))
            {
                ImGui.SameLine();
                var tBBItem = pPlugin.mBBDataManager.GetItem(pLuminaItemId);
                if (tBBItem != null && !pFlag.HasFlag(InventoryItemWidgetFlag.NoSelectableLink_PU))        // BBItem found
                {
                    UtilsGUI.SelectableLink_WithPopup(
                        pPlugin, 
                        tBBItem.mName, 
                        tBBItem.GetGenId(), 
                        pTextColor: tCount < (pMaxCount ?? -1)
                                    ? UtilsGUI.Colors.BackgroundText_Grey
                                    : UtilsGUI.Colors.NormalText_White
                        );
                }
                else                        // if not, use normal text with lumina data
                {
                    if (tCount < (pMaxCount ?? -1)) ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                    ImGui.Text(tItem?.Name.ToString());
                    if (tCount < (pMaxCount ?? -1)) ImGui.PopStyleColor();
                }
                ImGui.SameLine();
            }

            ImGui.Text("");     // dummy text for SameLine()
        }
        /// <summary>
        /// While rendering in a pop up, texture's width will not exceed half of the screen's width.
        /// <para> ImgFilename:     name of the img file, located in BozjaBuddy </para>
        /// </summary>
        public static bool DrawImgFromDb(
            Plugin pPlugin, 
            string pImgFilename, 
            bool pIsScaledToRegionWidth = false,
            float pExtraScaling = 1,
            bool pIsPU = false)
        {
            bool tRes = false;

            if (pPlugin.mBBDataManager.mImages.TryGetValue(pImgFilename, out IDalamudTextureWrap? tImg)
                && tImg != null)
            {
                float tScale = (pIsScaledToRegionWidth
                                    ? (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X - ImGui.GetStyle().WindowPadding.X) / tImg.Width
                                    : 1)
                               * pExtraScaling;
                if (tImg.Width * tScale > ImGui.GetIO().DisplaySize.X / 2)
                {
                    tScale = (ImGui.GetIO().DisplaySize.X / 2) / tImg.Width;
                }
                UtilsGUI.InputPayload tInput = new();
                ImGui.Image(
                    tImg.ImGuiHandle, 
                    new Vector2(
                        tImg.Width * tScale, 
                        tImg.Height * tScale
                        )
                    );
                tInput.CaptureInput();
                // extras
                if (ImGui.IsItemHovered())
                {
                    if (!pIsPU)
                    {
                        // hovered text
                        UtilsGUI.SetTooltipForLastItem("[RMB] View large");
                        // img pu
                        if (tInput.mIsMouseRmb)
                        {
                            ImGui.OpenPopup($"##imgpu{pImgFilename}");
                        }
                    }
                }
                if (ImGui.BeginPopup($"##imgpu{pImgFilename}"))
                {
                    UtilsGUI.DrawImgFromDb(pPlugin, pImgFilename, pIsPU: true);
                    ImGui.EndPopup();
                }

                tRes = true;
            }

            return tRes;
        }
        public static void DrawRoleFlagAsIconString(Plugin pPlugin, RoleFlag pRoleFlag)
        {
            List<IDalamudTextureWrap?> tRoleIcons = RoleFlag.FlagToIcon(pRoleFlag.mRoleFlagBit);
            int tDrawCounter = 0;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
            foreach (IDalamudTextureWrap? iIcon in tRoleIcons)
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
        public static bool DrawIcon(TextureCollection.StandardIcon pGameIcon, float pScaling = 1)
        {
            var tTex = UtilsGameData.kTextureCollection?.GetStandardTexture(pGameIcon);
            if (tTex == null) return false;
            ImGui.Image(tTex.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight() * pScaling));
            return true;
        }
        public static bool DrawIcon(FontAwesomeIcon pFontAwesomeIcon)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(pFontAwesomeIcon.ToIconString());
            ImGui.PopFont();
            return true;
        }

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
        /// <summary>
        /// https://github.com/shdwp/xivFaderPlugin/blob/main/FaderPlugin/Addon.cs#L35
        /// </summary>
        public static bool IsAddonFocused(string name)
        {
            unsafe {
                try
                {
                    foreach (var addon in stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList.Entries)
                    {
                        //var addonName = Marshal.PtrToStringAnsi(new IntPtr(addon.Value->Name));
                        var addonName = addon.Value->NameString;

                        if (addonName == name)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        internal class Colors
        {
            public readonly static Vector4 NormalText_White = ImGuiColors.DalamudWhite2;
            public readonly static Vector4 NormalText_Latte = Utils.RGBAtoVec4(192, 180, 158, 255);
            public readonly static Vector4 NormalText_OrangeDark = Utils.RGBAtoVec4(232, 159, 91, 255);
            public readonly static Vector4 NormalText_Orange = Utils.RGBAtoVec4(251, 225, 202, 255);
            public readonly static Vector4 BackgroundText_Grey = ImGuiColors.ParsedGrey;
            public readonly static Vector4 BackgroundText_Blue = Utils.RGBAtoVec4(148, 181, 216, 255);
            public readonly static Vector4 BackgroundText_Red = Utils.RGBAtoVec4(216, 148, 148, 255);
            public readonly static Vector4 BackgroundText_Green = Utils.RGBAtoVec4(163, 216, 148, 255);
            public readonly static Vector4 ActivatedText_Green = ImGuiColors.ParsedGreen;
            public readonly static Vector4 NormalBar_Grey = Utils.RGBAtoVec4(165, 165, 165, 80);
            public readonly static Vector4 ActivatedBar_Green = Utils.RGBAtoVec4(176, 240, 6, 80);
            public readonly static Vector4 NormalText_Red = ImGuiColors.DalamudRed;
            public readonly static Vector4 TableCell_Green = new Vector4(0.67f, 1, 0.59f, 0.2f);
            public readonly static Vector4 TableCell_Yellow = new Vector4(0.93f, 0.93f, 0.35f, 0.2f);
            public readonly static Vector4 TableCell_Red = Utils.RGBAtoVec4(249, 77, 77, 51);
            public readonly static Vector4 Button_Red = Utils.RGBAtoVec4(92, 63, 70, 255);
            public readonly static Vector4 Button_Green = new(0.61f, 0.92f, 0.77f, 0.4f);
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
            public readonly static Vector4 GenObj_BrownFieldNote = Utils.RGBAtoVec4(224, 197, 160, 122);

            public readonly static Vector4 NodeBg = Utils.RGBAtoVec4(49, 48, 49, 255);
            public readonly static Vector4 NodeFg = Utils.RGBAtoVec4(148, 121, 74, 255);
            public readonly static Vector4 NodeText = Utils.RGBAtoVec4(223, 211, 185, 255);
            public readonly static Vector4 NodePack = Utils.RGBAtoVec4(157, 189, 99, 255);
            public readonly static Vector4 NodeEdgeHighlightNeg = Utils.RGBAtoVec4(246, 132, 118, 255);
            public readonly static Vector4 NodeGraphViewer_BackdropGrey = Utils.RGBAtoVec4(165, 165, 165, 255);
            public readonly static Vector4 NodeGraphViewer_SnaplineGold = Utils.RGBAtoVec4(148, 121, 74, 255);
            public readonly static Vector4 NodeNotifInfo = Utils.RGBAtoVec4(223, 211, 185, 255);
            public readonly static Vector4 NodeNotifWarning = Utils.RGBAtoVec4(240, 160, 19, 255);
            public readonly static Vector4 NodeNotifError = Utils.RGBAtoVec4(216, 148, 148, 255);
        }

        public class InputPayload
        {
            private static DateTime kLastMouseClicked = DateTime.MinValue;
            private static DateTime kLastKeyClicked = DateTime.MinValue;
            private static DateTime kLastWheelWheeled = DateTime.MinValue;
            private static Vector2? kLastMouseDragDelta = null;
            private const double kMouseClickValidityThreshold = 150;
            private const double kKeyClickValidityThreshold = 250;
            private static float kDelayBetweenMouseWheelCapture = 100;
            public static bool kWasLmbDragged = false;
            private static List<ImGuiKey> kKeysToCheck = new() { ImGuiKey.Delete, ImGuiKey.C, ImGuiKey.V };
            private static Dictionary<ImGuiKey, bool> kKeysDown = new();
            private static double DeltaLastMouseClick() => (DateTime.Now - InputPayload.kLastMouseClicked).TotalMilliseconds;
            private static double DeltaLastKeyClick() => (DateTime.Now - InputPayload.kLastKeyClicked).TotalMilliseconds;
            public static bool CheckMouseClickValidity()
            {
                if (InputPayload.DeltaLastMouseClick() > kMouseClickValidityThreshold)
                {
                    InputPayload.kLastMouseClicked = DateTime.Now;
                    return true;
                }
                return false;
            }
            public static bool CheckKeyClickValidity()
            {
                if (InputPayload.DeltaLastKeyClick() > kKeyClickValidityThreshold)
                {
                    InputPayload.kLastKeyClicked = DateTime.Now;
                    return true;
                }
                return false;
            }

            public bool mIsHovered = false;
            public bool mIsMouseRmb = false;
            public bool mIsMouseLmb = false;
            public bool mIsMouseMid = false;
            public bool mIsMouseRmbDown = false;
            public bool mIsMouseLmbDown = false;
            public bool mIsKeyShift = false;
            public bool mIsKeyAlt = false;
            public bool mIsKeyCtrl = false;
            public bool mIsKeyDel = false;
            public bool mIsKeyC = false;
            public bool mIsKeyV = false;
            public Vector2 mMousePos = Vector2.Zero;
            private Vector2? mFirstMouseLeftHoldPos = null;
            public bool mIsALmbDragRelease = false;
            public bool mIsMouseDragLeft = false;
            public bool mIsMouseDragRight = false;
            public Vector2? mLmbDragDelta = null;
            public float mMouseWheelValue = 0;

            /// <summary>
            /// ExtraKeyboardInputs:            in case something is blocking ImGui keyboard non-mod input capture,
            ///                                 use this to submit inputs.
            /// </summary>
            public void CaptureInput(bool pCaptureMouseWheel = false, bool pCaptureMouseDrag = false, HashSet<ImGuiKey>? pExtraKeyboardInputs = null)
            {
                var io = ImGui.GetIO();
                if (io.KeyShift) mIsKeyShift = true;
                if (io.KeyAlt) mIsKeyAlt = true;
                if (io.KeyCtrl) mIsKeyCtrl = true;
                if (io.MouseReleased[0]) mIsMouseLmb = true;
                if (io.MouseReleased[1]) mIsMouseRmb = true;
                if (io.MouseReleased[2]) mIsMouseMid = true;
                if (io.MouseDown[0]) mIsMouseLmbDown = true;
                if (io.MouseDown[1]) mIsMouseRmbDown = true;
                this.mMousePos = io.MousePos;
                this.mIsALmbDragRelease = InputPayload.kWasLmbDragged;

                if (pCaptureMouseDrag)
                {
                    this.CaptureMouseDragDelta();
                }
                if (pCaptureMouseWheel) this.CaptureMouseWheel();
                if (!this.mIsMouseLmbDown && !this.mIsMouseLmb)
                {
                    InputPayload.kWasLmbDragged = false;
                    this.mIsALmbDragRelease = InputPayload.kWasLmbDragged;
                }

                // extra-inputs
                if (pExtraKeyboardInputs != null)
                {
                    foreach (var imKey in InputPayload.kKeysToCheck)
                    {
                        // Try get state: Down
                        if (pExtraKeyboardInputs.Contains(imKey))
                        {
                            if (!InputPayload.kKeysDown.TryAdd(imKey, true))
                                InputPayload.kKeysDown[imKey] = true;
                        }
                        else
                        {
                            // Try get state: Released
                            if (InputPayload.kKeysDown.TryGetValue(imKey, out var isKeyDown) && isKeyDown)
                            {
                                switch (imKey)
                                {
                                    case ImGuiKey.Delete: mIsKeyDel = true; break;
                                    case ImGuiKey.C: mIsKeyC = true; break;
                                    case ImGuiKey.V: mIsKeyV = true; break;
                                }
                                InputPayload.kKeysDown[imKey] = false;
                            }
                        }
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Delete)) mIsKeyDel = true;
            }
            /// <summary> https://git.anna.lgbt/ascclemens/QuestMap/src/commit/2030f8374eb65a64947b2bc37f35fc53ff3723f4/QuestMap/PluginUi.cs#L857 </summary>
            private Vector2? CaptureMouseDragDeltaInternal()
            {
                // Get first left hold.
                if (this.mIsMouseLmbDown && this.mFirstMouseLeftHoldPos == null) { this.mFirstMouseLeftHoldPos = mMousePos; }
                if (this.mIsMouseLmb) { this.mFirstMouseLeftHoldPos = null; }

                Vector2? tRes = null;
                this.mIsMouseDragLeft = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                this.mIsMouseDragRight = ImGui.IsMouseDragging(ImGuiMouseButton.Right);
                if (this.mIsMouseDragLeft)
                {
                    if (InputPayload.kLastMouseDragDelta == null)
                    {
                        InputPayload.kLastMouseDragDelta = ImGui.GetMouseDragDelta();
                        // Imgui's MouseDelta does not recognize tiny drag under certain threshold (prob to distinguish click vs drag)
                        // So this is a compensation which adds an extra distance equal to that threshold if the node is being dragged.
                        tRes = InputPayload.kLastMouseDragDelta + (this.mMousePos - this.mFirstMouseLeftHoldPos);
                    }
                    else
                    {
                        var d = ImGui.GetMouseDragDelta();
                        if (this.mIsMouseDragLeft)
                        {
                            tRes = (d - InputPayload.kLastMouseDragDelta);
                            tRes = tRes.Value + tRes.Value * 0;   // dragging loss is around 16% without compensation
                        }
                        InputPayload.kLastMouseDragDelta = d;
                    }
                }
                else
                {
                    InputPayload.kLastMouseDragDelta = null;
                }


                // distinguishing between a release from click or drag
                if (this.mIsMouseLmbDown)
                {
                    if (tRes.HasValue) InputPayload.kWasLmbDragged = true;
                }
                else if (!this.mIsMouseLmb)
                {
                    InputPayload.kWasLmbDragged = false;
                    this.mIsALmbDragRelease = InputPayload.kWasLmbDragged;
                }
                return tRes;
            }
            public void CaptureMouseDragDelta()
            {
                this.mLmbDragDelta = this.CaptureMouseDragDeltaInternal();
            }
            private float CaptureMouseWheelInternal()
            {
                var tRes = ImGui.GetIO().MouseWheel;
                if (tRes != 0
                    && (DateTime.Now - InputPayload.kLastWheelWheeled).TotalMilliseconds < InputPayload.kDelayBetweenMouseWheelCapture)
                {
                    InputPayload.kLastWheelWheeled = DateTime.Now;
                }
                return ImGui.GetIO().MouseWheel;
            }
            public void CaptureMouseWheel()
            {
                this.mMouseWheelValue = this.CaptureMouseWheelInternal();
            }
        }

        [Flags]
        public enum InventoryItemWidgetFlag
        {
            None = 0,
            NoCount = 1,
            NoName = 2,
            NoIcon = 4,
            NoSelectableLink_PU = 8
        }
    }
}
