using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.LoadoutTableSection
{
    internal class FilterGroup : Filter
    {
        public override string mFilterName { get; set; } = "group";

        public FilterGroup() { 
            Init();
        }

        public FilterGroup(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(Loadout pLoadout) => CanPassFilter(pLoadout.mGroup);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
