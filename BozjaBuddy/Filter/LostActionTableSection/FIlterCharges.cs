using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterCharges : Filter
    {
        public override string mFilterName { get; set; } = "charges";
        private new int[] mLastValue = default!;
        private new int[] mCurrValue = default!;

        public FilterCharges()
        {
            Init();
        }
        public FilterCharges(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }
        protected override void Init()
        {
            mLastValue = new int[2] { 0, 99 };
            mCurrValue = new int[2] { 0, 99 };
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | pLostAction.mCharges > 100 | (pLostAction.mCharges >= mCurrValue[0] && pLostAction.mCharges <= mCurrValue[1]);
        public override bool IsFiltering() => this.mCurrValue[0] > 0 || this.mCurrValue[1] < 99;
        public override void ResetCurrValue() { this.mCurrValue[0] = 0; this.mCurrValue[1] = 99; }

        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue, this);
        }
    }
}
