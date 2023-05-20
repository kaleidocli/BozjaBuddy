using BozjaBuddy.Data;
using BozjaBuddy.Utils;
using Dalamud.Logging;
using ImGuiNET;
using System.Runtime.CompilerServices;

namespace BozjaBuddy.Filter.FieldNoteTableSection
{
    internal class FilterOwn : Filter
    {
        public override string mFilterName { get; set; } = "owned";
        public new string mCurrValue = "all";

        private FilterOwn() { }

        public FilterOwn(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public override bool CanPassFilter(FieldNote tFieldNote)
        {
            return mCurrValue switch
            {
                "all" => true,
                "owned" => this.mPlugin != null && this.mPlugin.Configuration.mUserFieldNotes.Contains(tFieldNote.mId),
                "missing" => this.mPlugin != null && !this.mPlugin.Configuration.mUserFieldNotes.Contains(tFieldNote.mId),
                _ => true
            };
        }
        public override bool IsFiltering() => this.mCurrValue != "all";
        public override void ResetCurrValue() { this.mCurrValue = "all"; }

        public override void DrawFilterGUI()
        {
            if (ImGui.Button(mCurrValue switch
                {
                    "all"       => "  ALL  ",
                    "owned"     => " OWN ",
                    "missing"   => "\t~\t",
                    _ => "-----"
                }
            ))
            {
                switch (mCurrValue)
                {
                    case "all": mCurrValue = "owned"; break;
                    case "owned": mCurrValue = "missing"; break;
                    case "missing": mCurrValue = "all"; break;
                    default: mCurrValue = "all"; break;
                }
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem(
                    mCurrValue switch
                    {
                        "all"       => "Visible: all",
                        "owned"     => "Visible: obtained notes",
                        "missing"   => "Visible: missing notes",
                        _ => "----"
                    }
                    );
            }
        }

        public override string GetCurrValue() => mCurrValue;
    }
}
