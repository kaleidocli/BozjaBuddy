using ImGuiNET;
using ImGuiScene;
using BozjaBuddy.Data;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using System.Text.Json;
using Dalamud.Logging;
using BozjaBuddy.Utils;
using Dalamud.Interface.Style;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.GUI.Sections
{
    internal class AuxiliaryViewerSection : Section
    {
        public static Dictionary<int, bool> mTabGenIds = new Dictionary<int, bool>();
        public static List<int> mTabGenIdsToDraw = new List<int>();
        public static LoadoutJson? mTenpLoadout = null;
        public static bool mIsRefreshRequired = false;
        public static TextureCollection? mTextureCollection = null;
        public static int _mGenIdToTabFocus = -1;
        private static ImGuiTabBarFlags AUXILIARY_TAB_FLAGS = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton;
        public static GUIFilter mGUIFilter = new GUIFilter();
        unsafe static ImGuiTextFilterPtr mFilter = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

        private LostActionTableSection mLostActionTableSection;

        protected override Plugin mPlugin { get; set; }

        public AuxiliaryViewerSection(Plugin tPLugin)
        {
            this.mPlugin = tPLugin;
            this.mLostActionTableSection = new(this.mPlugin);
        }

        public override bool DrawGUI()
        {
            // Legacy Auxi viewer
            if (!this.mPlugin.Configuration.mIsAuxiUsingNGV)
            {
                if (mTabGenIds.Keys.Count == 0) return false;

                if (ImGui.BeginTabBar("Aux", AuxiliaryViewerSection.AUXILIARY_TAB_FLAGS))
                {
                    int tCurr = 0;
                    while (true)        // Side-stepping "Collectioned was modified" issue
                    {
                        if (tCurr >= AuxiliaryViewerSection.mTabGenIdsToDraw.Count) break;
                        this.DrawTab(this.mPlugin.mBBDataManager.mGeneralObjects[AuxiliaryViewerSection.mTabGenIdsToDraw[tCurr]]);
                        tCurr++;
                    }
                    ImGui.EndTabBar();
                }
                return true;
            }

            // Getting extra inputs
            HashSet<ImGuiKey> tExtraKbInputs = new();
            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
            {
                if (this.mPlugin.isKeyPressed(Dalamud.Game.ClientState.Keys.VirtualKey.DELETE))
                {
                    tExtraKbInputs.Add(ImGuiKey.Delete);
                }
                if (this.mPlugin.isKeyPressed(Dalamud.Game.ClientState.Keys.VirtualKey.C))
                {
                    tExtraKbInputs.Add(ImGuiKey.C);
                }
                if (this.mPlugin.isKeyPressed(Dalamud.Game.ClientState.Keys.VirtualKey.V))
                {
                    tExtraKbInputs.Add(ImGuiKey.V);
                }
            }

            // Node graph viewer
            this.mPlugin.NodeGraphViewer_Auxi.Draw(pExtraKeyboardInputs: tExtraKbInputs);
            var tSaveData = this.mPlugin.NodeGraphViewer_Auxi.GetLatestSaveDataSinceLastChange();
            if (tSaveData != null)
            {
                this.mPlugin.Configuration.mAuxiNGVSaveData = tSaveData.Item2;
                this.mPlugin.Configuration.Save();
            }

            return true;
        }

        private void DrawTab(GeneralObject pObj)
        {
            bool[] tIsOpened = { true };
            
            if (pObj.mTabColor != null) ImGui.PushStyleColor(ImGuiCol.Tab, pObj.mTabColor.Value);

            if (ImGui.BeginTabItem($"{pObj.mName}##{pObj.mId}", ref tIsOpened[0], pObj.GetGenId() == AuxiliaryViewerSection._mGenIdToTabFocus
                                                                    ? ImGuiTabItemFlags.SetSelected
                                                                    : ImGuiTabItemFlags.None))
            {
                if (pObj.GetGenId() == AuxiliaryViewerSection._mGenIdToTabFocus) AuxiliaryViewerSection._mGenIdToTabFocus = -1;   // Release selected
                ImGui.BeginChild($"##{pObj.mId}", new Vector2(ImGui.GetWindowWidth(), this.mPlugin.Configuration.isAuxiVisible < 2
                                                                      ? 390
                                                                      : ImGui.GetWindowHeight() - ImGui.GetStyle().WindowPadding.Y
                                                                      ));
                if (pObj is Loadout)
                {
                    this.DrawTabHeaderLoadout(pObj);
                    if (AuxiliaryViewerSection.mTenpLoadout == null)
                        this.DrawTabContentLoadout(pObj);
                    else
                    {
                        this.DrawTabContentLoadout_Edit(pObj);
                    }
                }
                else
                {
                    this.DrawTabHeader(pObj);
                    this.DrawTabContent(pObj);
                }
                ImGui.Separator();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (pObj.mTabColor != null) ImGui.PopStyleColor();

            if (!tIsOpened[0])
            {
                AuxiliaryViewerSection.RemoveTab(this.mPlugin, pObj.GetGenId());
            }
        }

        public void DrawTabHeader(GeneralObject pObj)
        {
            // Icon
            IDalamudTextureWrap? tIconWrap;
            Vector2? tSize = null;
            switch (pObj.GetSalt())
            {
                case GeneralObject.GeneralObjectSalt.Fragment:
                    //PluginLog.LogDebug($"DrawTab(): Active iconIds in sheet {TextureCollection.Sheet.Item.ToString()}: {String.Join(", ", AuxiliaryViewerSection.mTextureCollection!.mIcons[Sheet.Item].Keys)}");
                    //PluginLog.LogDebug($"DrawTab(): Loading icon of itemId {pObj.mId} from sheet {TextureCollection.Sheet.Item.ToString()}");
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.Item);
                    break;
                case GeneralObject.GeneralObjectSalt.Fate:
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetStandardTexture((uint)this.mPlugin.mBBDataManager.mFates[pObj.mId].mType);
                    break;
                case GeneralObject.GeneralObjectSalt.Mob:
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetStandardTexture((uint)this.mPlugin.mBBDataManager.mMobs[pObj.mId].mType);
                    break;
                case GeneralObject.GeneralObjectSalt.FieldNote:
                    tIconWrap = UtilsGameData.kTexCol_FieldNote?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.FieldNote);
                    if (tIconWrap != null) tSize = new Vector2(tIconWrap.Width * 0.5f, tIconWrap.Height * 0.5f);
                    break;
                default:
                    tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId));
                    if (tIconWrap == null)      // try reloading the texture, which is sometimes lost due to LoadoutTab's disposing process
                    {
                        AuxiliaryViewerSection.mTextureCollection?.AddTextureFromItemId(Convert.ToUInt32(pObj.mId), GeneralObject.GeneralObjectSalt.LostAction);
                        tIconWrap = AuxiliaryViewerSection.mTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId));
                    }
                    break;
            }
            if (tIconWrap != null)
            {
                ImGui.Image(tIconWrap.ImGuiHandle, tSize ?? new System.Numerics.Vector2(tIconWrap.Width, tIconWrap.Height));
                ImGui.SameLine();           // Do not Sameline() if there's no image, since it'll Sameline() to the TabItem above
            }
            // Name and Details and Location
            ImGui.BeginGroup();
            UtilsGUI.SelectableLink_WithPopup(this.mPlugin, pObj.mName, pObj.GetGenId());
            ImGui.Text(pObj.mDetail);
            // Alarm button
            if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Fate)
            {
                ImGui.SameLine();
                AuxiliaryViewerSection.GUIAlignRight(1);
                UtilsGUI.ACPUFateCeButton(this.mPlugin, pObj.mId, pObj.mName);
            }
            if (pObj.mLocation != null)
            {
                ImGui.SameLine();
                UtilsGUI.LocationLinkButton(this.mPlugin, pObj.mLocation, true, pRightAlignOffset: 12f);
            }
            ImGui.EndGroup();
        }
        public void DrawTabContent(GeneralObject pObj)
        {
            if (ImGui.BeginTabBar(pObj.mName))
            {
                // Description
                if (ImGui.BeginTabItem("Description"))
                {
                    this.DrawTabContent_Description(pObj);
                    ImGui.EndTabItem();
                }
                // Sources
                if (ImGui.BeginTabItem("Sources/Drop"))
                {
                    this.DrawTabContent_Source(pObj);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
        public void DrawTabHeaderLoadout(GeneralObject pObj)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            Loadout tLoadout = this.mPlugin.mBBDataManager.mLoadouts[pObj.mId];
            // Instruction
            UtilsGUI.GreyText("[Shift+LMB/RMB] on action's icon to add/remove action from loadout");
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("To edit your Custom Loadout, press the Pen icon button on the right.\n=========== WHILE EDITING ===========\n- There is an Action table below to add/remove actions from loadout.\n- Similar to in-game loadout, [Shift+LMB/RMB] on action's icon to add/remove action from loadout.\n- The grey number on the right of action's name is its weight.");
            ImGui.SameLine();
            AuxiliaryViewerSection.GUIAlignRight((float)(22 * 5.1));
            // DELETE button
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash, UtilsGUI.AdjustTransparency(ImGuiColors.DalamudRed, 0.4f)) && io.KeyShift)
            {
                BBDataManager.DynamicRemoveGeneralObject<Loadout>(this.mPlugin, tLoadout, this.mPlugin.mBBDataManager.mLoadouts);
                AuxiliaryViewerSection.mIsRefreshRequired = true;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + LMB] to delete the current entry"); }
            ImGui.SameLine(); ImGui.Spacing(); ImGui.SameLine(); ImGui.Spacing(); ImGui.SameLine();
            // COPY button
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Upload))
            {
                ImGui.SetClipboardText(JsonSerializer.Serialize(new LoadoutJson(tLoadout)));
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Copy the current entry to clipboard"); }
            ImGui.SameLine();
            // EDIT button
            if (AuxiliaryViewerSection.mTenpLoadout == null && ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.PencilAlt))
            {
                AuxiliaryViewerSection.mTenpLoadout = new LoadoutJson(tLoadout);
                AuxiliaryViewerSection.mTenpLoadout.RecalculateWeight(this.mPlugin);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.Colors.Button_Red);
                if (AuxiliaryViewerSection.mTenpLoadout != null && ImGui.Button("  X "))
                {
                    AuxiliaryViewerSection.mTenpLoadout = null;
                }
                ImGui.PopStyleColor();
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Edit / Discard edit"); }
            ImGui.SameLine();
            // SAVE button
            if (AuxiliaryViewerSection.mTenpLoadout == null)
            {
                ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Save changes"); }
            }
            else if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save, UtilsGUI.Colors.Button_Green) && AuxiliaryViewerSection.mTenpLoadout != null)
            {
                // Save to cache
                Loadout tLoadoutNew = new Loadout(this.mPlugin, AuxiliaryViewerSection.mTenpLoadout);
                BBDataManager.DynamicAddGeneralObject<Loadout>(this.mPlugin, tLoadoutNew, this.mPlugin.mBBDataManager.mLoadouts);
            }
            ImGui.Separator();
        }
        public void DrawTabContentLoadout(GeneralObject pObj)
        {
            Loadout tLoadout = this.mPlugin.mBBDataManager.mLoadouts[pObj.mId];
            // Action List
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
                ImGui.BeginChild("loadout_actionlist", 
                    new System.Numerics.Vector2(ImGui.GetWindowWidth() / 5 * 2 - ImGui.GetStyle().FramePadding.X, ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetStyle().FramePadding.Y),
                    true,
                    ImGuiWindowFlags.MenuBar);
                if (ImGui.BeginMenuBar())
                {
                    ImGui.TextColored(tLoadout.mWeight > 99 ? UtilsGUI.Colors.NormalText_Red : UtilsGUI.Colors.BackgroundText_Grey, $"WEIGHT: {tLoadout.mWeight} / 99");
                    ImGui.EndMenuBar();
                }
                foreach (int iActionId in tLoadout.mActionIds.Keys)
                {
                    // icon
                    AuxiliaryViewerSection.mTextureCollection!.AddTextureFromItemId(Convert.ToUInt32(iActionId));
                    IDalamudTextureWrap? tIconWrap = AuxiliaryViewerSection.mTextureCollection.GetTextureFromItemId(Convert.ToUInt32(iActionId));
                    if (tIconWrap != null)
                    {
                        UtilsGUI.SelectableLink_Image(
                                this.mPlugin,
                                this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                                tIconWrap,
                                pIsShowingCacheAmount: true,
                                pImageScaling: 0.6f
                            );
                    }
                    // link
                    ImGui.SameLine();
                    UtilsGUI.SelectableLink_WithPopup(
                        this.mPlugin,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].mName,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId()
                        );
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUIAlignRight(-15);
                    ImGui.Text($"{tLoadout.mActionIds[iActionId]}");
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            // Description
            ImGui.SameLine();
            {
                ImGui.BeginChild("loadout_description", new System.Numerics.Vector2(ImGui.GetWindowWidth() / 5 * 3 - ImGui.GetStyle().FramePadding.X, ImGui.GetWindowHeight() - ImGui.GetCursorPosY()));
                ImGui.TextUnformatted(tLoadout.mName);
                string tTemp = $"[{tLoadout.mGroup}] • [{tLoadout.mRole.ToString()}]";
                AuxiliaryViewerSection.GUIAlignRight(tTemp); ImGui.TextUnformatted(tTemp);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                this.DrawTabContent_Description(pObj);
                ImGui.EndChild();
            }
        }
        public void DrawTabContentLoadout_Edit(GeneralObject pObj)
        {
            LoadoutJson tLoadout = AuxiliaryViewerSection.mTenpLoadout!;
            // Action List
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
                ImGui.BeginChild("loadout_actionlist",
                    new System.Numerics.Vector2(ImGui.GetWindowWidth() / 5 * 2 - ImGui.GetStyle().FramePadding.X, 241),
                    true,
                    ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar);
                if (ImGui.BeginMenuBar())
                {
                    ImGui.TextColored(tLoadout.mWeight > 99 ? UtilsGUI.Colors.NormalText_Red : UtilsGUI.Colors.BackgroundText_Grey, $"WEIGHT: {tLoadout.mWeight} / 99");
                    ImGui.SameLine();
                    // Lost action grid popup
                    //ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + (ImGui.GetWindowWidth() / 5 * 2 + 10), 0));
                    AuxiliaryViewerSection.GUIAlignRight(-5);
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(UtilsGUI.Colors.Button_Green));
                    if (ImGui.Button(" + "))
                    {
                        ImGui.PopStyleColor();
                        ImGui.OpenPopup("##lagrid");
                    }
                    else ImGui.PopStyleColor();
                    if (ImGui.BeginPopup("##lagrid"))
                    {
                        this.mLostActionTableSection.DrawTable_GridOnly();
                        ImGui.EndPopup();
                    }
                    ImGui.EndMenuBar();
                }
                UtilsGUI.InputPayload tInputPayload = new();
                foreach (int iActionId in tLoadout.mActionIds.Keys)
                {
                    // icon
                    AuxiliaryViewerSection.mTextureCollection!.AddTextureFromItemId(Convert.ToUInt32(iActionId));
                    IDalamudTextureWrap? tIconWrap = AuxiliaryViewerSection.mTextureCollection.GetTextureFromItemId(Convert.ToUInt32(iActionId));
                    if (tIconWrap != null
                        && UtilsGUI.SelectableLink_Image(
                                this.mPlugin,
                                this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                                tIconWrap,
                                pIsAuxiLinked: !ImGui.GetIO().KeyShift,
                                pIsShowingCacheAmount: true,
                                pImageScaling: 0.6f,
                                pInputPayload: tInputPayload,
                                pAdditionalHoverText: $"[Shift+LMB/RMB] Add/remove from loadout\n"
                            )
                        && tInputPayload.mIsKeyShift)
                    {
                        AuxiliaryViewerSection.GUILoadoutEditAdjuster_Incre(this.mPlugin, iActionId);
                    }
                    else if (tIconWrap != null && tInputPayload.mIsHovered && tInputPayload.mIsMouseRmb && tInputPayload.mIsKeyShift)
                    {
                        AuxiliaryViewerSection.GUILoadoutEditAdjuster_Decre(this.mPlugin, iActionId);
                    }
                    // link
                    ImGui.SameLine();

                    UtilsGUI.SelectableLink_WithPopup(
                        this.mPlugin,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].mName,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                        true
                        );
                    ImGui.SameLine(); 
                    ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, $"+{this.mPlugin.mBBDataManager.mLostActions[iActionId].mWeight}");
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUIAlignRight(-15);
                    // adjuster
                    ImGui.Text(tLoadout.mActionIds[iActionId].ToString());
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            // Description
            ImGui.SameLine();
            {
                ImGui.BeginChild("loadout_description", new System.Numerics.Vector2(ImGui.GetWindowWidth() / 5 * 3 - ImGui.GetStyle().FramePadding.X, 241));
                // Name
                ImGui.InputText("##name", ref AuxiliaryViewerSection.mTenpLoadout!._mName, 120);
                // Group & Role
                ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
                ImGui.InputText("##group", ref AuxiliaryViewerSection.mTenpLoadout!._mGroup, 120);
                ImGui.PopItemWidth();
                ImGui.SameLine(); ImGui.Text(" • ");
                ImGui.SameLine(); AuxiliaryViewerSection.mGUIFilter.HeaderRoleIconButtons(AuxiliaryViewerSection.mTenpLoadout!._mRole, null);


                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Description
                ImGui.InputTextMultiline("##description",
                                        ref AuxiliaryViewerSection.mTenpLoadout!._mDescription,
                                        1024,
                                        new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X, ImGui.GetTextLineHeight() * 10));

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.EndChild();
            }

            // Action table
            this.mLostActionTableSection.DrawTable_GridOnly();
        }
        public void DrawTabContent_Description(GeneralObject pObj)
        {
            ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.None);
            ImGui.PushTextWrapPos(0);
            // Fate chains
            if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Fate
                && (this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFatePrev != -1
                    || this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFateNext != -1))
            {
                int iCurrFateId = this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFatePrev != -1
                    ? this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFatePrev
                    : this.mPlugin.mBBDataManager.mFates[pObj.mId].mChainFateNext;
                while (this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev != -1)        // Find the starting point of FATE chain
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFatePrev;
                ImGui.Text("Chain: ");
                do
                {
                    ImGui.SameLine();
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin,
                        this.mPlugin.mBBDataManager.mFates[iCurrFateId].mName,
                        this.mPlugin.mBBDataManager.mFates[iCurrFateId].GetGenId(),
                        true);
                    iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFateNext;
                }
                while (iCurrFateId != -1);
                ImGui.Separator();
            }
            if (pObj.mIGMarkup == null) 
                ImGui.TextUnformatted(pObj.mDescription);
            else
                pObj.mIGMarkup!.DrawGUI();
            ImGui.PopTextWrapPos();
            ImGui.EndChild();
        }
        public void DrawTabContent_Source(GeneralObject pObj)
        {
            ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.None);
            if (pObj.mLinkFragments.Count != 0 && ImGui.CollapsingHeader($"Fragment ({pObj.mLinkFragments.Count})"))
            {
                foreach (int iId in pObj.mLinkFragments)
                {
                    Fragment tFragment = this.mPlugin.mBBDataManager.mFragments[iId];
                    // LOCATION
                    if (tFragment.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFragment.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"Forgetten Fragment of {tFragment.mName}", tFragment.GetGenId());
                    // Display as vendor's stock
                    if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Vendor)
                    {
                        Vendor tVendor = this.mPlugin.mBBDataManager.mVendors[pObj.mId];
                        ImGui.SameLine();
                        ImGui.TextUnformatted($" ({tVendor.GetAmountPriceCurrency(iId).Item1}) {tVendor.GetAmountPriceCurrency(iId).Item2} {tVendor.GetAmountPriceCurrency(iId).Item3}");
                    }
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkFates.Count != 0 && ImGui.CollapsingHeader($"FATE ({pObj.mLinkFates.Count})"))
            {
                foreach (int iId in pObj.mLinkFates)
                {
                    BozjaBuddy.Data.Fate tFate = this.mPlugin.mBBDataManager.mFates[iId];
                    // LOCATION
                    if (tFate.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFate.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tFate.mName}", tFate.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkMobs.Count != 0 && ImGui.CollapsingHeader($"Mob ({pObj.mLinkMobs.Count})"))
            {
                foreach (int iId in pObj.mLinkMobs)
                {
                    Mob tMob = this.mPlugin.mBBDataManager.mMobs[iId];
                    // LOCATION
                    if (tMob.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tMob.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tMob.mName}", tMob.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkActions.Count != 0 && ImGui.CollapsingHeader($"Action ({pObj.mLinkActions.Count})"))
            {
                foreach (int iId in pObj.mLinkActions)
                {
                    LostAction tAction = this.mPlugin.mBBDataManager.mLostActions[iId];
                    // LOCATION
                    if (tAction.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tAction.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tAction.mName}", tAction.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkVendors.Count != 0 && ImGui.CollapsingHeader($"Vendor ({pObj.mLinkActions.Count})"))
            {
                foreach (int iId in pObj.mLinkVendors)
                {
                    Vendor tVendor = this.mPlugin.mBBDataManager.mVendors[iId];
                    // LOCATION
                    if (tVendor.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tVendor.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tVendor.mName}\t({tVendor.GetAmountPriceCurrency(pObj.mId).Item1}) {tVendor.GetAmountPriceCurrency(pObj.mId).Item2} {tVendor.GetAmountPriceCurrency(pObj.mId).Item3.ToString()}", tVendor.GetGenId());
                    ImGui.Separator();
                }
            }
            if (pObj.mLinkFieldNotes.Count != 0 && ImGui.CollapsingHeader($"Field Notes ({pObj.mLinkFieldNotes.Count})"))
            {
                foreach (int iId in pObj.mLinkFieldNotes)
                {
                    FieldNote tFieldNote = this.mPlugin.mBBDataManager.mFieldNotes[iId];
                    // LOCATION
                    if (tFieldNote.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFieldNote.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tFieldNote.mName}", tFieldNote.GetGenId());
                    ImGui.Separator();
                }
            }
            ImGui.EndChild();
        }

        /// <summary>
        /// For first-time binding a GeneralObject to an Auxiliary Tab. Do nothing if the object has already been bound.
        /// </summary>
        /// <param name="tPlugin"></param>
        /// <param name="pGenId"></param>
        public static void BindToGenObj(Plugin pPlugin, int pGenId)
        {
            if (AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            AuxiliaryViewerSection.mTabGenIds.Add(pGenId, false);
        }
        public static void UnbindFromGenObj(Plugin pPlugin, int pGenId)
        {
            AuxiliaryViewerSection.mTabGenIds.Remove(pGenId);
        }
        public static void AddTab(Plugin pPlugin, int pGenId)
        {
            if (!AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            GeneralObject.GeneralObjectSalt tSalt = pPlugin.mBBDataManager.mGeneralObjects[pGenId].GetSalt();
            AuxiliaryViewerSection.mTabGenIds[pGenId] = true;
            AuxiliaryViewerSection.mTabGenIdsToDraw.Add(pGenId);
            AuxiliaryViewerSection._mGenIdToTabFocus = pGenId;
            AuxiliaryViewerSection.mTextureCollection ??= new TextureCollection(pPlugin);
            AuxiliaryViewerSection.mTextureCollection.AddTextureFromItemId(Convert.ToUInt32(tId), tSalt);
        }
        public static void RemoveTab(Plugin pPlugin, int pGenId)
        {
            if (!AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            GeneralObject.GeneralObjectSalt tSalt = pPlugin.mBBDataManager.mGeneralObjects[pGenId].GetSalt();
            AuxiliaryViewerSection.mTabGenIds[pGenId] = false;
            AuxiliaryViewerSection.mTabGenIdsToDraw.Remove(pGenId);
            AuxiliaryViewerSection.mTextureCollection?.RemoveTextureFromItemId(Convert.ToUInt32(tId), tSalt);
            if (tSalt == GeneralObject.GeneralObjectSalt.Loadout)
            {
                AuxiliaryViewerSection.mTextureCollection?.RemoveTextureFromItemId(
                    pPlugin.mBBDataManager.mLoadouts[tId].mActionIds.Keys, 
                    GeneralObject.GeneralObjectSalt.LostAction
                    );
            }
        }

        public static void GUIAlignRight(float pTargetItemWidth)
        {
            ImGuiStylePtr tStyle = ImGui.GetStyle();
            float tPadding = tStyle.WindowPadding.X + tStyle.FramePadding.X * 2 + tStyle.ScrollbarSize;
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - pTargetItemWidth - tPadding);
        }
        public static void GUILoadoutEditAdjuster_Incre(Plugin pPlugin, int pActionId)
        {
            if (AuxiliaryViewerSection.mTenpLoadout == null) return;
            if (AuxiliaryViewerSection.mTenpLoadout!.mActionIds.ContainsKey(pActionId))
                AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] += 1;
            else
                AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] = 1;
            AuxiliaryViewerSection.mTenpLoadout!.mWeight += pPlugin.mBBDataManager.mLostActions[pActionId].mWeight;
        }
        public static void GUILoadoutEditAdjuster_Decre(Plugin pPlugin, int pActionId)
        {
            if (AuxiliaryViewerSection.mTenpLoadout == null) return;
            if (AuxiliaryViewerSection.mTenpLoadout!.mActionIds.ContainsKey(pActionId))
            {
                if (AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] == 1)
                    AuxiliaryViewerSection.mTenpLoadout!.mActionIds.Remove(pActionId);
                else
                    AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] -= 1;
                AuxiliaryViewerSection.mTenpLoadout!.mWeight -= pPlugin.mBBDataManager.mLostActions[pActionId].mWeight;
            }
        }
        public static void GUILoadoutEditAdjuster(Plugin pPlugin, int pActionId)
        {
            if (AuxiliaryViewerSection.mTenpLoadout == null) return;
            ImGui.PushID(pActionId);
            bool tIsInLoadout = AuxiliaryViewerSection.mTenpLoadout!.mActionIds.ContainsKey(pActionId);
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.LongArrowAltUp))
            {
                AuxiliaryViewerSection.GUILoadoutEditAdjuster_Incre(pPlugin, pActionId);
            }
            ImGui.SameLine();
            ImGui.Text(tIsInLoadout
                       ? $"{AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId]}"
                       : "-");
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.LongArrowAltDown)
                && tIsInLoadout)
            {
                AuxiliaryViewerSection.GUILoadoutEditAdjuster_Decre(pPlugin, pActionId);
            }
            ImGui.PopID();
        }
        public static void GUITextFilterAction(Plugin pPlugin)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
            ImGui.BeginChild("loadout_description_actionfilter");
            unsafe
            {
                ImGui.BeginChild("filter", new Vector2(ImGui.GetContentRegionAvail().X / 3, ImGui.GetContentRegionAvail().Y));
                AuxiliaryViewerSection.mFilter.Draw("", ImGui.GetContentRegionAvail().X);
                ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, "Action filter");
                ImGui.EndChild();
                ImGui.SameLine();
                ImGui.BeginChild("filterResult", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
                foreach (LostAction iAction in pPlugin.mBBDataManager.mLostActions.Values)
                {
                    if (AuxiliaryViewerSection.mFilter.PassFilter(iAction.mName))
                    {
                        if (AuxiliaryViewerSection.mTenpLoadout != null)
                        {
                            AuxiliaryViewerSection.GUILoadoutEditAdjuster(pPlugin, iAction.mId);
                            ImGui.SameLine();
                        }
                        UtilsGUI.SelectableLink_WithPopup(pPlugin, iAction.mName, iAction.GetGenId());
                        ImGui.SameLine();
                        ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, $"[{iAction.mWeight}]");
                    }
                }
                ImGui.EndChild();

                ImGui.EndChild();
                ImGui.PopStyleVar();
            }
        }
        public static void GUIAlignRight(string pText)
        {
            AuxiliaryViewerSection.GUIAlignRight(ImGui.CalcTextSize(pText).X);
        }

        public override void DrawGUIDebug()
        {
            string tRes = "";
            foreach (int i in AuxiliaryViewerSection.mTabGenIdsToDraw) tRes += i.ToString();
            ImGui.Text($"Aux: {tRes}");
        }
        public override void Dispose()
        {
            unsafe 
            {
                ImGuiNative.ImGuiTextFilter_destroy(AuxiliaryViewerSection.mFilter.NativePtr);
            }
        }
    }
}
