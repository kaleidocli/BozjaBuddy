using BozjaBuddy.GUI.Sections;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Game;
using BozjaBuddy.Data;
using Dalamud.Logging;
using BozjaBuddy.Utils;
using System.Security.Cryptography;
using Dalamud.Interface;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;

namespace BozjaBuddy.GUI.GUIExtension
{
    internal class ExtGui_MycItemBagTrade : ExtGui
    {
        public override string mId { get; set; } = "extMycItemBagTrade";
        public override string mAddonName { get; set; } = "MYCItemBagTrade";
        private Plugin mPlugin;
        private Dictionary<string, ImGuiTextFilterPtr> mGuiVar_TextFilters = new();
        private Dictionary<string, int> mGuiVar_TextFiltersCurrVal = new();

        private ExtGui_MycItemBagTrade() { }
        public ExtGui_MycItemBagTrade(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override void Draw()
        {
            // Toolbar
            if (!this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_Toolbar)
            {
                LoadoutTableSection.DrawOverlayBar(
                    this.mPlugin,
                    this.mGuiVar_TextFilters,
                    this.mGuiVar_TextFiltersCurrVal,
                    pOverlayComboFixedWidth: 200
                );
            }

            var tBgDrawList = ImGui.GetBackgroundDrawList();

            if (this.mPlugin.Configuration.mGuiAssistConfig.itemBox.isDisabled_LoadoutMiniview) return;

            // Getting addon info
            Vector2 tOrigin = new();
            Vector2? tEnd = null;
            float tScale = 0;
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName(this.mAddonName);
                if (tAddon != null)
                {
                    tOrigin.X = tAddon->X; tOrigin.Y = tAddon->Y;
                    var tNode = (AtkResNode*)tAddon->RootNode;
                    if (tNode != null)
                    {
                        tEnd = tOrigin + new Vector2(tAddon->GetScaledWidth(true), tAddon->GetScaledHeight(true));
                    }
                    tScale = tAddon->Scale;
                }
            }

            // Draw mini-holster view
            float tLimitX = tOrigin.X + ((tEnd!.Value - tOrigin).X * 0.786f);
            float tLimitY = tOrigin.Y + ((tEnd!.Value - tOrigin).Y * 0.15875f);
            Vector2 tOrigin2 = new(tLimitX, tLimitY);
            Vector2 tEnd2 = tOrigin2 + ((tEnd.Value - tOrigin2) + new Vector2(-10, -88) * tScale);

            // Draw items
            Loadout? tLoadout = this.mPlugin.Configuration.GetActiveOverlay(this.mPlugin);
            if (tLoadout == null) return;
            if (tLoadout.mActionIds.Count > 9)      // Downscale if more than 9 items
            {
                tScale *= (1 - 0.1f * (tLoadout.mActionIds.Count - 9));
            }
            float tLineHeight = ImGui.CalcTextSize("A").X * 4 * tScale;
            float tCurrOffset = 0;
            float tInnerSpacing = 5 * tScale;
            // Draw backdrop
            tBgDrawList.AddRectFilled(
                tOrigin2 + new Vector2(0, -2.05f),
                tEnd2 + new Vector2(4, 0) * tScale,
                ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_RedDark, 0.95f))
                );
            // Draw title/example/legend
            {
                tCurrOffset -= tLineHeight + tInnerSpacing;
                // Amount (Loadout)
                tBgDrawList.AddText(
                        tOrigin2 + new Vector2(0, tCurrOffset) + new Vector2(4, -3.5f),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalText_Latte),
                        "Loadout"
                    );
                // Amount (Cache)                     // Origin2 + CurrentItemPosition + ItemInnerLineOffset
                tBgDrawList.AddText(
                        tOrigin2 + new Vector2(0, tCurrOffset + tLineHeight - 14.5f) + new Vector2(4, 0),
                        //tOrigin2 + new Vector2(tLineHeight, tCurrOffset + 10.5f) + new Vector2(4 + tInnerSpacing, (tScale < 0.8 ? 0 : tInnerSpacing) - 5),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.75f)),
                        "Cache"
                    );
                // Item separator
                tBgDrawList.AddLine(
                        tOrigin2 + new Vector2(0, tCurrOffset) + new Vector2(4, tLineHeight / 2),
                        tOrigin2 + new Vector2(tLineHeight, tCurrOffset) + new Vector2(tEnd.Value.X - tOrigin2.X - tLineHeight - 3.5f, tLineHeight / 2),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_NeonRedWhite, 0.95f)),
                        2f * tScale
                    );
                tCurrOffset = 0;
            }
            // Draw loadout items
            {
                var tItemBoxConfig = this.mPlugin.Configuration.mGuiAssistConfig.itemBox;
                foreach (int iId in tLoadout.mActionIds.Keys)
                {
                    var tIcon = UtilsGameData.kTexCol_LostAction!.GetTextureFromItemId((uint)iId);
                    if (tIcon == null) { continue; }
                    // Icon
                    tBgDrawList.AddImage(
                            tIcon.ImGuiHandle,
                            tOrigin2 + new Vector2(0, tCurrOffset) + new Vector2(4, 0),
                            tOrigin2 + new Vector2(tLineHeight, tCurrOffset + tLineHeight) + new Vector2(4, 0)
                        );
                    // Amount (Loadout)
                    int tAmountLoadout = tLoadout.mActionIds[iId];
                    tBgDrawList.AddText(
                            tOrigin2 + new Vector2(tLineHeight, tCurrOffset) + new Vector2(4 + tInnerSpacing, -3.5f),
                            ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalText_Latte),
                            tAmountLoadout.ToString()
                        );
                    // Amount (Holster)
                    int tAmountHolster = tItemBoxConfig.userHolsterData.ContainsKey(iId) ? tItemBoxConfig.userHolsterData[iId] : 0;
                    tBgDrawList.AddText(
                            new Vector2(tEnd2.X, tOrigin2.Y) + new Vector2(0, tCurrOffset) + new Vector2(- (4 + (tAmountHolster > 9 ? 1 : 0) * 6.5f), -3.5f),
                            tAmountHolster == tAmountLoadout
                                ? ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalText_Latte)
                                : tAmountHolster < tAmountLoadout
                                    ? ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.65f))
                                    : ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.MycItemBoxOverlay_Red),
                            tAmountHolster.ToString()
                        );
                    // Amount (Cache)                     // Origin2 + CurrentItemPosition + ItemInnerLineOffset
                    tBgDrawList.AddText(
                            tOrigin2 + new Vector2(tLineHeight, tCurrOffset + tLineHeight - 14.5f) + new Vector2(4 + tInnerSpacing, 0),
                            //tOrigin2 + new Vector2(tLineHeight, tCurrOffset + 10.5f) + new Vector2(4 + tInnerSpacing, (tScale < 0.8 ? 0 : tInnerSpacing) - 5),
                            ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.65f)),
                            tItemBoxConfig.userCacheData.ContainsKey(iId)
                                ? tItemBoxConfig.userCacheData[iId].ToString()
                                : "0"
                        );
                    // Item separator
                    tBgDrawList.AddLine(
                            tOrigin2 + new Vector2(0, tCurrOffset) + new Vector2(4 + tLineHeight, tLineHeight / 2),
                            tOrigin2 + new Vector2(tLineHeight, tCurrOffset) + new Vector2(tEnd.Value.X - tOrigin2.X - tLineHeight - 3.5f, tLineHeight / 2),
                            ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_NeonRedWhite, 0.95f)),
                            2f * tScale
                        );
                    // Icon border
                    tBgDrawList.AddRect(
                            tOrigin2 + new Vector2(0, tCurrOffset) + new Vector2(4, 0),
                            tOrigin2 + new Vector2(tLineHeight, tCurrOffset + tLineHeight) + new Vector2(4, 0),
                            tAmountLoadout == tAmountHolster
                            ? ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_ItemBorderGold, 0.95f))
                            : ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.MycItemBoxOverlay_Latte),
                            0,
                            ImDrawFlags.None,
                            2f * tScale
                        );

                    tCurrOffset += tLineHeight + tInnerSpacing;
                }
            }
        }

        
    }
}
