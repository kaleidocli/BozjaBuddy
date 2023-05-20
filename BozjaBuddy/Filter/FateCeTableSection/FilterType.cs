using BozjaBuddy.Data;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.FateCeTableSection
{
    internal class FilterType : Filter
    {
        public override string mFilterName { get; set; } = "type";
        private new Fate.FateType mCurrValue = default!;

        public FilterType()
        {
            Init();
        }
        public FilterType(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }

        public override bool CanPassFilter(Fate pLostAction)
            => !mIsFilteringActive
                | mCurrValue == Fate.FateType.None
                | mCurrValue == pLostAction.mType;

        public override void DrawFilterGUI()
        {
            Fate.FateType[] tValues = { Fate.FateType.None,
                                        Fate.FateType.Fate,
                                        Fate.FateType.CriticalEngagement,
                                        Fate.FateType.Duel,
                                        Fate.FateType.Raid};
            mGUI.HeaderComboEnum(" ", ref mCurrValue, tValues, tValues[0]);
        }
        public override bool IsFiltering() => this.mCurrValue != Fate.FateType.None;
        public override void ResetCurrValue() { this.mCurrValue = Fate.FateType.None; }

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mFates[id].mType).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mFates[id].mType).ToList();
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
