using BozjaBuddy.Data;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.Filter.FateCeTableSection
{
    internal class FilterFieldNote : Filter
    {
        public override string mFilterName { get; set; } = "field note";
        protected Dictionary<int, Tuple<string, bool>?> _cacheRes = new();

        public FilterFieldNote() { }

        public FilterFieldNote(bool pIsFilteringActive, Plugin? pPlugin = null)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
        }

        public override bool CanPassFilter(Fate pFate)
        {
            if (this.mPlugin == null | !this.mIsFilteringActive) return true;
            if (!this._cacheRes.ContainsKey(pFate.mId))
            {
                this._cacheRes.TryAdd(pFate.mId, null);
            }
            if (this.mCurrValue.Length == 0)
            {
                this._cacheRes[pFate.mId] = null;
                return true;
            }
            if (this._cacheRes[pFate.mId] != null && this._cacheRes[pFate.mId]!.Item1 == this.mCurrValue)
            {
                return this._cacheRes[pFate.mId]!.Item2;
            }

            foreach (int iID in pFate.mLinkFieldNotes)
            {
                if (!this.mPlugin!.mBBDataManager.mFieldNotes.TryGetValue(iID, out FieldNote? tFieldNote) || tFieldNote == null) continue;
                if (tFieldNote.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    this._cacheRes[pFate.mId] = new(this.mCurrValue, true);
                    return this._cacheRes[pFate.mId]!.Item2;
                }
            }
            this._cacheRes[pFate.mId] = new(this.mCurrValue, false);
            return this._cacheRes[pFate.mId]!.Item2;
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
