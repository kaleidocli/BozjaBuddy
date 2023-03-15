using BozjaBuddy.Data;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.FateCeTableSection
{
    internal class FilterExp : Filter
    {
        public override string mFilterName { get; set; } = "exp";
        private new int[] mCurrValue = default!;

        public FilterExp()
        {
            Init();
        }
        public FilterExp(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }
        protected override void Init()
        {
            mCurrValue = new int[2] { 0, 99999999 };
        }

        public override bool CanPassFilter(Fate tFate)
            => !mIsFilteringActive | (tFate.mRewardExpMin >= mCurrValue[0] && tFate.mRewardExpMax <= mCurrValue[1]);

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mFates[id].mRewardExpMin).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mFates[id].mRewardExpMin).ToList();
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue);
        }
    }
}
