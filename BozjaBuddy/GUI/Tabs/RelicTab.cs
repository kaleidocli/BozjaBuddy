﻿using BozjaBuddy.GUI.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.Tabs
{
    internal class RelicTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Dictionary<int, Section> mSortedSections_Default { get; set; }
        protected override Plugin mPlugin { get; set; }
        public RelicTab(Plugin pPlugin)
        {
            this.mName = "Relic";
            this.mPlugin = pPlugin;
            this.mSortedSections = new Dictionary<int, Section>() {
                { 0, new RelicSection(this.mPlugin) },
                { 1, new AuxiliaryViewerSection(this.mPlugin) }
            };
            this.mSortedSections_Default = this.mSortedSections;
        }
        public override bool DrawGUI()
        {
            return base.DrawGUI();
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
