using ImGuiNET;
using System;
using BozjaBuddy.Data;
using BozjaBuddy.Utils;
using System.Collections.Generic;
using BozjaBuddy.Filter;
using Dalamud.Logging;

namespace BozjaBuddy.GUI
{
    public class GUIFilter
    {
        public static int HEADER_TEXT_FIELD_SIZE_OFFSET = 12;

        public void HeaderNormal(String pHeaderName)
        {
            ImGui.Text(pHeaderName.ToUpper());
        }

        public void HeaderTextInput(String pHeaderName, ref string pCurrValue, ref bool pEditedState, Filter.Filter? pFilter, ImGuiInputTextFlags pFlags = ImGuiInputTextFlags.None)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            if (pFilter != null && pFilter.mIsContainedInCell && pFilter.IsFiltering()) 
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth() - 30);

            ImGui.InputTextWithHint("", pHeaderName.ToUpper(), ref pCurrValue, 64, ImGuiInputTextFlags.None | pFlags);

            ImGui.PopStyleVar(); ImGui.PopStyleVar();

            if (pFilter != null && pFilter.IsFiltering())
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
                if (ImGui.Button($" X ##{pHeaderName}")) pFilter.ResetCurrValue();
            }

            pEditedState = ImGui.IsItemDeactivatedAfterEdit();
        }

        public bool HeaderSelectable(String pRoleName, ref bool pSelecableFlag, Filter.Filter? pFilter, bool pSameLine = true, bool pClosePopupOnSelect = false)
        {
            var tIsFiltering = pFilter != null && pFilter.IsFiltering();
            var tRes = ImGui.Selectable(pRoleName, 
                                ref pSelecableFlag, 
                                (pClosePopupOnSelect ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.DontClosePopups),
                                new System.Numerics.Vector2(ImGui.GetColumnWidth() - GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET - (tIsFiltering ? 30 : 0), ImGui.GetFontSize()));
            if (tIsFiltering)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 10);
                if (ImGui.Button($" X ##{pRoleName}")) pFilter!.ResetCurrValue();
            }
            if (pSameLine) ImGui.SameLine();
            return tRes;
        }

        public void HeaderNumberInputPair(String pHeaderName, ref int[] pCurrValue, Filter.Filter? pFilter)
        {
            bool tIsFiltering = pFilter != null && pFilter.IsFiltering();
            if (ImGui.Selectable(pHeaderName.ToUpper(), 
                    true, 
                    ImGuiSelectableFlags.None, 
                    new System.Numerics.Vector2(ImGui.GetColumnWidth() - GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET, ImGui.GetFontSize())))
            {
                ImGui.OpenPopup("popup");
            }
            if (ImGui.BeginPopup("popup"))
            {
                ImGui.PushItemWidth(ImGui.GetFontSize() * 12);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new System.Numerics.Vector2(1, 0));

                ImGui.InputInt2("", ref pCurrValue[0], ImGuiInputTextFlags.CharsDecimal);
                if (tIsFiltering)
                {
                    ImGui.SameLine();
                    if (ImGui.Button($" X ##{pHeaderName}")) pFilter!.ResetCurrValue();
                }

                ImGui.PopStyleVar();
                ImGui.PopItemWidth();
                ImGui.TreePop();
                ImGui.EndPopup();
            }
        }

        public void HeaderComboEnum<T>(String pHeaderName, ref T pCurrValue, T[] pValues, T pDefaultValue) where T : struct, Enum
        {
            ImGuiComboFlags tFlags = ImGuiComboFlags.NoPreview;
            if (ImGui.BeginCombo(pHeaderName.ToString(), pDefaultValue.ToString(), tFlags))
            {
                for (int i = 0; i < pValues.Length; i++)
                {
                    bool tIsSelected = pValues[i].CompareTo(pCurrValue) == 0;
                    if (ImGui.Selectable(pValues[i].ToString(), tIsSelected))
                    {
                        pCurrValue = pValues[i];
                    }
                    if (tIsSelected) { ImGui.SetItemDefaultFocus(); }
                }
                ImGui.EndCombo();
            }
        }
    
        public void HeaderRoleIconButtons(RoleFlag pRoleFlag, Filter.Filter? pFilter)
        {
            bool tIsFiltering = pFilter != null && pFilter.IsFiltering();
            pRoleFlag.UpdateRoleFlagBit();
            List<Role> tRoles = new List<Role>() { Role.Tank, Role.Healer, Role.Melee, Role.Range, Role.Caster };
            for (int i = 0; i < tRoles.Count; i++)
            {
                if (i != 0) ImGui.SameLine();
                ImGui.PushID(i);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new System.Numerics.Vector2(0, 0));
                if (ImGui.ImageButton(
                    UtilsGameData.GetRoleIcon(pRoleFlag.mRoleFlagArray[i] ? tRoles[i] : Role.None)!.ImGuiHandle,
                    new System.Numerics.Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight())))
                {
                    pRoleFlag.mRoleFlagArray[i] = !pRoleFlag.mRoleFlagArray[i];
                }
                else
                {
                    UtilsGUI.SetTooltipForLastItem((tRoles[i].ToString()));
                }
                ImGui.PopStyleVar();
                ImGui.PopStyleVar();
                ImGui.PopID();
            }
            if (tIsFiltering)
            {
                ImGui.SameLine();
                if (ImGui.Button($" X ##rcancel")) pFilter!.ResetCurrValue();
            }
        }
    }
}
