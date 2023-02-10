using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.IGMarkup
{
    public class IGMarkupBlock_Table : IGMarkupBlock
    {
        private Queue<Queue<IGMarkupBlock>[]> mRows = new Queue<Queue<IGMarkupBlock>[]>();
        public IGMarkupBlock_Table(string pRawBlock, int pCallerScope = 1)
        {
            this.mScope = pCallerScope + 1;
            this.ProcessBlock(pRawBlock);
        }
        protected override void ProcessBlock(string pRawBlock)
        {
            string[] tRows = pRawBlock.Split("---", StringSplitOptions.RemoveEmptyEntries);
            foreach(string tRow in tRows)
            {
                string[] tBlockGroups = tRow.Split("|||", StringSplitOptions.RemoveEmptyEntries);
                this.mRows.Enqueue(new Queue<IGMarkupBlock>[] {
                        IGMarkup.ProcessBlockGroup(tBlockGroups[0], this.mScope),
                        IGMarkup.ProcessBlockGroup(tBlockGroups.Length > 1 ? tBlockGroups[1] : " ", this.mScope)
                        });
            }
        }

        public override void DrawGUI()
        {
            ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
            int tColCount = 2;
            float TABLE_SIZE_Y = ImGui.GetTextLineHeightWithSpacing() * 9;
            if (ImGui.BeginTable($"table {this.mScope}", tColCount, TABLE_FLAG, new System.Numerics.Vector2(0.0f, TABLE_SIZE_Y)))
            {
                foreach (Queue<IGMarkupBlock>[] tRow in this.mRows)
                {
                    ImGui.TableNextRow();
                    for (int tCol = 0; tCol < tColCount; tCol++)
                    {
                        ImGui.TableSetColumnIndex(tCol);
                        foreach (IGMarkupBlock tBlock in tRow[tCol])
                        {
                            tBlock.DrawGUI();
                        }
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}
