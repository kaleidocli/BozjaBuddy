using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using ImGuiScene;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace BozjaBuddy.GUI.Sections
{
    /// <summary>
    /// A Section featuring a table of Lost Actions with filters
    /// </summary>
    internal class LostActionTableSection : Section, IDisposable
    {
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        const ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable    | ImGuiTableFlags.RowBg    |
                                     ImGuiTableFlags.ScrollY           | ImGuiTableFlags.Reorderable  | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        float FIXED_LINE_HEIGHT;
        private List<int> mActionIDs;
        private Filter.Filter[] mFilters;
        private TextureCollection mTextureCollection;

        protected override Plugin mPlugin { get; set; }



        public LostActionTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;

            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;
            this.FIXED_LINE_HEIGHT = (float)(ImGui.GetTextLineHeight() * 3);

            this.mFilters = new Filter.Filter[]{
                new Filter.LostActionTableSection.FilterType(),
                new Filter.LostActionTableSection.FilterName(),
                new Filter.LostActionTableSection.FilterRole(),
                new Filter.LostActionTableSection.FilterDescription(),
                new Filter.LostActionTableSection.FilterFragment(true, this.mPlugin, true),
                new Filter.LostActionTableSection.FilterWeight(true, this.mPlugin, true),
                new Filter.LostActionTableSection.FilterCharges(),
                new Filter.LostActionTableSection.FilterCast(false),
                new Filter.LostActionTableSection.FilterRecast(false)
            };
            LostActionTableSection.COLUMN_COUNT = this.mFilters.Length;

            this.mActionIDs = this.mPlugin.mBBDataManager.mLostActions.Keys.ToList();

            this.mTextureCollection = new TextureCollection(this.mPlugin);
            this.mTextureCollection.AddTextureFromItemId(this.mActionIDs);
        }

        private bool CheckFilter(LostAction pEntity)
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
            if (ImGui.BeginTable("##LostAction", LostActionTableSection.COLUMN_COUNT, LostActionTableSection.TABLE_FLAG, new System.Numerics.Vector2(0.0f, this.TABLE_SIZE_Y)))
            {
                DrawTableHeader();
                List<int> tIDs = SortTableContent();
                DrawTableContent(tIDs);

                ImGui.EndTable();
            }
        }

        private void DrawTableHeader()
        {
            ImGui.TableSetupScrollFreeze(2, 1);

            for (int i = 0; i < this.mFilters.Length; i++)
                ImGui.TableSetupColumn(this.mFilters[i].mFilterName,
                                        this.mFilters[i].mIsSortingActive ? ImGuiTableColumnFlags.None : ImGuiTableColumnFlags.NoSort,
                                        0.0f,
                                        (uint)i);

            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            for (int iCol = 0; iCol < LostActionTableSection.COLUMN_COUNT; iCol++)
            {
                ImGui.TableSetColumnIndex(iCol);
                ImGui.PushID(ImGui.TableGetColumnName(iCol));
                ImGui.PushItemWidth(ImGui.GetColumnWidth(iCol) - LostActionTableSection.HEADER_TEXT_FIELD_SIZE_OFFSET);
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
            this.DrawTableContent(this.mActionIDs);
        }

        private void DrawTableContent(List<int> pIDs)
        {
            // CONTENT
            foreach (int iID in pIDs)
            {
                LostAction tLostAction = this.mPlugin.mBBDataManager.mLostActions[iID];
                if (!this.CheckFilter(tLostAction)) continue;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().CellPadding.Y));
                TextureWrap? tIconWrap = this.mTextureCollection.GetTextureFromItemId(Convert.ToUInt32(iID));
                for (int i = 0; i < LostActionTableSection.COLUMN_COUNT; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    switch (i)
                    {
                        case 0:
                            if (tIconWrap != null) ImGui.Image(tIconWrap.ImGuiHandle, new System.Numerics.Vector2(tIconWrap.Width * 0.75f, tIconWrap.Height * 0.75f));
                            break;
                        case 1:
                            ImGui.PushTextWrapPos(0); AuxiliaryViewerSection.GUISelectableLink(mPlugin, tLostAction.mName, tLostAction.GetGenId()); ImGui.PopTextWrapPos();
                            break; 
                        case 2:
                            ImGui.Text(RoleFlag.FlagToString(tLostAction.mRole.mRoleFlagBit));
                            break;
                        case 3:
                            ImGui.TextUnformatted(tLostAction.mDescription_semi);
                            break;
                        case 4:
                            foreach (int iiID in tLostAction.mLinkFragments)
                            {
                                Fragment tFragment = this.mPlugin.mBBDataManager.mFragments[iiID];
                                ImGui.PushTextWrapPos(0); AuxiliaryViewerSection.GUISelectableLink(mPlugin, tFragment.mName + $"##{iID}", tFragment.GetGenId()); ImGui.PopTextWrapPos();
                            }
                            break;
                        case 5:
                            ImGui.Text(tLostAction.mWeight.ToString());
                            break; 
                        case 6:
                            ImGui.Text(String.Format("{0}/{1}", tLostAction.mCharges.ToString(), tLostAction.mCharges.ToString()));
                            break;
                        case 7:
                            ImGui.Text(String.Format("{0} s", tLostAction.mRecast.ToString()));
                            break;
                        case 8:
                            ImGui.Text(String.Format("{0} s", tLostAction.mCast.ToString()));
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
                    this.mActionIDs, 
                    tColIndexToSort.Specs.SortDirection == ImGuiSortDirection.Ascending ? true : false
                    );
            }
            return this.mActionIDs;
        }

        public override void DrawGUIDebug()
        {
            ImGui.Text(String.Format("Edited: {0} {1}", this.mFilters[0].GetCurrValue(),
                                                        this.mFilters[1].GetCurrValue()));
        }

        public void DrawTableDebug()
        {
        }

        public override void Dispose()
        {
            this.mTextureCollection.Dispose();
        }
    }
}
