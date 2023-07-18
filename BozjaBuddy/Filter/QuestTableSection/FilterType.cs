using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.QuestTableSection
{
    internal class FilterType : Filter
    {
        public override string mFilterName { get; set; } = "type";
        private new Quest.QuestType mCurrValue = default!;

        public FilterType()
        {
            Init();
        }
        public FilterType(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(Quest pQuest)
            => !mIsFilteringActive
                | mCurrValue == Quest.QuestType.None
                | mCurrValue == pQuest.mType;
        public override bool CanPassFilter(Fragment pFragment) => true;
        public override bool IsFiltering() => this.mCurrValue != Quest.QuestType.None;
        public override void ResetCurrValue() { this.mCurrValue = Quest.QuestType.None; }

        public override void DrawFilterGUI()
        {
            Quest.QuestType[] tValues = { 
                Quest.QuestType.None,   
                Quest.QuestType.Msq,
                Quest.QuestType.Side,
                Quest.QuestType.Key,
                Quest.QuestType.Repeatable
            };
            mGUI.HeaderComboEnum("", ref mCurrValue, tValues, tValues[0]);
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
