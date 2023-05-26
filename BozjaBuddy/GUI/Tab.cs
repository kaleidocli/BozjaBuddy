using ImGuiNET;
using BozjaBuddy.Interface;
using System.Collections.Generic;
using BozjaBuddy.Utils;

namespace BozjaBuddy.GUI
{
    /// <summary>
    /// Represents a Tab of a Window
    /// </summary>
    internal abstract class Tab : IDrawable
    {
        protected abstract string mName { get; set; }
        protected abstract Dictionary<int, Section> mSortedSections { get; set; }
        protected abstract Plugin mPlugin { get; set; }

        
        public virtual bool DrawGUI()
        {
            if (ImGui.BeginTabItem(this.mName))
            {
                bool tRes = true;
                Section? tSection;
                for (int i = 0; i < mSortedSections.Count; i++)
                {
                    if (this.mSortedSections.TryGetValue(i, out tSection))
                    {
                        ImGui.Separator();
                        tRes = tRes
                                ? (tSection.DrawGUI()
                                    ? true
                                    : false)
                                : false;
                    }
                }
                ImGui.EndTabItem();
                return tRes;
            }
            return false;
        }
    }
}
