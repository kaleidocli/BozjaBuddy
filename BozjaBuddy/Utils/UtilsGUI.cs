using Dalamud.Interface.Colors;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Utils
{
    internal class UtilsGUI
    {
        
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

        public unsafe static AtkResNode* GetNodeByIdPath(AtkUnitBase* pAddonBase, int[] pNoteIdPath)
        {
            return UtilsGUI.GetNodeByIdPath(pAddonBase, new Queue(pNoteIdPath));
        }
        public unsafe static AtkResNode* GetNodeByIdPath(Plugin pPlugin, string pAddonName, int[] pNoteIdPath)
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
            public readonly static Vector4 BackgroundText_Grey = ImGuiColors.ParsedGrey;
            public readonly static Vector4 ActivatedText_Green = ImGuiColors.ParsedGreen;
            public readonly static Vector4 NormalBar_Grey = Utils.RGBAtoVec4(165, 165, 165, 80);
            public readonly static Vector4 ActivatedBar_Green = Utils.RGBAtoVec4(176, 240, 6, 80);
            public readonly static Vector4 NormalText_Red = ImGuiColors.DalamudRed;
            public readonly static Vector4 TableCell_Green = new System.Numerics.Vector4(0.67f, 1, 0.59f, 0.2f);
            public readonly static Vector4 TableCell_Yellow = new System.Numerics.Vector4(0.93f, 0.93f, 0.35f, 0.2f);
        }
    }
}
