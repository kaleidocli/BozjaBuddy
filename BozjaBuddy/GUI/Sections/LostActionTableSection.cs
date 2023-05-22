using ImGuiNET;
using ImGuiScene;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Components;
using BozjaBuddy.Utils;
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
        private bool mIsCompactModeActive = false;
        private List<int> mActionIDs;
        private Filter.Filter[] mFilters;
        private TextureCollection mTextureCollection;

        protected override Plugin mPlugin { get; set; }
        private float FIXED_LINE_HEIGHT
        {
            get
            {
                return ImGui.GetTextLineHeight() * (this.mIsCompactModeActive ? 1 : 3);
            }
            set { this.FIXED_LINE_HEIGHT = value; }
        }


        public LostActionTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;

            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;

            this.mFilters = new Filter.Filter[]{
                new Filter.LostActionTableSection.FilterType(),
                new Filter.LostActionTableSection.FilterName(),
                new Filter.LostActionTableSection.FilterRole(),
                new Filter.LostActionTableSection.FilterFragment(true, this.mPlugin, true),
                new Filter.LostActionTableSection.FilterDescription(),
                new Filter.LostActionTableSection.FilterWeight(true, this.mPlugin, true),
                new Filter.LostActionTableSection.FilterCharges(),
                new Filter.LostActionTableSection.FilterCast(false),
                new Filter.LostActionTableSection.FilterRecast(false)
            };
            LostActionTableSection.COLUMN_COUNT = this.mFilters.Length;

            this.mActionIDs = this.mPlugin.mBBDataManager.mLostActions.Keys.ToList();

            if (UtilsGameData.kTexCol_LostAction != null)
            {
                this.mTextureCollection = UtilsGameData.kTexCol_LostAction;
            }
            else
            {
                this.mTextureCollection = new TextureCollection(this.mPlugin);
                this.mTextureCollection.AddTextureFromItemId(this.mActionIDs);
            }
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
            DrawOptionBar();
            ImGui.Separator();
            DrawTable();
            return true;
        }

        private void DrawTable()
        {
            if (ImGui.BeginTable("##LostAction", LostActionTableSection.COLUMN_COUNT, LostActionTableSection.TABLE_FLAG, new System.Numerics.Vector2(0.0f, this.TABLE_SIZE_Y)))
            {
                DrawTableHeader();
                List<int> tIDs = SortTableContent(this.mActionIDs, this.mFilters);
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
                if (this.mFilters[iCol].IsFiltering())
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.TableCell_Red));

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
                            if (tIconWrap != null)
                                ImGui.Image(tIconWrap.ImGuiHandle,
                                    this.mIsCompactModeActive
                                    ? Utils.Utils.ResizeToIcon(this.mPlugin, tIconWrap!)
                                    : new System.Numerics.Vector2(tIconWrap.Width * 0.75f, tIconWrap.Height * 0.75f));
                            AuxiliaryViewerSection.GUILoadoutEditAdjuster(this.mPlugin, iID);
                            break;
                        case 1:
                            UtilsGUI.SelectableLink_WithPopup(mPlugin, tLostAction.mName, tLostAction.GetGenId(), pIsShowingCacheAmount: true);
                            break; 
                        case 2:
                            UtilsGUI.DrawRoleFlagAsIconString(this.mPlugin, tLostAction.mRole);
                            break;
                        case 3:
                            foreach (int iiID in tLostAction.mLinkFragments)
                            {
                                Fragment tFragment = this.mPlugin.mBBDataManager.mFragments[iiID];
                                ImGui.PushTextWrapPos(0);
                                UtilsGUI.SelectableLink_WithPopup(mPlugin, tFragment.mName + $"  »##{iID}", tFragment.GetGenId()); ImGui.PopTextWrapPos();
                            }
                            break;
                        case 4:
                            if (this.mIsCompactModeActive)
                            {
                                ImGui.Text("<hover to view>");
                                if (ImGui.IsItemHovered())
                                    ImGui.SetTooltip(tLostAction.mDescription_semi);
                            }
                            else
                                ImGui.TextUnformatted(tLostAction.mDescription_semi);
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
        private void DrawOptionBar()
        {
            AuxiliaryViewerSection.GUIAlignRight("Compact mode      ");
            //ImGui.Checkbox("Compact", ref this.mIsCompactModeActive);
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Compact mode");
            ImGui.SameLine();
            ImGuiComponents.ToggleButton("comTog", ref this.mIsCompactModeActive);
        }
        public override void DrawGUIDebug()
        {
            ImGui.Text(String.Format("Edited: {0} {1}", this.mFilters[0].GetCurrValue(),
                                                        this.mFilters[1].GetCurrValue()));
        }

        public override void Dispose()
        {
            this.mTextureCollection.Dispose();
        }
    }
}
