using BozjaBuddy.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.QuestTableSection
{
    internal class FilterQuestChains : Filter
    {
        public override string mFilterName { get; set; } = "Quest Chain";
        protected Dictionary<int, Tuple<string, bool>?> _cacheRes = new();

        public FilterQuestChains() { }

        public FilterQuestChains(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }

        public override bool CanPassFilter(Quest pQuest)
        {
            if (this.mPlugin == null | !this.mIsFilteringActive) return true;
            if (this.mCurrValue.Length == 0)
            {
                this._cacheRes[pQuest.mId] = null;
                return true;
            }
            if (this._cacheRes[pQuest.mId] != null && this._cacheRes[pQuest.mId]!.Item1 == this.mCurrValue)
            {
                return this._cacheRes[pQuest.mId]!.Item2;
            }

            foreach (int iID in pQuest.mQuestChains)
            {
                if (!this.mPlugin!.mBBDataManager.mQuestChains.TryGetValue(iID, out var tQuestChain) || tQuestChain == null) continue;
                if (tQuestChain.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    this._cacheRes[pQuest.mId] = new(this.mCurrValue, true);
                    return this._cacheRes[pQuest.mId]!.Item2;
                }
            }
            this._cacheRes[pQuest.mId] = new(this.mCurrValue, false);
            return this._cacheRes[pQuest.mId]!.Item2;
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
