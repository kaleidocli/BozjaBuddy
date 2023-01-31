using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ImGuizmoNET;
using SamplePlugin.Data;
using SamplePlugin.GUI;

namespace SamplePlugin.Filter
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
        protected Plugin? mPlugin = null;

        protected virtual void Init() { }

        public bool IsEdited() => mIsEdited;
        public virtual bool HasChanged() => mCurrValue.Equals(mCurrValue);
        public virtual bool CanPassFilter(LostAction pLostAction) => true;
        public virtual bool CanPassFilter(Fragment pFragment) => true;
        public virtual bool CanPassFilter(Fate tFate) => true;
        public virtual bool CanPassFilter(Mob tMob) => true;
        protected bool CanPassFilter(string pEntityValue)
            => !mIsFilteringActive | pEntityValue.Contains(mCurrValue.ToString(), StringComparison.CurrentCultureIgnoreCase);

        protected bool CanPassFilter(int pEntityValue)
            => !mIsFilteringActive | mCurrValue.Equals(pEntityValue);
        public virtual List<int> Sort(List<int> tIDs, bool pIsAscending = true) => tIDs;
        public abstract void DrawFilterGUI();
        public virtual void EnableFiltering(bool pOption) { mIsFilteringActive = pOption; }

        public virtual string GetCurrValue() => "";
    }
}
