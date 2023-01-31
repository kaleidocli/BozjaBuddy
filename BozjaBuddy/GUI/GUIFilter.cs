using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SamplePlugin.Data;
using SamplePlugin.GUI.Sections;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.ComponentModel;

namespace SamplePlugin.GUI
{
    internal class GUIFilter
    {
        public static int HEADER_TEXT_FIELD_SIZE_OFFSET = 12;

        public void HeaderNormal(String pHeaderName)
        {
            ImGui.Text(pHeaderName.ToUpper());
        }

        public void HeaderTextInput(String pHeaderName, ref string pCurrValue, ref bool pEditedState)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

            ImGui.InputTextWithHint("", pHeaderName.ToUpper(), ref pCurrValue, 64);

            ImGui.PopStyleVar(); ImGui.PopStyleVar();
            pEditedState = ImGui.IsItemDeactivatedAfterEdit();
        }

        public void HeaderSelectable(String pRoleName, ref bool pSelecableFlag, bool pSameLine = true, bool pClosePopupOnSelect = false)
        {
            ImGui.Selectable(pRoleName, 
                                ref pSelecableFlag, 
                                (pClosePopupOnSelect ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.DontClosePopups), 
                                new System.Numerics.Vector2(ImGui.GetFontSize() / 2, ImGui.GetFontSize()));
            if (pSameLine) ImGui.SameLine();
        }

        public void HeaderNumberInputPair(String pHeaderName, ref int[] pCurrValue)
        {
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
    }
}
