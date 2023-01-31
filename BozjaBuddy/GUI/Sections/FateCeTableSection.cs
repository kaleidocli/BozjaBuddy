using ImGuiNET;
using ImGuiScene;
using SamplePlugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.GUI.Sections
{
    internal class FateCeTableSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }
        private Filter.Filter[] mFilters;
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        protected static ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        float FIXED_LINE_HEIGHT;
        private List<int> mFateIDs;
        private TextureCollection mTextureCollection;

        public FateCeTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;
            this.FIXED_LINE_HEIGHT = (float)(ImGui.GetTextLineHeight() * 1);

            this.mFilters= new Filter.Filter[] {
                new Filter.FateCeTableSection.FilterType(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterName(),
                new Filter.FateCeTableSection.FilterMettle(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterExp(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterTome(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterLocation()
            };
            FateCeTableSection.COLUMN_COUNT = this.mFilters.Length;
            this.mFateIDs = this.mPlugin.mBBDataManager.mFates.Keys.ToList();
            this.mTextureCollection = new TextureCollection(this.mPlugin);
        }

        private bool CheckFilter(Fate pEntity)
        {
            foreach (var iFilter in this.mFilters)
            {
                if (!iFilter.CanPassFilter(pEntity)) return false;
            }
            return true;
        }

        public override bool DrawGUI()
        {
            DrawTable();


            // debug misc
            //this.DrawTableDebug();
            return true;
        }

        private void DrawTable()
        {
            if (ImGui.BeginTable("##FateCe", FateCeTableSection.COLUMN_COUNT, FateCeTableSection.TABLE_FLAG, new System.Numerics.Vector2(0.0f, this.TABLE_SIZE_Y)))
            {
                DrawTableHeader();
                List<int> tIDs = SortTableContent();
                DrawTableContent(tIDs);

                ImGui.EndTable();
            }
        }

        private void DrawTableHeader()
        {
            ImGui.TableSetupScrollFreeze(1, 1);

            for (int i = 0; i < this.mFilters.Length; i++)
                ImGui.TableSetupColumn(this.mFilters[i].mFilterName,
                                        this.mFilters[i].mIsSortingActive ? ImGuiTableColumnFlags.DefaultSort : ImGuiTableColumnFlags.NoSort,
                                        0.0f,
                                        (uint)i);

            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            for (int iCol = 0; iCol < FateCeTableSection.COLUMN_COUNT; iCol++)
            {
                ImGui.TableSetColumnIndex(iCol);
                ImGui.PushID(ImGui.TableGetColumnName(iCol));
                ImGui.PushItemWidth(ImGui.GetColumnWidth(iCol) - FateCeTableSection.HEADER_TEXT_FIELD_SIZE_OFFSET);
                ImGui.PushTextWrapPos(0);

                this.mFilters[iCol].DrawFilterGUI(); ImGui.SameLine();

                ImGui.PopTextWrapPos();
                ImGui.TableHeader("");
                ImGui.PopItemWidth();
                ImGui.PopID();
            }
        }

        private void DrawTableContent()
        {
            this.DrawTableContent(this.mFateIDs);
        }

        private void DrawTableContent(List<int> pIDs)
        {
            // CONTENT
            foreach (int iID in pIDs)
            {
                Fate tFate = this.mPlugin.mBBDataManager.mFates[iID];
                if (!this.CheckFilter(tFate)) continue;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(1, 0));
                TextureWrap? tIconWrap = this.mTextureCollection.GetStandardTexture(Convert.ToUInt32(tFate.mType));

                for (int i = 0; i < FateCeTableSection.COLUMN_COUNT; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    switch (i)
                    {
                        case 0:
                            if (tIconWrap != null) ImGui.Image(tIconWrap.ImGuiHandle, new System.Numerics.Vector2(tIconWrap.Width, tIconWrap.Height));
                            break; 
                        case 1:
                            AuxiliaryViewerSection.GUISelectableLink(mPlugin, tFate.mName, tFate.GetGenId());
                            break;
                        case 2:
                            ImGui.Text($"{tFate.mRewardMettleMin} - {tFate.mRewardMettleMax}");
                            break;
                        case 3:
                            ImGui.Text($"{tFate.mRewardExpMin} - {tFate.mRewardExpMax}");
                            break;
                        case 4:
                            ImGui.Text($"{tFate.mRewardTome}");
                            break;
                        case 5:
                            AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, tFate.mLocation!);
                            break;
                        default: break;
                    }
                }

                ImGui.PopStyleVar();
            }
        }

        private List<int> SortTableContent()
        {
            ImGuiTableSortSpecsPtr tColIndexToSort = ImGui.TableGetSortSpecs();
            if (tColIndexToSort.SpecsDirty)
            {
                return this.mFilters[tColIndexToSort.Specs.ColumnIndex].Sort(
                    this.mFateIDs,
                    tColIndexToSort.Specs.SortDirection == ImGuiSortDirection.Ascending ? true : false
                    );
            }
            return this.mFateIDs;
        }

        public override void DrawGUIDebug()
        {

        }

        public override void Dispose()
        {
            this.mTextureCollection.Dispose();
        }
    }
}
