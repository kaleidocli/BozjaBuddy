using ImGuiNET;
using BozjaBuddy;
using BozjaBuddy.Data;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.FateCeTableSection
{
    internal class FilterStatus : Filter
    {
        public override string mFilterName { get; set; } = "status";
        private new int mLastValue = default!;
        private new int mCurrValue = default!;

        public FilterStatus()
        {
            Init();
        }
        public FilterStatus(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }
        protected override void Init()
        {
            mLastValue = 0;
            mCurrValue = 0;
        }

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
            {

                return tIDs.OrderBy(
                    id => this.mPlugin.mBBDataManager.mFates[id].mCSFate is not null 
                    ? this.mPlugin.mBBDataManager.mFates[id].mCSFate!.State + this.mPlugin.mBBDataManager.mFates[id].mCSFate!.Progress
                    : 0
                    ).ToList();
            }
            else
                return tIDs.OrderByDescending(
                    id => this.mPlugin.mBBDataManager.mFates[id].mCSFate is not null
                    ? this.mPlugin.mBBDataManager.mFates[id].mCSFate!.State + this.mPlugin.mBBDataManager.mFates[id].mCSFate!.Progress
                    : 0
                    ).ToList();
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNormal(this.mFilterName);
        }
    }
}
