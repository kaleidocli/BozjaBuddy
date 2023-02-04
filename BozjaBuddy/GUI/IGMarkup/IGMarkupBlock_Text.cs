using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace BozjaBuddy.GUI.IGMarkup
{
    public class IGMarkupBlock_Text : IGMarkupBlock
    {
        private string mText = "";
        public IGMarkupBlock_Text(string pRawBlock, int pCallerScope)
        {
            this.mScope = pCallerScope;
            this.ProcessBlock(pRawBlock);
        }
        protected override void ProcessBlock(string pRawBlock)
        {
            this.mText = pRawBlock.Trim(' ', '\n');
        }

        public override void DrawGUI()
        {
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(this.mText);
            ImGui.PopTextWrapPos();
        }
    }
}
