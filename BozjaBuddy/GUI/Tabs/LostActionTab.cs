using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        protected override Plugin mPlugin { get; set; }

        public LostActionTab(Plugin pPlugin)
        {
            this.mName = "Lost Action";
            this.mPlugin = pPlugin;
            mSortedSections = new Dictionary<int, Section>() {
                { 0, new LostActionTableSection(this.mPlugin) },
                { 1, new AuxiliaryViewerSection(this.mPlugin) }
            };
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
