using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.FateCeTableSection
{
    internal class FilterMettle : Filter
    {
        public override string mFilterName { get; set; } = "mettle";
        private new int[] mLastValue = default!;
        private new int[] mCurrValue = default!;

        public FilterMettle()
        {
            Init();
        }
        public FilterMettle(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }
        protected override void Init()
        {
            mLastValue = new int[2] { 0, 99999999 };
            mCurrValue = new int[2] { 0, 99999999 };
        }

        public override bool CanPassFilter(Fate tFate)
            => !mIsFilteringActive | (tFate.mRewardMettleMin >= mCurrValue[0] && tFate.mRewardMettleMax <= mCurrValue[1]);

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mFates[id].mRewardMettleMin).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mFates[id].mRewardMettleMin).ToList();
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue);
        }
    }
}
