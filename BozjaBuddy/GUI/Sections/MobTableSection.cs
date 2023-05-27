using ImGuiNET;
using ImGuiScene;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using BozjaBuddy.Utils;

namespace BozjaBuddy.GUI.Sections
{
    internal class MobTableSection : Section, IDisposable
    {
        protected override Plugin mPlugin { get; set; }
        private Filter.Filter[] mFilters;
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        protected static ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        float FIXED_LINE_HEIGHT;
        private List<int> mMobIDs;
        private TextureCollection mTextureCollection;

        public MobTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 1.23f);
            this.FIXED_LINE_HEIGHT = (float)(ImGui.GetTextLineHeight() * 1);

            this.mFilters = new Filter.Filter[] {
                new Filter.MobTableSection.FilterType(true, this.mPlugin, true),
                new Filter.MobTableSection.FilterName(),
                new Filter.MobTableSection.FilterLevel(true, this.mPlugin, true),
                new Filter.MobTableSection.FilterLocation()
            };
            MobTableSection.COLUMN_COUNT = this.mFilters.Length;
            this.mMobIDs = this.mPlugin.mBBDataManager.mMobs.Keys.ToList();
            this.mTextureCollection = new TextureCollection(this.mPlugin);
        }

        private bool CheckFilter(Mob pEntity)
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
            if (ImGui.BeginTable("##Mob", MobTableSection.COLUMN_COUNT, MobTableSection.TABLE_FLAG, new System.Numerics.Vector2(0.0f, this.TABLE_SIZE_Y)))
            {
                DrawTableHeader();
                List<int> tIDs = SortTableContent(this.mMobIDs, this.mFilters);
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
            for (int iCol = 0; iCol < MobTableSection.COLUMN_COUNT; iCol++)
            {
                ImGui.TableSetColumnIndex(iCol);
                ImGui.PushID(ImGui.TableGetColumnName(iCol));
                ImGui.PushItemWidth(ImGui.GetColumnWidth(iCol) - MobTableSection.HEADER_TEXT_FIELD_SIZE_OFFSET);
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
            this.DrawTableContent(this.mMobIDs);
        }

        private void DrawTableContent(List<int> pIDs)
        {
            // CONTENT
            foreach (int iID in pIDs)
            {
                Mob tMob = this.mPlugin.mBBDataManager.mMobs[iID];
                if (!this.CheckFilter(tMob)) continue;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(1, 0));
                TextureWrap? tIconWrap = this.mTextureCollection.GetStandardTexture(Convert.ToUInt32(tMob.mType));

                for (int i = 0; i < MobTableSection.COLUMN_COUNT; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    switch (i)
                    {
                        case 0:
                            if (tIconWrap != null) ImGui.Image(tIconWrap.ImGuiHandle, Utils.Utils.ResizeToIcon(this.mPlugin, tIconWrap!));
                            break;
                        case 1:
                            UtilsGUI.SelectableLink_WithPopup(mPlugin, tMob.mName, tMob.GetGenId());
                            break;
                        case 2:
                            ImGui.Text($"{tMob.mLevel}");
                            break;
                        case 3:
                            UtilsGUI.LocationLinkButton(this.mPlugin, tMob.mLocation!);
                            break;
                        default: break;
                    }
                }

                ImGui.PopStyleVar();
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
