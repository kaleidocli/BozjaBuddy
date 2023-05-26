using ImGuiNET;
using ImGuiScene;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Components;
using BozjaBuddy.Utils;
using Dalamud.Logging;
using System.Numerics;
using Dalamud.Interface;
using System.Security.Cryptography;
using BozjaBuddy.Windows;

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
        protected static ImGuiTableFlags GRID_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter |
                             ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        private float TABLE_HEADER_HEIGHT = 45;
        private bool mIsCompactModeActive = false;
        private List<int> mActionIDs;
        private Filter.Filter[] mFilters;
        private TextureCollection mTextureCollection;
        unsafe ImGuiTextFilterPtr mFilter_CacheAlert1 = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
        unsafe ImGuiTextFilterPtr mFilter_CacheAlert2 = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

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

            //this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;
            this.CalcTableHeight();

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
            if (ImGui.BeginTable(
                    "##LostAction", 
                    LostActionTableSection.COLUMN_COUNT, 
                    LostActionTableSection.TABLE_FLAG, 
                    new System.Numerics.Vector2(0.0f, this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection
                                                      ? this.TABLE_HEADER_HEIGHT
                                                      : this.TABLE_SIZE_Y))
                )
            {
                DrawTableHeader();
                if (!this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection)
                {
                    List<int> tIDs = SortTableContent(this.mActionIDs, this.mFilters);
                    DrawTableContent(tIDs);
                }
                ImGui.EndTable();
            }
            if (this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection)
            {
                this.DrawGridContent(this.mPlugin.mBBDataManager.mUiMap_MycItemBox, 16);
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
        /// <summary>pColCount:        Specifically hard-coded, for cell scaling and performance.</summary>
        private void DrawGridContent(List<List<int>> pMap, int pColCount)
        {
            if (ImGui.BeginTable(
                    "##gridfieldnote",
                    pColCount,
                    LostActionTableSection.GRID_FLAG,
                    new Vector2(0.0f, this.TABLE_SIZE_Y - this.TABLE_HEADER_HEIGHT - 25)
                    ))
            {
                float tCellWidth = 500.95f / pColCount;     // default=538.95
                foreach (List<int> iRow in pMap)
                {
                    ImGui.TableNextRow();
                    int iColIdx = 0;
                    foreach (int iId in iRow)
                    {
                        if (iColIdx >= pColCount) break;
                        ImGui.TableSetColumnIndex(iColIdx);
                        this.DrawGridCell(iId, tCellWidth);
                        iColIdx++;
                    }
                }
                //// Specifically for Lost Elixir
                //if (pMap[^1][^1] == 23922)
                //{
                //    ImGui.TableNextRow();
                //    ImGui.TableSetColumnIndex(0);
                //    this.DrawGridCell(23922, tCellWidth);
                //}
                ImGui.EndTable();
            }
        }
        private void DrawGridCell(int pId, float pCellWidth)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            // CONTENT
            var tAnchor = ImGui.GetCursorPos();
            if (!this.mPlugin.mBBDataManager.mLostActions.TryGetValue(pId, out LostAction? tLostAction)) return;
            if (tLostAction == null) { return; }
            bool tIsValid = this.CheckFilter(tLostAction);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(1, 0));
            TextureWrap? tIconWrap = this.mTextureCollection.GetTextureFromItemId(
                            Convert.ToUInt32(tLostAction.mId),
                            pSheet: TextureCollection.Sheet.Action);
            float tScaling = 1;
            if (tIconWrap != null) tScaling = (pCellWidth - 1) / tIconWrap.Width;       // account for frame padding 
            if (tIconWrap != null
                && UtilsGUI.SelectableLink_Image(
                        this.mPlugin,
                        tLostAction.GetGenId(),
                        tIconWrap,
                        pIsLink: true,
                        pIsAuxiLinked: !io.KeyShift,
                        pImageScaling: tScaling,
                        pImageOverlayRGBA: tIsValid
                                            ? true      // FIXME: Check if currently edited custom loadout contains this action
                                                ? null
                                                : new Vector4(1, 1, 1, 0.25f)
                                            : new Vector4(1, 1, 1, 0)
                        //pAdditionalHoverText: $"[Shift + LMB] \n"
                        )
                && io.KeyShift)
            {
                // FIXME: DO SOMETHING HERE (add to custom loadout being edited)
            }
            // Cache amount
            if (this.mPlugin.Configuration._userCacheData.TryGetValue(pId, out int tAmount))
            {
                var tDrawList = ImGui.GetWindowDrawList();
                var tScreenAnchor = ImGui.GetCursorScreenPos();
                ImGui.SetCursorPos(tAnchor + new Vector2(pCellWidth - 7.25f * tAmount.ToString().Length, pCellWidth - 15));
                tDrawList.AddRectFilled(
                        ImGui.GetCursorScreenPos() + new Vector2(-3, 3.5f),
                        tScreenAnchor + new Vector2(pCellWidth, -1),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_Black, 0.15f))
                    );
                tDrawList.AddText(
                        ImGui.GetCursorScreenPos(),
                        ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.NormalText_Orange),
                        $"{tAmount}"
                    );
                // Action Alert
                if (this.mPlugin.Configuration.mIsAroVisible_LostActionTableSection
                    && CharStatsWindow.CheckActionAlert(this.mPlugin, pId))
                {
                    tDrawList.AddCircleFilled(
                            tScreenAnchor + new Vector2(2, -pCellWidth + 2),
                            3.9f,
                            ImGui.ColorConvertFloat4ToU32(UtilsGUI.AdjustTransparency(UtilsGUI.Colors.MycItemBoxOverlay_Red, 0.8f))
                        );
                }
            }

            ImGui.PopStyleVar();
        }
        private void DrawOptionBar()
        {
            if (this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection)
            {
                // ARO config
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.SlidersH))
                {
                    ImGui.OpenPopup("##alertConfig");
                }
                else { UtilsGUI.SetTooltipForLastItem("Configs for Action-Running-out Alert (also found in Config > Misc > [A])"); }
                if (ImGui.IsPopupOpen("##alertConfig")) ImGui.SetNextWindowSize(new Vector2(550, 450));
                if (ImGui.BeginPopup("##alertConfig"))
                {
                    ConfigWindow.Draw_CacheAlertConfig(this.mPlugin, this.mPlugin.Configuration, this.mFilter_CacheAlert1, this.mFilter_CacheAlert2);
                    ImGui.EndPopup();
                }
                // ARO toggle
                ImGui.SameLine();
                ImGuiComponents.ToggleButton("##togAro", ref this.mPlugin.Configuration.mIsAroVisible_LostActionTableSection);
                ImGui.SameLine();
                UtilsGUI.TextWithHelpMarker(
                    "Action-running-out Alert", 
                    "The below replicates player's Lost find Cache.\n\n- Filterable.\n- The number represents the amount player possesses.\n- The red dot (Action-running-out Alert) notifies actions that are running low. The alert threshold is modifiable, using the outmost left button.\n- Pressing [Shift + LMB] on an action while in Custom loadout edit mode will add that action to the loadout being edited.",
                    UtilsGUI.Colors.BackgroundText_Grey);
                ImGui.SameLine();
            }

            AuxiliaryViewerSection.GUIAlignRight("Compact mode      [GRID]");
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Compact mode");
            ImGui.SameLine();
            ImGuiComponents.ToggleButton("comTog", ref this.mIsCompactModeActive);
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection
                             ? FontAwesomeIcon.GripHorizontal
                             : FontAwesomeIcon.List))
            {
                this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection = !this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection;
                this.CalcTableHeight();
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem($"Current view mode: {(this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection ? "GRID" : "LIST")}");
            }
        }
        private void CalcTableHeight() => this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * (15 + (this.mPlugin.Configuration.mIsInGridMode_LostActionTableSection
                                                                                                        ? 3
                                                                                                        : 2));
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
