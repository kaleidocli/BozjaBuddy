using BozjaBuddy.GUI.GUIAssist;
using BozjaBuddy.GUI.Sections;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.GUI.Tabs
{
    internal class FateCeTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Dictionary<int, Section> mSortedSections_Default { get; set; }
        protected override Plugin mPlugin { get; set; }
        public FateCeTab(Plugin pPlugin)
        {
            this.mName = "Fate/CE";
            this.mPlugin = pPlugin;
            this.mSortedSections = new Dictionary<int, Section>() {
                { 0, new WeatherBarSection(this.mPlugin) },
                { 1, new FateCeTableSection(this.mPlugin) },
                { 2, new AuxiliaryViewerSection(this.mPlugin) }
            };
            this.mSortedSections_Default = this.mSortedSections;
        }
        public override bool DrawGUI()
        {
            bool tRes = base.DrawGUI();
            if (tRes)
            {
                this.mPlugin.GUIAssistManager.RequestOption(this.GetHashCode(), GUIAssistManager.GUIAssistOption.MycInfoBoxAlarm);
            }
            else
            {
                this.mPlugin.GUIAssistManager.UnrequestOption(this.GetHashCode(), GUIAssistManager.GUIAssistOption.MycInfoBoxAlarm);
            }
            return tRes;
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
