using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.LostActionTableSection
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

        public override bool CanPassFilter(LostAction pLostAction) => CanPassFilter(pLostAction.mName);
        public override bool CanPassFilter(Fragment pFragment) => CanPassFilter(pFragment.mName);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
