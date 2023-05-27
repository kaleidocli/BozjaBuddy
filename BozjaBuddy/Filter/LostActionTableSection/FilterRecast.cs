using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterRecast : Filter
    {
        public override string mFilterName { get; set; } = "recast";
        private new int[] mCurrValue;

        public FilterRecast()
        {
            mCurrValue = new int[2] { 0, 9999 };
        }

        public FilterRecast(bool pIsFilteringActive = true)
        {
            mCurrValue = new int[2] { 0, 9999 };
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | (pLostAction.mRecast >= mCurrValue[0] && pLostAction.mRecast <= mCurrValue[1]);
        public override bool IsFiltering() => this.mCurrValue[0] > 0 || this.mCurrValue[1] < 9999;
        public override void ResetCurrValue() { this.mCurrValue[0] = 0; this.mCurrValue[1] = 9999; }

        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNormal(mFilterName);
        }
    }
}
