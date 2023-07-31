using BozjaBuddy.Data;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterFragment : Filter
    {
        public override string mFilterName { get; set; } = "fragment";
        protected Dictionary<int, Tuple<string, bool>?> _cacheRes = new();

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
            if (!this._cacheRes.ContainsKey(pLostAction.mId))
            {
                this._cacheRes.TryAdd(pLostAction.mId, null);
            }
            if (this.mCurrValue.Length == 0)
            {
                this._cacheRes[pLostAction.mId] = null;
                return true;
            }
            if (this._cacheRes[pLostAction.mId] != null && this._cacheRes[pLostAction.mId]!.Item1 == this.mCurrValue)
            {
                return this._cacheRes[pLostAction.mId]!.Item2;
            }

            foreach (int iID in pLostAction.mLinkFragments)
            {
                Fragment tFragment = this.mPlugin!.mBBDataManager.mFragments[iID];
                if (tFragment.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    this._cacheRes[pLostAction.mId] = new(this.mCurrValue, true);
                    return this._cacheRes[pLostAction.mId]!.Item2;
                }
            }
            this._cacheRes[pLostAction.mId] = new(this.mCurrValue, false);
            return this._cacheRes[pLostAction.mId]!.Item2;
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
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;

    }
}
