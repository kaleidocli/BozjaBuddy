using BozjaBuddy.Data;
using BozjaBuddy.Utils;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.Sections
{
    internal class FieldNoteTableSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }
        private Filter.Filter[] mFilters;
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        protected static ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        protected static ImGuiTableFlags GRID_FLAG = ImGuiTableFlags.SizingFixedFit |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        private float TABLE_HEADER_HEIGHT = 45;
        float FIXED_LINE_HEIGHT;
        private List<int> mFieldNoteIds;
        private TextureCollection mTextureCollection;

        public FieldNoteTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;
            this.FIXED_LINE_HEIGHT = (float)(ImGui.GetTextLineHeight() * 1);

            this.mFilters = new Filter.Filter[] {
                new Filter.FieldNoteTableSection.FilterOwn(this.mPlugin),
                new Filter.FieldNoteTableSection.FilterName(),
                new Filter.FieldNoteTableSection.FilterRarity(true, this.mPlugin, true),
                new Filter.FieldNoteTableSection.FitlerFate(this.mPlugin),
                new Filter.FieldNoteTableSection.FilterDescription()
            };
            FieldNoteTableSection.COLUMN_COUNT = this.mFilters.Length;
            this.mFieldNoteIds = this.mPlugin.mBBDataManager.mFieldNotes.Keys.OrderBy(o => mPlugin.mBBDataManager.mFieldNotes.TryGetValue(o, out FieldNote? value) && value != null
                                                                                           ? value.mNumber
                                                                                           : 0)
                                                                             .ToList();
            if (UtilsGameData.kTexCol_FieldNote != null)
            {
                this.mTextureCollection = UtilsGameData.kTexCol_FieldNote;
            }
            else
            {
                this.mTextureCollection = new TextureCollection(this.mPlugin);
                this.mTextureCollection.AddTextureFromItemId(this.mFieldNoteIds, pSheet: TextureCollection.Sheet.FieldNote);
            }
        }
        private bool CheckFilter(FieldNote pEntity)
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

            // debug misc
            //this.DrawTableDebug();
            return true;
        }

        private void DrawTable()
        {
            if (ImGui.BeginTable(
                    "##FieldNote", 
                    FieldNoteTableSection.COLUMN_COUNT, 
                    FieldNoteTableSection.TABLE_FLAG, 
                    new System.Numerics.Vector2(0.0f, this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection
                                                      ? this.TABLE_HEADER_HEIGHT
                                                      : this.TABLE_SIZE_Y)
                    ))
            {
                DrawTableHeader();
                if (!this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection)
                {
                    List<int> tIDs = SortTableContent(this.mFieldNoteIds, this.mFilters);
                    DrawTableContent(tIDs);
                }
                ImGui.EndTable();
            }
            if (this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection)
            {
                DrawGridContent(this.mFieldNoteIds);
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
            for (int iCol = 0; iCol < FieldNoteTableSection.COLUMN_COUNT; iCol++)
            {
                ImGui.TableSetColumnIndex(iCol);
                ImGui.PushID(ImGui.TableGetColumnName(iCol));
                ImGui.PushItemWidth(ImGui.GetColumnWidth(iCol) - FieldNoteTableSection.HEADER_TEXT_FIELD_SIZE_OFFSET);
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
            ImGuiIOPtr io = ImGui.GetIO();
            // CONTENT
            foreach (int iID in pIDs)
            {
                FieldNote tFieldNote = this.mPlugin.mBBDataManager.mFieldNotes[iID];
                if (!this.CheckFilter(tFieldNote)) continue;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(1, 0));
                TextureWrap? tIconWrap = this.mTextureCollection.GetTextureFromItemId(
                                Convert.ToUInt32(tFieldNote.mId), 
                                pSheet: TextureCollection.Sheet.FieldNote);

                for (int i = 0; i < FieldNoteTableSection.COLUMN_COUNT; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    switch (i)
                    {
                        case 0:
                            if (tIconWrap != null
                                && UtilsGUI.SelectableLink_Image(
                                        this.mPlugin,
                                        tFieldNote.GetGenId(),
                                        tIconWrap,
                                        pIsLink: true,
                                        pIsAuxiLinked: !io.KeyShift,
                                        pImageScaling: 0.5f,
                                        pImageOverlayRGBA: this.mPlugin.Configuration.mUserFieldNotes.Contains(tFieldNote.mId)
                                                            ? null
                                                            : new Vector4(1, 1, 1, 0.25f),
                                        pAdditionalHoverText: "[Shift + LMB] Mark as obtained/unobtained\n"
                                        )
                                && io.KeyShift)
                            {
                                if (this.mPlugin.Configuration.mUserFieldNotes.Add(tFieldNote.mId))
                                {
                                    this.mPlugin.Configuration.Save();
                                }
                                else
                                {
                                    this.mPlugin.Configuration.mUserFieldNotes.Remove(tFieldNote.mId);
                                    this.mPlugin.Configuration.Save();
                                }
                            }
                            break;
                        case 1:
                            UtilsGUI.SelectableLink_WithPopup(mPlugin, tFieldNote.mName, tFieldNote.GetGenId());
                            break;
                        case 2:
                            ImGui.Text($"{tFieldNote.mRarity}");
                            break;
                        case 3:
                            foreach (int iId in tFieldNote.mLinkFates)
                            {
                                if (!this.mPlugin.mBBDataManager.mFates.TryGetValue(iId, out var tFate) || tFate == null) continue;
                                UtilsGUI.SelectableLink_WithPopup(this.mPlugin, tFate.mName, tFate.GetGenId());
                            }
                            break;
                        case 4:
                            ImGui.PushTextWrapPos();
                            ImGui.Text(tFieldNote.mDescription);
                            ImGui.PopTextWrapPos();
                            break;
                        default: break;
                    }
                }

                ImGui.PopStyleVar();
            }
        }
        private void DrawGridContent(List<int> pIDs)
        {
            int tColCount = 10;
            int tRowCount = 5;
            if (ImGui.BeginTable(
                    "##gridfieldnote",
                    tColCount,
                    FieldNoteTableSection.GRID_FLAG,
                    new Vector2(0.0f, this.TABLE_SIZE_Y - this.TABLE_HEADER_HEIGHT)
                    ))
            {
                for (int iRow = 0; iRow < tRowCount; iRow++)
                {
                    ImGui.TableNextRow();
                    for (int iCol = 0; iCol < tColCount; iCol++)
                    {
                        int iIndex = iCol + iRow * 10;
                        ImGui.TableSetColumnIndex(iCol);
                        if (iIndex >= pIDs.Count) continue;
                        float tCellWidth = (ImGui.GetWindowSize().X - ImGui.GetStyle().FramePadding.X * 2 - ImGui.GetStyle().ItemSpacing.X * (tColCount - 1)) / tColCount;
                        this.DrawGridCell(pIDs[iIndex], tCellWidth);
                    }
                }
                ImGui.EndTable();
            }
        }
        private void DrawGridCell(int pId, float pCellWidth)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            // CONTENT
            var tAnchor = ImGui.GetCursorPos();
            if (!this.mPlugin.mBBDataManager.mFieldNotes.TryGetValue(pId, out FieldNote? tFieldNote)) return;
            if (tFieldNote == null) { return; }
            bool tIsValid = this.CheckFilter(tFieldNote);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(1, 0));
            TextureWrap? tIconWrap = this.mTextureCollection.GetTextureFromItemId(
                            Convert.ToUInt32(tFieldNote.mId),
                            pSheet: TextureCollection.Sheet.FieldNote);
            float tScaling = 1;
            if (tIconWrap != null) tScaling = (pCellWidth - 1) / tIconWrap.Width;       // account for frame padding 
            if (tIconWrap != null
                && UtilsGUI.SelectableLink_Image(
                        this.mPlugin,
                        tFieldNote.GetGenId(),
                        tIconWrap,
                        pIsLink: true,
                        pIsAuxiLinked: !io.KeyShift,
                        pImageScaling: tScaling,
                        pImageOverlayRGBA: tIsValid 
                                            ? this.mPlugin.Configuration.mUserFieldNotes.Contains(tFieldNote.mId)
                                                ? null
                                                : new Vector4(1, 1, 1, 0.25f)
                                            : new Vector4(1, 1, 1, 0),
                        pAdditionalHoverText: "[Shift + LMB] Mark as obtained/unobtained\n"
                        )
                && io.KeyShift)
            {
                if (this.mPlugin.Configuration.mUserFieldNotes.Add(tFieldNote.mId))
                {
                    this.mPlugin.Configuration.Save();
                }
                else
                {
                    this.mPlugin.Configuration.mUserFieldNotes.Remove(tFieldNote.mId);
                    this.mPlugin.Configuration.Save();
                }
            }
            // Note Number
            ImGui.SetCursorPos(tAnchor + new Vector2(0, -2));
            UtilsGUI.TextDescriptionForWidget(tFieldNote.mNumber.ToString());

            ImGui.PopStyleVar();
        }
        
        private void DrawOptionBar()
        {
            ImGui.Text("Scan for obtained Notes:");
            ImGui.SameLine();
            UtilsGUI.TextDescriptionForWidget("Type \"/collection\", choose \"Field Record\", then go through the page numbers.");
            //ImGui.SameLine();
            //UtilsGUI.ShowHelpMarker("1. Type \"/collection\".\n2. Choose [Field Record]\n3. Go through the page numbers at the bottom of Field Record window. (no need to go through the records)");
            ImGui.SameLine();
            AuxiliaryViewerSection.GUIAlignRight(1);
            if (ImGuiComponents.IconButton(this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection
                             ? FontAwesomeIcon.GripHorizontal
                             : FontAwesomeIcon.List))
            {
                this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection = !this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection;
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem($"Current view mode: {(this.mPlugin.Configuration.mIsInGridMode_FieldNoteTableSection ? "GRID" : "LIST")}");
            }
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
