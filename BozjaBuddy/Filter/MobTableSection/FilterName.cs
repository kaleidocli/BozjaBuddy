using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.MobTableSection
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

        public override bool CanPassFilter(Mob tMob) => this.CanPassFilter(tMob.mName);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
