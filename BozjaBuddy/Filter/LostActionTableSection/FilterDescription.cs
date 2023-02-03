using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterDescription : Filter
    {
        public override string mFilterName { get; set; } = "description";

        public FilterDescription()
        {
            Init();
        }
        public FilterDescription(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(LostAction pLostAction) => CanPassFilter(pLostAction.mDescription_semi);
        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
