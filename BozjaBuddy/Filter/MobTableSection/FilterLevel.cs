using BozjaBuddy.Data;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.MobTableSection
{
    internal class FilterLevel : Filter
    {
        public override string mFilterName { get; set; } = "level";
        private new int[] mCurrValue = default!;

        public FilterLevel()
        {
            Init();
        }
        public FilterLevel(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }
        protected override void Init()
        {
            mCurrValue = new int[2] { 0, 10 };
        }

        public override bool CanPassFilter(Mob tMob)
            => !mIsFilteringActive | (tMob.mLevel >= mCurrValue[0] && tMob.mLevel <= mCurrValue[1]);
        public override bool IsFiltering() => this.mCurrValue[0] > 0 || this.mCurrValue[1] < 10;
        public override void ResetCurrValue() { this.mCurrValue[0] = 0; this.mCurrValue[0] = 10; }

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mMobs[id].mLevel).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mMobs[id].mLevel).ToList();
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue, this);
        }
    }
}
