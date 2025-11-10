using BozjaBuddy.Data;
using BozjaBuddy.Interface;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.GUI
{
    /// <summary>
    /// Represents a Section in a Tab
    /// </summary>
    public abstract class Section : IDrawable, IDisposable
    {
        protected abstract Plugin mPlugin { get; set; }
        protected virtual int mLastSortedColIndex { get; set; } = -1;
        protected virtual ImGuiSortDirection mLastSortedDirection { get; set; } = ImGuiSortDirection.None;
        protected virtual List<int> mLastSortedIdList { get; set; } = new List<int>();
        public bool mIsForcingSort = false;

        public abstract bool DrawGUI();

        public abstract void DrawGUIDebug();

        public abstract void Dispose();
        protected virtual void RefreshIdList<T>(List<int> pIds, Dictionary<int, T> pDbDict)  where T : GeneralObject
        {
            int index = 0;
            while (index < pIds.Count)
            {
                if (!pDbDict.ContainsKey(pIds[index]))
                    pIds.Remove(pIds[index]);
                index++;
            }
            foreach (int iId in pDbDict.Keys)
            {
                if (!pIds.Contains(iId))
                {
                    pIds.Add(iId);
                }
            }
        }
        protected virtual List<int> SortTableContent(List<int> pIds, Filter.Filter[] pFilters)
        {
            ImGuiTableSortSpecsPtr tColIndexToSort = ImGui.TableGetSortSpecs();
            if (tColIndexToSort.SpecsCount != 0 && tColIndexToSort.SpecsDirty)
            {
                if (tColIndexToSort.Specs.ColumnIndex == this.mLastSortedColIndex 
                    && tColIndexToSort.Specs.SortDirection == this.mLastSortedDirection
                    && !this.mIsForcingSort)
                    return mLastSortedIdList;
            }
            else return pIds;
            unsafe
            {
                this.mLastSortedColIndex = tColIndexToSort.Specs.ColumnIndex;
                this.mLastSortedIdList = pFilters[tColIndexToSort.Specs.ColumnIndex].Sort(
                    pIds,
                    tColIndexToSort.Specs.SortDirection == ImGuiSortDirection.Ascending ? true : false
                    );
                this.mIsForcingSort = false;
                return this.mLastSortedIdList;
            }
        }
    }
}
