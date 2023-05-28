using BozjaBuddy.GUI.Sections;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.GUI.Tabs
{
    internal class MobTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Dictionary<int, Section> mSortedSections_Default { get; set; }
        protected override Plugin mPlugin { get; set; }
        public MobTab(Plugin pPlugin)
        {
            this.mName = "Mob";
            this.mPlugin = pPlugin;
            this.mSortedSections = new Dictionary<int, Section>() {
                { 0, new WeatherBarSection(this.mPlugin) },
                { 1, new MobTableSection(this.mPlugin) },
                { 2, new AuxiliaryViewerSection(this.mPlugin) }
            };
            this.mSortedSections_Default = this.mSortedSections;
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
