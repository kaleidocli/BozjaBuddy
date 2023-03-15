using BozjaBuddy.Data;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterType : Filter
    {
        public override string mFilterName { get; set; } = "type";
        private new LostActionType mLastValue = default!;
        private new LostActionType mCurrValue = default!;

        public FilterType()
        {
            Init();
        }
        public FilterType(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive
                | mCurrValue == LostActionType.None
                | mCurrValue == pLostAction.mType;
        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            LostActionType[] tValues = { LostActionType.None,
                                         LostActionType.Offensive,
                                         LostActionType.Defensive,
                                         LostActionType.Restorative,
                                         LostActionType.Beneficial,
                                         LostActionType.Tactical,
                                         LostActionType.Detrimental,
                                         LostActionType.Item };
            mGUI.HeaderComboEnum("", ref mCurrValue, tValues, tValues[0]);
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
