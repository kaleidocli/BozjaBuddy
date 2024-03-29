﻿using BozjaBuddy.GUI.Sections;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.GUI.Tabs
{
    internal class LoadoutTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Dictionary<int, Section> mSortedSections_Default { get; set; }
        protected override Plugin mPlugin { get; set; }

        public LoadoutTab(Plugin pPlugin)
        {
            this.mName = "Custom Loadout";
            this.mPlugin = pPlugin;
            mSortedSections = new Dictionary<int, Section>() {

                { 0, new LoadoutTableSection(this.mPlugin) },
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
