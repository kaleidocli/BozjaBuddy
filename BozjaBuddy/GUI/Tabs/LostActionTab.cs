using System;
using System.Collections.Generic;
using BozjaBuddy.GUI.Sections;

namespace BozjaBuddy.GUI.Tabs
{
    /// <summary>
    /// A Tab featuring a LostActionTableSection and an AuxilaryViewerSection
    /// </summary>
    internal class LostActionTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Dictionary<int, Section> mSortedSections_Default { get; set; }
        protected override Plugin mPlugin { get; set; }

        public LostActionTab(Plugin pPlugin)
        {
            this.mName = "Lost Action/Fragment";
            this.mPlugin = pPlugin;
            mSortedSections = new Dictionary<int, Section>() {
                { 0, new LostActionTableSection(this.mPlugin) },
                { 1, new AuxiliaryViewerSection(this.mPlugin) }
            };
            this.mSortedSections_Default = this.mSortedSections;
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
