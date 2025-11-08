using BozjaBuddy.Data;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterWeight : Filter
    {
        public override string mFilterName { get; set; } = "weight";
        public bool mIsCompact = true;
        private new int[] mLastValue = default!;
        private new int[] mCurrValue = default!;

        public FilterWeight()
        {
            Init();
        }
        public FilterWeight(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }
        protected override void Init()
        {
            mLastValue = new int[2] { 0, 9999 };
            mCurrValue = new int[2] { 0, 9999 };
        }
        public override void ClearInputValue()
        {
            this.mCurrValue[0] = 0;
            this.mCurrValue[1] = 9999;
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | (pLostAction.mWeight >= mCurrValue[0] && pLostAction.mWeight <= mCurrValue[1]);
        public override bool CanPassFilter(Fragment pFragment) => true;
        public override bool IsFiltering() => this.mCurrValue[0] > 0 || this.mCurrValue[1] < 9999;
        public override void ResetCurrValue() { this.mCurrValue[0] = 0; this.mCurrValue[1] = 9999; }

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mLostActions[id].mWeight).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mLostActions[id].mWeight).ToList();
        }

        public override void DrawFilterGUI()
        {
            if (!this.mIsCompact)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new System.Numerics.Vector2(1, 0));

                ImGui.InputInt("", ref mCurrValue[0], 0,0,default(ImU8String),ImGuiInputTextFlags.CharsDecimal);
                if (this.IsFiltering())
                {
                    ImGui.SameLine();
                    if (ImGui.Button($" X ##pcancel")) this.ResetCurrValue();
                }

                ImGui.PopStyleVar();
                return;
            }
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue, this);
        }
        public override string GetCurrValue() => $"{this.mCurrValue[0]}-{this.mCurrValue[1]}";
    }
}
