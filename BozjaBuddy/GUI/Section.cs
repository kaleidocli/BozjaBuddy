using BozjaBuddy.Interface;
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

        public abstract bool DrawGUI();

        public abstract void DrawGUIDebug();

        public abstract void Dispose();
        protected virtual List<int> SortTableContent(List<int> pIds, Filter.Filter[] pFilters)
        {
            ImGuiTableSortSpecsPtr tColIndexToSort = ImGui.TableGetSortSpecs();
            unsafe
            {
                if (tColIndexToSort.SpecsCount != 0 && tColIndexToSort.SpecsDirty)
                {
                    return pFilters[tColIndexToSort.Specs.ColumnIndex].Sort(
                        pIds,
                        tColIndexToSort.Specs.SortDirection == ImGuiSortDirection.Ascending ? true : false
                        );
                }
            }
            return pIds;
        }
    }
}
