using BozjaBuddy.Interface;
using Dalamud.Game.ClientState.Keys;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI
{
    /// <summary>
    /// Represents a Section in a Tab
    /// </summary>
    internal abstract class Section : IDrawable, IDisposable
    {
        protected abstract Plugin mPlugin { get; set; }
        protected virtual int mLastSortedColIndex { get; set; } = -1;
        protected virtual List<int> mLastSortedIdList { get; set; } = new List<int>();

        public abstract bool DrawGUI();

        public abstract void DrawGUIDebug();

        public abstract void Dispose();
        protected virtual List<int> SortTableContent(List<int> pIds, Filter.Filter[] pFilters)
        {
            ImGuiTableSortSpecsPtr tColIndexToSort = ImGui.TableGetSortSpecs();
            if (tColIndexToSort.SpecsCount != 0 && tColIndexToSort.Specs.ColumnIndex == this.mLastSortedColIndex)
            {
                return mLastSortedIdList;
            }
            unsafe
            {
                if (tColIndexToSort.SpecsCount != 0 && tColIndexToSort.SpecsDirty)
                {
                    this.mLastSortedColIndex = tColIndexToSort.Specs.ColumnIndex;
                    this.mLastSortedIdList = pFilters[tColIndexToSort.Specs.ColumnIndex].Sort(
                        pIds,
                        tColIndexToSort.Specs.SortDirection == ImGuiSortDirection.Ascending ? true : false
                        );
                    return this.mLastSortedIdList;
                }
            }
            return pIds;
        }
    }
}
