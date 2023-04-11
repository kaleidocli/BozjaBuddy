using BozjaBuddy.Data;
using Dalamud.Interface.Components;
using ImGuiNET;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using BozjaBuddy.Utils;

namespace BozjaBuddy.GUI.Sections
{
    internal class LoadoutTableSection : Section
    {
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        const ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        float FIXED_LINE_HEIGHT;
        private List<int> mLoadoutIds;
        private Filter.Filter[] mFilters;
        private bool mIsShowingRec = true;

        protected override Plugin mPlugin { get; set; }



        public LoadoutTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;

            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;
            this.FIXED_LINE_HEIGHT = (float)(ImGui.GetTextLineHeight() * 1);

            this.mFilters = new Filter.Filter[]{
                new Filter.LoadoutTableSection.FilterName(),
                new Filter.LoadoutTableSection.FilterRole(),
                new Filter.LoadoutTableSection.FilterGroup()
            };
            LoadoutTableSection.COLUMN_COUNT = this.mFilters.Length;

            this.mLoadoutIds = this.mPlugin.mBBDataManager.mLoadouts.Keys.ToList();
        }

        private bool CheckFilter(Loadout pEntity)
        {
            foreach (var iFilter in this.mFilters)
            {
                if (!iFilter.CanPassFilter(pEntity)) return false;
            }
            return true;
        }

        public override bool DrawGUI()
        {
            if (AuxiliaryViewerSection.mIsRefreshRequired)
            {
                this.RefreshIdList<Loadout>(this.mLoadoutIds, this.mPlugin.mBBDataManager.mLoadouts);
                AuxiliaryViewerSection.mIsRefreshRequired = false;
            }
            DrawOptionBar();
            ImGui.Separator();
            DrawTable();

            // debug misc
            //this.DrawTableDebug();
            return true;
        }

        private void DrawTable()
        {
            if (ImGui.BeginTable("##Loadout", LoadoutTableSection.COLUMN_COUNT, LoadoutTableSection.TABLE_FLAG, new System.Numerics.Vector2(0.0f, this.TABLE_SIZE_Y)))
            {
                DrawTableHeader();
                List<int> tIDs =  SortTableContent(this.mLoadoutIds, this.mFilters);
                DrawTableContent(tIDs, this.mIsShowingRec);

                ImGui.EndTable();
            }
        }
        private void DrawTableHeader()
        {
            ImGui.TableSetupScrollFreeze(0, 1);

            for (int i = 0; i < this.mFilters.Length; i++)
                ImGui.TableSetupColumn(this.mFilters[i].mFilterName,
                                        this.mFilters[i].mIsSortingActive ? ImGuiTableColumnFlags.None : ImGuiTableColumnFlags.NoSort,
                                        0.0f,
                                        (uint)i);

            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            for (int iCol = 0; iCol < LoadoutTableSection.COLUMN_COUNT; iCol++)
            {
                ImGui.TableSetColumnIndex(iCol);
                ImGui.PushID(ImGui.TableGetColumnName(iCol));
                ImGui.PushItemWidth(ImGui.GetColumnWidth(iCol) - LoadoutTableSection.HEADER_TEXT_FIELD_SIZE_OFFSET);
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
            this.DrawTableContent(this.mLoadoutIds);
        }
        private void DrawTableContent(List<int> pIDs, bool pIsShowingRec = true)
        {
            // CONTENT
            foreach (int iID in pIDs)
            {
                if (!pIsShowingRec && iID > 9999) continue;
                Loadout tLoadout = this.mPlugin.mBBDataManager.mLoadouts[iID];
                if (!this.CheckFilter(tLoadout)) continue;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().CellPadding.Y));
                for (int i = 0; i < LoadoutTableSection.COLUMN_COUNT; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    switch (i)
                    {
                        case 0:
                            UtilsGUI.SelectableLink_WithPopup(mPlugin, tLoadout.mName, tLoadout.GetGenId());
                            break;
                        case 1:
                            ImGui.Text(RoleFlag.FlagToString(tLoadout.mRole.mRoleFlagBit));
                            break;
                        case 2:
                            ImGui.TextUnformatted(tLoadout.mGroup);
                            break;
                        default: break;
                    }
                }
                ImGui.PopStyleVar();
            }
        }

        private void DrawOptionBar()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            AuxiliaryViewerSection.GUIAlignRight((float)(21*12.7));
            // Toggle rec visibility
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Recommended loadouts");
            ImGui.SameLine();
            ImGuiComponents.ToggleButton("recToggle", ref this.mIsShowingRec);
            // Restore preset
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin) && io.KeyShift)
            {
                this.mPlugin.mBBDataManager.ReloadLoadoutsPreset();
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + RMB] to return recommended loadouts to default. (user's loadouts are intact)"); }
            ImGui.SameLine();
            ImGui.Text(" | ");
            ImGui.SameLine();
            // Add
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus) && io.KeyShift)
            {
                // Save to cache
                Loadout tLoadoutNew = new Loadout(this.mPlugin, new LoadoutJson());
                BBDataManager.DynamicAddGeneralObject<Loadout>(this.mPlugin, tLoadoutNew, this.mPlugin.mBBDataManager.mLoadouts);
                this.mIsForcingSort = true;
                // Open Auxiliary tab
                if (!AuxiliaryViewerSection.mTabGenIds[tLoadoutNew.GetGenId()])
                {
                    AuxiliaryViewerSection.AddTab(this.mPlugin, tLoadoutNew.GetGenId());
                }
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + RMB] to add a new entry"); }
            // Import
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.FileImport) && io.KeyShift)
            {
                // Save to cache
                LoadoutJson? tLoadoutJson = JsonSerializer.Deserialize<LoadoutJson>(ImGui.GetClipboardText());
                if (tLoadoutJson != null)
                {
                    Loadout tLoadoutNew = new Loadout(this.mPlugin, tLoadoutJson, true);
                    BBDataManager.DynamicAddGeneralObject<Loadout>(this.mPlugin, tLoadoutNew, this.mPlugin.mBBDataManager.mLoadouts);
                    this.mIsForcingSort = true;
                    // Open Auxiliary tab
                    if (!AuxiliaryViewerSection.mTabGenIds[tLoadoutNew.GetGenId()])
                    {
                        AuxiliaryViewerSection.AddTab(this.mPlugin, tLoadoutNew.GetGenId());
                    }
                }
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + RMB] to import an entry from clipboard"); }
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
        }
    }
}
