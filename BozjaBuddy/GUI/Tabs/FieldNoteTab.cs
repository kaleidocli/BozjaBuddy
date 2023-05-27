using BozjaBuddy.GUI.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.Tabs
{
    internal class FieldNoteTab : Tab, IDisposable
    {
        protected override string mName { get; set; }
        protected override Dictionary<int, Section> mSortedSections { get; set; }
        protected override Plugin mPlugin { get; set; }

        public FieldNoteTab(Plugin pPlugin)
        {
            this.mName = "Field Note";
            this.mPlugin = pPlugin;
            mSortedSections = new Dictionary<int, Section>() {
                { 0, new FieldNoteTableSection(this.mPlugin) },
                { 1, new AuxiliaryViewerSection(this.mPlugin) }
            };
        }

        public void Dispose()
        {
            foreach (Section iSection in this.mSortedSections.Values) iSection.Dispose();
        }
    }
}
