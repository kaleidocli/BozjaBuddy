using ImGuiNET;
using BozjaBuddy.Interface;
using System.Collections.Generic;
using BozjaBuddy.Utils;
using BozjaBuddy.GUI.Sections;

namespace BozjaBuddy.GUI
{
    /// <summary>
    /// Represents a Tab of a Window
    /// </summary>
    internal abstract class Tab : IDrawable
    {
        protected abstract string mName { get; set; }
        protected abstract Dictionary<int, Section> mSortedSections { get; set; }
        protected abstract Dictionary<int, Section> mSortedSections_Default { get; set; }
        protected abstract Plugin mPlugin { get; set; }

        /// <summary>
        /// Do not create new Section. Find and put front Auxi section by default
        /// </summary>
        public virtual void RearrangeSection()
        {
            if (this.mPlugin.Configuration.isAuxiVisible == 2)          // Auxi-only
            {
                if (this.mSortedSections.Count < 2) return;

                Dictionary<int, Section> tRes = new();
                int tResKey = 1; // save 0 idx for focused Section
                Section? tFocusedSection = null;
                foreach (var iKey in mSortedSections.Keys)
                {
                    if (mSortedSections[iKey] is AuxiliaryViewerSection)
                    {
                        tFocusedSection = mSortedSections[iKey];
                    }
                    else
                    {
                        tRes[tResKey] = mSortedSections[iKey];
                        tResKey++;
                    }
                }
                if (tFocusedSection == null) { return; }
                tRes.Add(0, tFocusedSection);
                this.mSortedSections = tRes;
            }
            else if (this.mPlugin.Configuration.isAuxiVisible == 0)     // Auxi hidden
            {
                Dictionary<int, Section> tRes = new();
                int tResKey = 0;
                foreach (var iKey in mSortedSections_Default.Keys)
                {
                    if (mSortedSections[iKey] is not AuxiliaryViewerSection)
                    {
                        tRes[tResKey] = mSortedSections[iKey];
                        tResKey++;
                    }
                }
                this.mSortedSections = tRes;
            }
            else if (this.mSortedSections_Default != null)              // Default
            {
                this.mSortedSections = this.mSortedSections_Default;
            }
        }
        
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
