using BozjaBuddy.Data;
using Dalamud.Bindings.ImGui;

namespace BozjaBuddy.Filter
{
    internal class FilterNone : Filter
    {
        public override string mFilterName { get; set; } = "none";

        protected FilterNone() { }

        public FilterNone(bool pIsFilteringActive, string pFilterName)
        {
            this.mFilterName = pFilterName;
            Init();
            EnableFiltering(pIsFilteringActive);
        }

        public override void DrawFilterGUI()
        {
            ImGui.Text(this.mFilterName);
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
