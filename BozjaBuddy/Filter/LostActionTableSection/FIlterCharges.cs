using SamplePlugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.Filter.LostActionTableSection
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
            mLastValue = new int[2] { 0, 9999 };
            mCurrValue = new int[2] { 0, 9999 };
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | (pLostAction.mCharges >= mCurrValue[0] && pLostAction.mCharges <= mCurrValue[1]);
        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNumberInputPair(mFilterName, ref mCurrValue);
        }
    }
}
