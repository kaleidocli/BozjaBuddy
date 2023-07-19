using BozjaBuddy.Data;
using BozjaBuddy.Utils;
using BozjaBuddy.Windows;
using Dalamud.Interface.Components;
using Dalamud.Interface;
using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using static BozjaBuddy.Utils.UtilsGUI;

namespace BozjaBuddy.GUI.Sections
{
    internal class QuestTableSection : Section
    {
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        const ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        protected static ImGuiTableFlags GRID_FLAG = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter |
                             ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        private float TABLE_HEADER_HEIGHT = 45;
        private List<int> mQuestDs;
        private Filter.Filter[] mFilters;
        private TextureCollection mTextureCollection;

        protected override Plugin mPlugin { get; set; }
        private float FIXED_LINE_HEIGHT
        {
            get
            {
                return ImGui.GetTextLineHeight();
            }
            set { this.FIXED_LINE_HEIGHT = value; }
        }


        public QuestTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;

            //this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * 15;
            this.CalcTableHeight();

            this.mFilters = new Filter.Filter[]{
                new Filter.QuestTableSection.FilterType(),
                new Filter.QuestTableSection.FilterName(),
                new Filter.QuestTableSection.FilterNpc(),
                new Filter.QuestTableSection.FilterQuestChains(true, this.mPlugin),
                new Filter.QuestTableSection.FilterPrevQuests(true, this.mPlugin),
                new Filter.QuestTableSection.FilterNextQuests(true, this.mPlugin)
            };
            QuestTableSection.COLUMN_COUNT = this.mFilters.Length;

            this.mQuestDs = this.mPlugin.mBBDataManager.mQuests.Keys.ToList();

            if (UtilsGameData.kTextureCollection != null)
            {
                this.mTextureCollection = UtilsGameData.kTextureCollection;
            }
            else
            {
                this.mTextureCollection = new TextureCollection(this.mPlugin);
                this.mTextureCollection.AddTextureFromItemId(this.mQuestDs);
            }
        }

        private bool CheckFilter(Quest pEntity)
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
                    "##Quest",
                    QuestTableSection.COLUMN_COUNT,
                    QuestTableSection.TABLE_FLAG,
                    new Vector2(0.0f, this.TABLE_SIZE_Y))
                )
            {
                DrawTableHeader();
                List<int> tIDs = SortTableContent(this.mQuestDs, this.mFilters);
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
            for (int iCol = 0; iCol < QuestTableSection.COLUMN_COUNT; iCol++)
            {
                ImGui.TableSetColumnIndex(iCol);
                ImGui.PushID(ImGui.TableGetColumnName(iCol));
                ImGui.PushItemWidth(ImGui.GetColumnWidth(iCol) - QuestTableSection.HEADER_TEXT_FIELD_SIZE_OFFSET);
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
                Quest tQuest = this.mPlugin.mBBDataManager.mQuests[iID];
                if (!this.CheckFilter(tQuest)) continue;

                ImGui.TableNextRow(ImGuiTableRowFlags.None, this.FIXED_LINE_HEIGHT);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().CellPadding.Y));
                TextureWrap? tIconWrap = this.mTextureCollection.GetStandardTexture(
                            tQuest.mType switch
                            {
                                Quest.QuestType.Msq => TextureCollection.StandardIcon.QuestMSQ,
                                Quest.QuestType.Side => TextureCollection.StandardIcon.QuestSide,
                                Quest.QuestType.Key => TextureCollection.StandardIcon.QuestKey,
                                Quest.QuestType.Repeatable => TextureCollection.StandardIcon.QuestRepeatable,
                                _ => TextureCollection.StandardIcon.QuestSide
                            }
                    );
                for (int i = 0; i < QuestTableSection.COLUMN_COUNT; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    switch (i)
                    {
                        case 0:
                            if (tIconWrap != null)
                            {
                                UtilsGUI.SelectableLink_Image(
                                        this.mPlugin,
                                        tQuest.GetGenId(),
                                        tIconWrap,
                                        pIsLink: true,
                                        pIsAuxiLinked: !ImGui.GetIO().KeyShift,
                                        pImageScaling: Utils.Utils.GetIconResizeRatio(this.mPlugin, tIconWrap.Height)
                                        );
                            }
                            break;
                        case 1:
                            UtilsGUI.SelectableLink_WithPopup(mPlugin, tQuest.mName, tQuest.GetGenId());
                            break;
                        case 2:
                            if (tQuest.mIssuerLocation != null)
                            {
                                UtilsGUI.LocationLinkButton(this.mPlugin, tQuest.mIssuerLocation, pDesc: tQuest.mIssuerName);
                            }
                            break;
                        case 3:
                            foreach (int iiID in tQuest.mQuestChains)
                            {
                                QuestChain qChain = this.mPlugin.mBBDataManager.mQuestChains[iiID];
                                ImGui.PushID($"{iID}{iiID}");
                                UtilsGUI.SelectableLink_QuestChain(this.mPlugin, qChain.mName, qChain);
                                ImGui.PopID();
                            }
                            break;
                        case 4:
                            foreach (int iiID in tQuest.mPrevQuestIds)
                            {
                                if (!this.mPlugin.mBBDataManager.mQuests.TryGetValue(iiID, out var q) || q == null) continue;
                                ImGui.PushID($"{iID}{iiID}");
                                UtilsGUI.SelectableLink_WithPopup(mPlugin, q.mName, q.GetGenId());
                                ImGui.PopID();
                            }
                            break;
                        case 5:
                            foreach (int iiID in tQuest.mNextQuestIds)
                            {
                                if (!this.mPlugin.mBBDataManager.mQuests.TryGetValue(iiID, out var q) || q == null) continue;
                                ImGui.PushID($"{iID}{iiID}");
                                UtilsGUI.SelectableLink_WithPopup(mPlugin, q.mName, q.GetGenId());
                                ImGui.PopID();
                            }
                            break;
                        default: break;
                    }
                }
                ImGui.PopStyleVar();
            }
        }
        private void DrawOptionBar()
        {
            UtilsGUI.TextDescriptionForWidget("Only covers ShB and later. Quest types may be inaccurate. Not sure if this tab would do any help, but might as well.");
        }
        private void CalcTableHeight() => this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 2.3f);
        public override void DrawGUIDebug()
        {
            ImGui.Text(String.Format("Edited: {0} {1}", this.mFilters[0].GetCurrValue(),
                                                        this.mFilters[1].GetCurrValue()));
        }

        public override void Dispose()
        {
        }
    }
}
