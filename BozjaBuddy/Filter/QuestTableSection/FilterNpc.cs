using BozjaBuddy.Data;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.QuestTableSection
{
    internal class FilterNpc : Filter
    {
        public override string mFilterName { get; set; } = "npc";

        public FilterNpc() { }

        public FilterNpc(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(Quest pQuest) => CanPassFilter(pQuest.mIssuerName);

        public override void DrawFilterGUI()
        {
            mGUI.HeaderTextInput(mFilterName, ref mCurrValue, ref mIsEdited, this);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
