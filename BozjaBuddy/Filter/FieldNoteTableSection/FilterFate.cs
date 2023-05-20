using BozjaBuddy.Data;
using Dalamud.Utility;
using System;

namespace BozjaBuddy.Filter.FieldNoteTableSection
{
    internal class FitlerFate : Filter
    {
        public override string mFilterName { get; set; } = "fate";

        public FitlerFate() { }

        public FitlerFate(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override bool CanPassFilter(FieldNote tFieldNote)
        {
            if (this.mCurrValue.IsNullOrEmpty()
                || this.mPlugin == null 
                || !this.mIsFilteringActive) return true;
            foreach (int iID in tFieldNote.mLinkFates)
            {
                this.mPlugin!.mBBDataManager.mFates.TryGetValue(iID, out Fate? tFate);
                if (tFate != null && tFate.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase)) return true;
            }
            return false;
        }

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
