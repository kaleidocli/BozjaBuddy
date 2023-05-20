using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.FieldNoteTableSection
{
    internal class FilterDescription : Filter
    {
        public override string mFilterName { get; set; } = "description";

        public FilterDescription() { }

        public FilterDescription(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(FieldNote tFieldNote) => this.CanPassFilter(tFieldNote.mDescription);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}