using BozjaBuddy.Data;
using System;

namespace BozjaBuddy.Filter.FateCeTableSection
{
    internal class FilterFieldNote : Filter
    {
        public override string mFilterName { get; set; } = "field note";

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
            foreach (int iID in pFate.mLinkFieldNotes)
            {
                if (!this.mPlugin!.mBBDataManager.mFieldNotes.TryGetValue(iID, out FieldNote? tFieldNote)) continue;
                if (tFieldNote == null) continue;
                if (tFieldNote.mName.Contains(this.mCurrValue, StringComparison.CurrentCultureIgnoreCase)) return true;
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
