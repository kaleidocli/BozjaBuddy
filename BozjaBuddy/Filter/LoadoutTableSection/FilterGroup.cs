using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.LoadoutTableSection
{
    internal class FilterGroup : Filter
    {
        public override string mFilterName { get; set; } = "group";

        public FilterGroup() { 
            Init();
        }

        public FilterGroup(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(Loadout pLoadout) => CanPassFilter(pLoadout.mGroup);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
