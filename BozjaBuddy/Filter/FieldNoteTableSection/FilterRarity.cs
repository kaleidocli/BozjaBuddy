using BozjaBuddy.Data;
using BozjaBuddy.GUI;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.FieldNoteTableSection
{
    internal class FilterRarity : Filter
    {
        public override string mFilterName { get; set; } = "rarity";
        public new int mCurrValue = 0;

        public FilterRarity() { }

        public FilterRarity(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }

        public override bool CanPassFilter(FieldNote tFieldNote)
            => !this.mIsFilteringActive
                || this.mCurrValue == 0
                || tFieldNote.mRarity == this.mCurrValue;
        public override bool IsFiltering() => this.mCurrValue != 0;
        public override void ResetCurrValue() { this.mCurrValue = 0; }
        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mFieldNotes[id].mRarity).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mFieldNotes[id].mRarity).ToList();
        }

        public override void DrawFilterGUI()
        {
            bool temp = true;
            if (this.mGUI.HeaderSelectable(
                    $"RARITY {(this.mCurrValue == 0 ? "" : $" {this.mCurrValue}")}",
                    ref temp,
                    null,
                    false,
                    false))
                ImGui.OpenPopup(this.mFilterName);
            if (ImGui.BeginPopup(this.mFilterName))
            {
                ImGui.SetNextItemWidth(100);
                ImGui.SliderInt("##sfrarity", ref this.mCurrValue, 0, 5);
                if (this.IsFiltering())
                {
                    ImGui.SameLine();
                    if (ImGui.Button($" X ##{this.mFilterName}")) this.ResetCurrValue();
                }
                ImGui.EndPopup();
            }
        }

        public override string GetCurrValue() => mCurrValue.ToString();
    }
}
