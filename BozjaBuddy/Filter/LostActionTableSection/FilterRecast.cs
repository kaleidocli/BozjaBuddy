using SamplePlugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.Filter.LostActionTableSection
{
    internal class FilterRecast : Filter
    {
        public override string mFilterName { get; set; } = "recast";
        private new int[] mLastValue;
        private new int[] mCurrValue;

        public FilterRecast()
        {
            mLastValue = new int[2] { 0, 9999 };
            mCurrValue = new int[2] { 0, 9999 };
        }

        public FilterRecast(bool pIsFilteringActive = true)
        {
            mLastValue = new int[2] { 0, 9999 };
            mCurrValue = new int[2] { 0, 9999 };
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | (pLostAction.mRecast >= mCurrValue[0] && pLostAction.mRecast <= mCurrValue[1]);
        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            mGUI.HeaderNormal(mFilterName);
        }
    }
}
