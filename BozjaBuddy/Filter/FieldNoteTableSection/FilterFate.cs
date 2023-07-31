using BozjaBuddy.Data;
using Dalamud.Utility;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.Filter.FieldNoteTableSection
{
    internal class FitlerFate : Filter
    {
        public override string mFilterName { get; set; } = "fate";
        protected Dictionary<int, Tuple<string, bool>?> _cacheRes = new();

        public FitlerFate() { }

        public FitlerFate(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override bool CanPassFilter(FieldNote tFieldNote)
        {
            if (this.mPlugin == null | !this.mIsFilteringActive) return true;
            if (!this._cacheRes.ContainsKey(tFieldNote.mId))
            {
                this._cacheRes.TryAdd(tFieldNote.mId, null);
            }
            if (this.mCurrValue.Length == 0)
            {
                this._cacheRes[tFieldNote.mId] = null;
                return true;
            }
            if (this._cacheRes[tFieldNote.mId] != null && this._cacheRes[tFieldNote.mId]!.Item1 == this.mCurrValue)
            {
                return this._cacheRes[tFieldNote.mId]!.Item2;
            }

            foreach (int iID in tFieldNote.mLinkFates)
            {
                if (!this.mPlugin!.mBBDataManager.mFates.TryGetValue(iID, out Fate? tFate) || tFate == null) continue;
                if (tFate.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    this._cacheRes[tFieldNote.mId] = new(this.mCurrValue, true);
                    return this._cacheRes[tFieldNote.mId]!.Item2;
                }
            }
            this._cacheRes[tFieldNote.mId] = new(this.mCurrValue, false);
            return this._cacheRes[tFieldNote.mId]!.Item2;
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
