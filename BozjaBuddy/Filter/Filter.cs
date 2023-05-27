using System;
using System.Collections.Generic;
using BozjaBuddy.Data;
using BozjaBuddy.GUI;
using Dalamud.Utility;

namespace BozjaBuddy.Filter
{
    /// <summary>
    /// Represents 1. Column header's GUI
    ///            2. The filter that is used to filter table content
    /// </summary>
    internal abstract class Filter
    {
        protected GUIFilter mGUI = new GUIFilter();

        public abstract string mFilterName { get; set; }
        protected bool mIsEdited = false;
        protected string mLastValue = string.Empty;
        protected string mCurrValue = string.Empty;
        protected bool mIsFilteringActive = true;
        public bool mIsSortingActive = false;
        public bool mIsContainedInCell = true;
        protected Plugin? mPlugin = null;

        protected virtual void Init() { }

        public bool IsEdited() => mIsEdited;
        public virtual bool HasChanged() => mCurrValue.Equals(mCurrValue);
        public virtual void ClearInputValue() => this.mCurrValue = string.Empty;
        public virtual bool CanPassFilter(LostAction pLostAction) => true;
        public virtual bool CanPassFilter(Fragment pFragment) => true;
        public virtual bool CanPassFilter(Fate tFate) => true;
        public virtual bool CanPassFilter(Mob tMob) => true;
        public virtual bool CanPassFilter(Loadout tLoadout) => true;
        public virtual bool CanPassFilter(FieldNote tFieldNote) => true;
        protected bool CanPassFilter(string pEntityValue)
            => !mIsFilteringActive | pEntityValue.Contains(mCurrValue.ToString(), StringComparison.CurrentCultureIgnoreCase);

        protected bool CanPassFilter(int pEntityValue)
            => !mIsFilteringActive | mCurrValue.Equals(pEntityValue);
        public virtual bool IsFiltering() => !this.mCurrValue.IsNullOrEmpty();
        public virtual void ResetCurrValue() => this.mCurrValue = "";
        public virtual List<int> Sort(List<int> tIDs, bool pIsAscending = true) => tIDs;
        public abstract void DrawFilterGUI();
        public virtual void EnableFiltering(bool pOption) { mIsFilteringActive = pOption; }

        public virtual string GetCurrValue() => "";
    }
}
