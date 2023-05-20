using BozjaBuddy.Data;
using ImGuiNET;
using ImGuiScene;

namespace BozjaBuddy.Filter.FieldNoteTableSection
{
    internal class FilterName : Filter
    {
        public override string mFilterName { get; set; } = "name";

        public FilterName() { }

        public FilterName(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(FieldNote tFieldNote) => this.CanPassFilter(tFieldNote.mName);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
