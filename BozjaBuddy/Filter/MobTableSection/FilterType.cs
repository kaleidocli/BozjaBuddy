using BozjaBuddy.Data;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Filter.MobTableSection
{
    internal class FilterType : Filter
    {
        public override string mFilterName { get; set; } = "type";
        private new Mob.MobType mLastValue = default!;
        private new Mob.MobType mCurrValue = default!;

        public FilterType()
        {
            Init();
        }
        public FilterType(bool pIsFilteringActive, Plugin? pPlugin = null, bool isSortingActive = false)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
            this.mPlugin = pPlugin;
            this.mIsSortingActive = isSortingActive;
        }

        public override bool CanPassFilter(Mob pMob)
            => !mIsFilteringActive
                | mCurrValue == Mob.MobType.None
                | mCurrValue == pMob.mType;

        public override void DrawFilterGUI()
        {
            Mob.MobType[] tValues = { Mob.MobType.None,
                                        Mob.MobType.Normal,
                                        Mob.MobType.Legion,
                                        Mob.MobType.Sprite,
                                        Mob.MobType.Ashkin,
                                        Mob.MobType.Boss};
            mGUI.HeaderComboEnum(" ", ref mCurrValue, tValues, tValues[0]);
        }

        public override List<int> Sort(List<int> tIDs, bool pIsAscending = true)
        {
            if (this.mPlugin == null) return tIDs;
            if (pIsAscending)
                return tIDs.OrderBy(id => this.mPlugin.mBBDataManager.mMobs[id].mType).ToList();
            else
                return tIDs.OrderByDescending(id => this.mPlugin.mBBDataManager.mMobs[id].mType).ToList();
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
