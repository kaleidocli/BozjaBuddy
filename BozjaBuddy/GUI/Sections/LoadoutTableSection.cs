using BozjaBuddy.Data;
using Dalamud.Interface.Components;
using ImGuiNET;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using BozjaBuddy.Utils;
using System.Numerics;
using System.Runtime.CompilerServices;
using BozjaBuddy.Windows;
using System.Security.Cryptography;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.GUI.Sections
{
    internal class LoadoutTableSection : Section
    {
        static int COLUMN_COUNT;
        static int HEADER_TEXT_FIELD_SIZE_OFFSET = GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET;
        private static int kField_CurrJob = -1;
        const ImGuiTableFlags TABLE_FLAG = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
                                     ImGuiTableFlags.ContextMenuInBody | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg |
                                     ImGuiTableFlags.ScrollY | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable |
                                     ImGuiTableFlags.ScrollX;
        private float TABLE_SIZE_Y;
        float FIXED_LINE_HEIGHT;
        private List<int> mLoadoutIds;
        private Filter.Filter[] mFilters;
        unsafe private Dictionary<string, ImGuiTextFilterPtr> mTextFilters = new();
        private Dictionary<string, int> mTextFiltersCurrVal = new();

        protected override Plugin mPlugin { get; set; }



        public LoadoutTableSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;

            this.TABLE_SIZE_Y = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 2.02f);
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
                DrawTableContent(tIDs, this.mPlugin.Configuration.mIsShowingRecLoadout);

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
                            UtilsGUI.DrawRoleFlagAsIconString(this.mPlugin, tLoadout.mRole);
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
            bool tIsCompact = ImGui.GetWindowWidth() < this.mPlugin.Configuration.SizeConstraints.MinimumSize.X + 210;
            // Overlay
            LoadoutTableSection.DrawOverlayBar(this.mPlugin, this.mTextFilters, this.mTextFiltersCurrVal, pIsCompactMode: tIsCompact);
            ImGui.SameLine();
            AuxiliaryViewerSection.GUIAlignRight((float)((tIsCompact ? 22 : 36) * ImGui.CalcTextSize("A").X));
            // Toggle rec visibility
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, tIsCompact ? "Rec." : "Recommended loadouts");
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("Recommended loadouts by many AdvForays communities.\n\n- To restore these loadouts, press the Spinning Arrow button.\n- Recommended loadouts only serve as a suggestion and may be outdated. As such, please abide by the rules of your community if you are attending their runs!\n- Any suggestions regarding this are preferred being posted in XIVLauncher's Discord > Plugins > #plugin-help-forum > Bozja Buddy.");
            ImGui.SameLine();
            ImGuiComponents.ToggleButton("recToggle", ref this.mPlugin.Configuration.mIsShowingRecLoadout);
            UtilsGUI.SetTooltipForLastItem("Hide recommended loadouts in Loadout tables, in loadout drop-down, and in Search-all results.");
            // Restore preset
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ArrowsSpin) && io.KeyShift)
            {
                this.mPlugin.mBBDataManager.ReloadLoadoutsPreset();
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + LMB] to return recommended loadouts to default. (user's loadouts are intact)"); }
            ImGui.SameLine();
            ImGui.Text(" | ");
            ImGui.SameLine();
            // Import
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Download) && io.KeyShift)
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
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + LMB] to import an entry from clipboard"); }
            // Add
            ImGui.SameLine();
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
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + LMB] to add a new entry"); }
        }

        /// <summary>
        /// No need to put any value into pGuiVar_TextFilters or pGuiVar_TextFiltersCurrVal. 
        /// </summary>
        public static void DrawOverlayBar(
            Plugin pPlugin, 
            Dictionary<string, ImGuiTextFilterPtr> pGuiVar_TextFilters, 
            Dictionary<string, int> pGuiVar_TextFiltersCurrVal,
            float? pOverlayComboFixedWidth = null,
            bool pIsCompactMode = false)
        {
            string tGuiKey = "aljob";
            bool tIsJobChanged = false;
            Job? tCurrJob = UtilsGameData.GetUserJob(pPlugin);
            ref Configuration.GuiAssistConfig tGaConfig = ref pPlugin.Configuration.mGuiAssistConfig;
            ref bool tFlag = ref tGaConfig.overlay.isUsingJobSpecific;

            // Init CurrVal if needed. Track current job
            if (!pGuiVar_TextFiltersCurrVal.ContainsKey("currJob"))
            {
                pGuiVar_TextFiltersCurrVal.Add("currJob", (int)(tCurrJob ?? Job.PLD));
            }
            else if (tCurrJob.HasValue && (int)tCurrJob!.Value != pGuiVar_TextFiltersCurrVal["currJob"])
            {
                tIsJobChanged = true;
                pGuiVar_TextFiltersCurrVal["currJob"] = (int)(tCurrJob ?? Job.PLD);
            }
            if (tFlag && tIsJobChanged)      // Switch to currrent job if job was changed
            {
                if (!pGuiVar_TextFiltersCurrVal.ContainsKey(tGuiKey))
                {
                    pGuiVar_TextFiltersCurrVal.Add(tGuiKey, (int)(tCurrJob ?? Job.PLD));
                }
                else
                    pGuiVar_TextFiltersCurrVal[tGuiKey] = (int)(tCurrJob ?? Job.PLD);
                LoadoutTableSection.kField_CurrJob = pGuiVar_TextFiltersCurrVal[tGuiKey];
            }
            if (!pGuiVar_TextFiltersCurrVal.ContainsKey("slotIndex"))
            {
                pGuiVar_TextFiltersCurrVal.Add("slotIndex", tGaConfig.overlay.currentSlotIndex);
            }
            if (!pGuiVar_TextFiltersCurrVal.ContainsKey(tGuiKey))       // currJob. If not init, set to current job.
            {
                pGuiVar_TextFiltersCurrVal.Add(tGuiKey, tFlag ? (int)(tCurrJob ?? Job.PLD) : (int)Job.ALL);
            }

            // GUI
            // Job combo (not saving to config)
            ImGui.PushItemWidth(35);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
            IDalamudTextureWrap? tCurrJobIcon = UtilsGameData.GetJobIcon((Job)pGuiVar_TextFiltersCurrVal[tGuiKey]);
            // Button job type
            if (!tFlag
                ? ImGui.Button("  ANY JOB  ")
                : ImGui.Button(" CURRENT")
                )
            {
                // Set to Any
                if (tFlag)
                {
                    tFlag = false;
                    pPlugin.Configuration.Save();
                    pGuiVar_TextFiltersCurrVal[tGuiKey] = (int)Job.ALL;
                    LoadoutTableSection.kField_CurrJob = pGuiVar_TextFiltersCurrVal[tGuiKey];
                }
                // Set to job-spec
                else
                {
                    pGuiVar_TextFiltersCurrVal[tGuiKey] = (int)(tCurrJob ?? Job.PLD);
                    LoadoutTableSection.kField_CurrJob = pGuiVar_TextFiltersCurrVal[tGuiKey];
                    tFlag = true;
                }
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("The Lost Find Cache will be filtered by the loadout on the right. \n" + (tFlag
                                                                                                                        ? "(Mode: CURRENT --> The loadout changes when job changes)"
                                                                                                                        : "(Mode: ANY     --> The loadout applies to all jobs)"));
            }
            // Sync up the value (FIXME: do this in a more proper way)
            if (LoadoutTableSection.kField_CurrJob != -1
                && pGuiVar_TextFiltersCurrVal[tGuiKey] != LoadoutTableSection.kField_CurrJob)
            {
                pGuiVar_TextFiltersCurrVal[tGuiKey] = LoadoutTableSection.kField_CurrJob;
            }
            // Job icon, if applicable
            if (tFlag)
            {
                ImGui.SameLine();
                if (tCurrJobIcon != null)
                    ImGui.Image(tCurrJobIcon!.ImGuiHandle, Utils.Utils.ResizeToIcon(pPlugin, tCurrJobIcon!) + new Vector2(3.7f));
                else
                    ImGui.Text(((Job)pGuiVar_TextFiltersCurrVal[tGuiKey]).ToString());
            }

            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopItemWidth();
            ImGui.SameLine();
            // Overlay
            ImGui.PushItemWidth(pOverlayComboFixedWidth ?? (ImGui.GetWindowWidth() / 2 - ImGui.GetCursorPosX() - 80));
            LoadoutTableSection.DrawOverlayCombo(
                pPlugin, 
                pGuiVar_TextFilters, 
                tFlag ? tCurrJob ?? Job.PLD : Job.ALL, 
                "aljob2", 
                pOverlaySlot: pGuiVar_TextFiltersCurrVal["slotIndex"]);
            ImGui.PopItemWidth();
            // Slot index
            ImGui.SameLine();
            if (ImGui.Button(pGuiVar_TextFiltersCurrVal["slotIndex"] == 0 ? "  I   " : "  II  "))
            {
                pGuiVar_TextFiltersCurrVal["slotIndex"] = pGuiVar_TextFiltersCurrVal["slotIndex"] == 0 ? 1 : 0;
                tGaConfig.overlay.currentSlotIndex = pGuiVar_TextFiltersCurrVal["slotIndex"];
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("Switch between Loadout I and II of the current job.");
            }
            // Popup: Other overlay settings
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ListUl))
            {
                ImGui.OpenPopup("aljob_pu");
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("More loadout pairing");
            }
            if (ImGui.BeginPopup("aljob_pu"))
            {
                ConfigWindow.Draw_LoadoutPairingButton(pPlugin, pGuiVar_TextFilters);
                ImGui.EndPopup();
            }
            // Button: Auto-pair
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.WandSparkles) && ImGui.GetIO().KeyShift)
            {
                var tPairing = UtilsGameData.GetRecPairingForCurrJob(pPlugin);
                var tJob = pPlugin.Configuration.mGuiAssistConfig.overlay.isUsingJobSpecific ? UtilsGameData.GetUserJob(pPlugin) : Job.ALL;
                if (tJob.HasValue && tPairing.HasValue
                    && pPlugin.mBBDataManager.mLoadouts.ContainsKey(tPairing.Value.Item1)
                    && pPlugin.mBBDataManager.mLoadouts.ContainsKey(tPairing.Value.Item2))
                {
                    pPlugin.Configuration.SetOverlay(tJob.Value, tPairing.Value.Item1, pSlotIndex: 0);
                    pPlugin.Configuration.SetOverlay(tJob.Value, tPairing.Value.Item2, pSlotIndex: 1);
                }
            }
            else
            {
                UtilsGUI.SetTooltipForLastItem("[Shift + LMB] to auto pair with a best suited Custom Loadout from the recommended loadouts (if those are available).\n\nIf you are in Bozja or Zadnor, it'll based on your current job and Bozja/Zadnor. Otherwise, it will based on your current job and Delubrum Reginae + Savage ver.");
            }

            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("Active Loadout - a Custom Loadout that is paired with a specific job.\n\n- [CURRENT JOB]: Use Active Loadout of player's current job.\n- [ANY]: Use Active Loadout that is applicable for all jobs. Think of [ANY] as a seperate job.\n- Each job has two slots to pair with - Loadout I & II\n\n- Active Loadouts are used to visually filter in-game Lost Finds Cache window.");
            ImGui.SameLine();
            ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, pIsCompactMode ? "A. Loadout" : "Active Loadout");
            ImGui.SameLine();
        }
        public static void DrawOverlayCombo(Plugin pPlugin, Dictionary<string, ImGuiTextFilterPtr> pGuiVar_TextFilters, Job pJob, string pGuiKey, int pOverlaySlot = 0)
        {
            int? tLoadoutId = LoadoutTableSection.DrawLoadoutCombo(pPlugin, pGuiVar_TextFilters, pGuiKey + pOverlaySlot, pSelectedLoadoutId: pPlugin.Configuration.GetOverlay(pJob, pSlotIndex: pOverlaySlot));
            if (tLoadoutId.HasValue)
            {
                pPlugin.Configuration.SetOverlay(pJob, tLoadoutId == -1 ? null : tLoadoutId, pSlotIndex: pOverlaySlot);
            }
        }
        public static int? DrawLoadoutCombo(Plugin pPlugin, Dictionary<string, ImGuiTextFilterPtr> pGuiVar_TextFilters, string pGuiKey, int? pSelectedLoadoutId = null)
        {
            int? tRes = null;
            unsafe
            {
                var tLoadouts = pPlugin.mBBDataManager.mLoadouts;
                if (!pGuiVar_TextFilters.ContainsKey(pGuiKey))
                {
                    pGuiVar_TextFilters.Add(pGuiKey, new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null)));
                }
                if (ImGui.BeginCombo($"##{pGuiKey}", pSelectedLoadoutId == null ? "-----------" : tLoadouts[pSelectedLoadoutId!.Value].mName))
                {
                    pGuiVar_TextFilters[pGuiKey].Draw("");
                    // null option
                    if (pGuiVar_TextFilters[pGuiKey].PassFilter("-----------")
                            && ImGui.Selectable("-----------"))
                    {
                        tRes = -1;
                    }
                    foreach (Loadout iLoadout in tLoadouts.Values)
                    {
                        if (!pPlugin.Configuration.mIsShowingRecLoadout && iLoadout.mId > 9999) continue;
                        if (pGuiVar_TextFilters[pGuiKey].PassFilter(iLoadout.mName)
                            && ImGui.Selectable($"{iLoadout.mName}"))
                        {
                            tRes = iLoadout.mId;
                        }
                    }
                    ImGui.EndCombo();
                }
                else
                {
                    UtilsGUI.SetTooltipForLastItem(pSelectedLoadoutId == null ? "-----------" : tLoadouts[pSelectedLoadoutId!.Value].mName);
                }
            }
            return tRes;
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
            unsafe
            {
                foreach (ImGuiTextFilterPtr iPtr in this.mTextFilters.Values)
                {
                    iPtr.Destroy();
                }
            }
        }
    }
}
