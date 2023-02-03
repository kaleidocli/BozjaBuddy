using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.FateCeTableSection
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

        public override bool CanPassFilter(Fate tFate) => this.CanPassFilter(tFate.mName);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
