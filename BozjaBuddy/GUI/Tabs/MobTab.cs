using SamplePlugin.GUI.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.GUI.Tabs
{
    internal class MobTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Plugin mPlugin { get; set; }
        public MobTab(Plugin pPlugin)
        {
            this.mName = "Mob";
            this.mPlugin = pPlugin;
            this.mSortedSections = new Dictionary<int, Section>() {
                { 0, new MobTableSection(this.mPlugin) },
                { 1, new AuxiliaryViewerSection(this.mPlugin) }
            };
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
