using SamplePlugin.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.Filter.MobTableSection
{
    internal class FilterLevel : Filter
    {
        public override string mFilterName { get; set; } = "level";
        private new int[] mLastValue = default!;
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
            mLastValue = new int[2] { 0, 10 };
            mCurrValue = new int[2] { 0, 10 };
        }

        public override bool CanPassFilter(Mob tMob)
            => !mIsFilteringActive | (tMob.mLevel >= mCurrValue[0] && tMob.mLevel <= mCurrValue[1]);

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
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue);
        }
    }
}
