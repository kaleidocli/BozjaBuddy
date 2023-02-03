using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterCast : Filter
    {
        public override string mFilterName { get; set; } = "cast";
        private new int[] mLastValue = default!;
        private new int[] mCurrValue = default!;

        public FilterCast()
        {
            Init();
        }
        public FilterCast(bool pIsFilteringActive)
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
            => !mIsFilteringActive | (pLostAction.mCast >= mCurrValue[0] && pLostAction.mCast <= mCurrValue[1]);
        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNormal(mFilterName);
        }
    }
}
