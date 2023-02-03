using Dalamud.Logging;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterFragment : Filter
    {
        public override string mFilterName { get; set; } = "fragment";

        public FilterFragment() { }

        public FilterFragment(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }

        public override bool CanPassFilter(LostAction pLostAction)
        {
            if (this.mPlugin == null | !this.mIsFilteringActive) return true;
            foreach (int iID in pLostAction.mLinkFragments)
            {
                Fragment tFragment = this.mPlugin!.mBBDataManager.mFragments[iID];
                return tFragment.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(
                    id => this.mPlugin!.mBBDataManager.mLostActions[id].mLinkFragments.Count == 0 
                            ? 0
                            : this.mPlugin.mBBDataManager.mLostActions[id].mLinkFragments[0]
                    ).ToList();
            else
                return tIDs.OrderByDescending(
                    id => this.mPlugin!.mBBDataManager.mLostActions[id].mLinkFragments.Count == 0
                            ? 0
                            : this.mPlugin.mBBDataManager.mLostActions[id].mLinkFragments[0]
                    ).ToList();
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(this.mFilterName, ref this.mCurrValue, ref this.mIsEdited);
            //mGUI.HeaderNormal(mFilterName);
        }

        public override string GetCurrValue() => mCurrValue;

    }
}
