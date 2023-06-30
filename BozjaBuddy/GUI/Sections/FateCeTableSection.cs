using ImGuiNET;
using ImGuiScene;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Fates;
using BozjaBuddy.Utils;

namespace BozjaBuddy.GUI.Sections
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
            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 1.23f);
            this.FIXED_LINE_HEIGHT = (float)(ImGui.GetTextLineHeight() * 1);

            this.mFilters = new Filter.Filter[] {
                new Filter.FateCeTableSection.FilterType(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterName(),
                new Filter.FateCeTableSection.FilterStatus(false, this.mPlugin, true),
                new Filter.FilterNone(false, " "),
                new Filter.FateCeTableSection.FilterFieldNote(true, this.mPlugin),
                new Filter.FateCeTableSection.FilterLocation(),
                new Filter.FateCeTableSection.FilterMettle(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterExp(true, this.mPlugin, true),
                new Filter.FateCeTableSection.FilterTome(true, this.mPlugin, true)
            };
            FateCeTableSection.COLUMN_COUNT = this.mFilters.Length;
            this.mFateIDs = this.mPlugin.mBBDataManager.mFates.Keys.ToList();
            this.mTextureCollection = new TextureCollection(this.mPlugin);
        }

        private bool CheckFilter(BozjaBuddy.Data.Fate pEntity)
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
                List<int> tIDs = SortTableContent(this.mFateIDs, this.mFilters);
                DrawTableContent(tIDs);

                ImGui.EndTable();
            }
        }

        private void DrawTableHeader()
        {
            ImGui.TableSetupScrollFreeze(2, 1);

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
            this.DrawTableContent(this.mFateIDs);
        }

        private void DrawTableContent(List<int> pIDs)
        {
            // Trying to update all Fate's status
            unsafe { BBDataManager.UpdateAllFateStatus(this.mPlugin); }
            // CONTENT
            List<int> tTableFateIds = this.mPlugin.FateTable
                                        .Select(o => Convert.ToInt32(o.FateId))
                                        .ToList();
            foreach (int iID in pIDs)
            {
                this.DrawTableRow(iID, tTableFateIds);
            }
        }
        private void DrawTableRow(int pID, List<int> pTableFateIds)
        {
            BozjaBuddy.Data.Fate tFate = this.mPlugin.mBBDataManager.mFates[pID];
            if (!this.CheckFilter(tFate)) return;

            ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(1, 0));
            TextureWrap? tIconWrap = this.mTextureCollection.GetStandardTexture(Convert.ToUInt32(tFate.mType));

            for (int i = 0; i < FateCeTableSection.COLUMN_COUNT; i++)
            {
                ImGui.TableSetColumnIndex(i);
                switch (i)
                {
                    case 0:
                        if (tIconWrap != null) ImGui.Image(tIconWrap.ImGuiHandle, Utils.Utils.ResizeToIcon(this.mPlugin, tIconWrap!));
                        break;
                    case 1:
                        UtilsGUI.SelectableLink_WithPopup(mPlugin, tFate.mName, tFate.GetGenId());
                        break;
                    case 2:
                        if (tFate.mDynamicEvent != null)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg,
                            tFate.mDynamicEvent.Value.State == FFXIVClientStructs.FFXIV.Client.UI.Agent.MycDynamicEventState.Underway
                                ? ImGui.GetColorU32(UtilsGUI.Colors.TableCell_Yellow)
                                : ImGui.GetColorU32(UtilsGUI.Colors.TableCell_Green)
                                );
                            ImGui.TextUnformatted($"{tFate.mDynamicEvent.Value.State}");
                        }
                        else if (tFate.mCSFate is not null)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 
                                tFate.mCSFate.State == FateState.Running
                                ? ImGui.GetColorU32(UtilsGUI.Colors.TableCell_Yellow)
                                : ImGui.GetColorU32(UtilsGUI.Colors.TableCell_Green)
                                );
                            ImGui.TextUnformatted($"{tFate.mCSFate!.Progress} %");
                        }
                        else
                        {
                            ImGui.Text(
                                tFate.mLastActive.HasValue
                                ? $"{Math.Round((DateTime.Now - tFate.mLastActive.Value).TotalMinutes, MidpointRounding.ToPositiveInfinity)}m ago"
                                : "-----");
                        }
                        break;
                    case 3:
                        ImGui.PushID($"a{tFate.mId}");
                        UtilsGUI.ACPUFateCeButton(this.mPlugin, tFate.mId, tFate.mName);
                        ImGui.PopID();
                        break;
                    case 4:
                        foreach (int iiID in tFate.mLinkFieldNotes)
                        {
                            FieldNote tFieldNote = this.mPlugin.mBBDataManager.mFieldNotes[iiID];
                            ImGui.PushID($"{iiID}{pID}");
                            UtilsGUI.SelectableLink_WithPopup(mPlugin, tFieldNote.mName, tFieldNote.GetGenId());
                            ImGui.PopID();
                        }
                        break;
                    case 5:
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFate.mLocation!);
                        break;
                    case 6:
                        ImGui.Text($"{Utils.Utils.FormatNum(tFate.mRewardMettleMin)} - {Utils.Utils.FormatNum(tFate.mRewardMettleMax)}");
                        break;
                    case 7:
                        ImGui.Text($"{Utils.Utils.FormatNum(tFate.mRewardExpMin)} - {Utils.Utils.FormatNum(tFate.mRewardExpMax)}");
                        break;
                    case 8:
                        ImGui.Text($"{tFate.mRewardTome}");
                        break;

                    default: break;
                }
            }

            ImGui.PopStyleVar();
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
