using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.MobTableSection
{
    internal class FilterLocation : Filter
    {
        public override string mFilterName { get; set; } = "location";
        private new Location.Area mLastValue = default!;
        private new Location.Area mCurrValue = default!;

        public FilterLocation()
        {
            Init();
        }
        public FilterLocation(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(Mob pMob)
            => !mIsFilteringActive
                | mCurrValue == Location.Area.None
                | mCurrValue == pMob.mLocation?.mAreaFlag;

        public override void DrawFilterGUI()
        {
            Location.Area[] tValues = { Location.Area.None,
                                        Location.Area.Bozja_Zone1,
                                        Location.Area.Bozja_Zone2,
                                        Location.Area.Bozja_Zone3,
                                        Location.Area.Zadnor_Zone1,
                                        Location.Area.Zadnor_Zone2,
                                        Location.Area.Zadnor_Zone3,
                                        Location.Area.Castrum,
                                        Location.Area.Dalriada,
                                        Location.Area.Delubrum,
                                        Location.Area.DelubrumSavage};
            mGUI.HeaderComboEnum("", ref mCurrValue, tValues, tValues[0]);
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
