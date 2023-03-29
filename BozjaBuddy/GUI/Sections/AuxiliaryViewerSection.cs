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

namespace BozjaBuddy.GUI.Sections
{
    internal class AuxiliaryViewerSection : Section
    {
        public static Dictionary<int, bool> mTabGenIds = new Dictionary<int, bool>();
        public static List<int> mTabGenIdsToDraw = new List<int>();
        public static LoadoutJson? mTenpLoadout = null;
        public static bool mIsRefreshRequired = false;
        private static TextureCollection? mTextureCollection = null;
        private static int mGenIdToTabFocus = -1;
        private static ImGuiTabBarFlags AUXILIARY_TAB_FLAGS = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton;
        private static GUIFilter mGUIFilter = new GUIFilter();
        unsafe static ImGuiTextFilterPtr mFilter = new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

        protected override Plugin mPlugin { get; set; }

        public AuxiliaryViewerSection(Plugin tPLugin)
        {
            this.mPlugin = tPLugin;
        }

        public override bool DrawGUI()
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

        private void DrawTab(GeneralObject pObj)
        {
            bool[] tIsOpened = { true };
            
            if (pObj.mTabColor != null) ImGui.PushStyleColor(ImGuiCol.Tab, pObj.mTabColor.Value);

            if (ImGui.BeginTabItem($"{pObj.mName}##{pObj.mId}", ref tIsOpened[0], pObj.GetGenId() == AuxiliaryViewerSection.mGenIdToTabFocus
                                                                    ? ImGuiTabItemFlags.SetSelected
                                                                    : ImGuiTabItemFlags.None))
            {
                if (pObj.GetGenId() == AuxiliaryViewerSection.mGenIdToTabFocus) AuxiliaryViewerSection.mGenIdToTabFocus = -1;   // Release selected
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
            TextureWrap? tIconWrap;
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
                ImGui.Image(tIconWrap.ImGuiHandle, new System.Numerics.Vector2(tIconWrap.Width, tIconWrap.Height));
                ImGui.SameLine();           // Do not Sameline() if there's no image, since it'll Sameline() to the TabItem above
            }
            // Name and Details and Location
            ImGui.BeginGroup();
            ImGui.Text(pObj.mName);
            ImGui.Text(pObj.mDetail);
            if (pObj.mLocation != null)
            {
                ImGui.SameLine();
                AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, pObj.mLocation, true);
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
            AuxiliaryViewerSection.GUIAlignRight((float)(22 * 5.1));
            // DELETE button
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash, ImGuiColors.DalamudRed) && io.KeyShift)
            {
                BBDataManager.DynamicRemoveGeneralObject<Loadout>(this.mPlugin, tLoadout, this.mPlugin.mBBDataManager.mLoadouts);
                AuxiliaryViewerSection.mIsRefreshRequired = true;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("[Shift + RMB] to delete the current entry"); }
            ImGui.SameLine(); ImGui.Spacing(); ImGui.SameLine(); ImGui.Spacing(); ImGui.SameLine();
            // COPY button
            if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.ClipboardList))
            {
                ImGui.SetClipboardText(JsonSerializer.Serialize(new LoadoutJson(tLoadout)));
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Copy the current entry to clipboard"); }
            ImGui.SameLine();
            // EDIT button
            if (AuxiliaryViewerSection.mTenpLoadout == null && ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.PenSquare))
            {
                AuxiliaryViewerSection.mTenpLoadout = new LoadoutJson(tLoadout);
                AuxiliaryViewerSection.mTenpLoadout.RecalculateWeight(this.mPlugin);
            }
            else if (AuxiliaryViewerSection.mTenpLoadout != null && ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.PenSquare,
                                                                                                new Vector4(0.98f, 0.33f, 0.33f, 1f)))
            {
                AuxiliaryViewerSection.mTenpLoadout = null;
            }
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Edit / Discard edit"); }
            ImGui.SameLine();
            // SAVE button
            if (AuxiliaryViewerSection.mTenpLoadout == null)
            {
                ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save);
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Save changes"); }
            }
            else if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Save, new Vector4(0.58f, 0.86f, 0.6f, 1f)) && AuxiliaryViewerSection.mTenpLoadout != null)
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
                    new System.Numerics.Vector2(ImGui.GetWindowWidth() / 2 - ImGui.GetStyle().FramePadding.X, ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetStyle().FramePadding.Y),
                    true,
                    ImGuiWindowFlags.MenuBar);
                if (ImGui.BeginMenuBar())
                {
                    ImGui.Text($"WEIGHT: {tLoadout.mWeight} / 99");
                    ImGui.EndMenuBar();
                }
                foreach (int iActionId in tLoadout.mActionIds.Keys)
                {
                    // icon
                    AuxiliaryViewerSection.mTextureCollection!.AddTextureFromItemId(Convert.ToUInt32(iActionId));
                    TextureWrap? tIconWrap = AuxiliaryViewerSection.mTextureCollection.GetTextureFromItemId(Convert.ToUInt32(iActionId));
                    if (tIconWrap != null) ImGui.Image(tIconWrap.ImGuiHandle, new System.Numerics.Vector2(tIconWrap.Width * 0.75f, tIconWrap.Height * 0.75f));
                    // link
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUISelectableLink(
                        this.mPlugin,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].mName,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId()
                        );
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUIAlignRight(ImGui.CalcTextSize($"{tLoadout.mActionIds[iActionId]}").X);
                    ImGui.Text($"{tLoadout.mActionIds[iActionId]}");
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            // Description
            ImGui.SameLine();
            {
                ImGui.BeginChild("loadout_description", new System.Numerics.Vector2(ImGui.GetWindowWidth() / 2, ImGui.GetWindowHeight() - ImGui.GetCursorPosY()));
                ImGui.TextUnformatted(tLoadout.mName);
                string tTemp = $"[{tLoadout.mGroup}] � [{tLoadout.mRole.ToString()}]";
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
                    new System.Numerics.Vector2(ImGui.GetWindowWidth() / 2 - ImGui.GetStyle().FramePadding.X, ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetStyle().FramePadding.Y),
                    true,
                    ImGuiWindowFlags.MenuBar);
                if (ImGui.BeginMenuBar())
                {
                    ImGui.Text($"WEIGHT: {tLoadout.mWeight} / 99");
                    ImGui.EndMenuBar();
                }
                foreach (int iActionId in tLoadout.mActionIds.Keys)
                {
                    // icon
                    AuxiliaryViewerSection.mTextureCollection!.AddTextureFromItemId(Convert.ToUInt32(iActionId));
                    TextureWrap? tIconWrap = AuxiliaryViewerSection.mTextureCollection.GetTextureFromItemId(Convert.ToUInt32(iActionId));
                    if (tIconWrap != null)
                    {
                        ImGui.Image(tIconWrap.ImGuiHandle, new System.Numerics.Vector2(tIconWrap.Width * 0.75f, tIconWrap.Height * 0.75f));
                    }
                    // link
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUISelectableLink(
                        this.mPlugin,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].mName,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                        true
                        );
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUIAlignRight(33);
                    // adjuster
                    AuxiliaryViewerSection.GUILoadoutEditAdjuster(this.mPlugin, iActionId);
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            // Description
            ImGui.SameLine();
            {
                ImGui.BeginChild("loadout_description", new System.Numerics.Vector2(ImGui.GetWindowWidth() / 2, ImGui.GetWindowHeight() - ImGui.GetCursorPosY()));
                // Name
                ImGui.InputText("##name", ref AuxiliaryViewerSection.mTenpLoadout!._mName, 120);
                // Group & Role
                //AuxiliaryViewerSection.GUIAlignRight(ImGui.GetFontSize() * (4 + 1));
                ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
                ImGui.InputText("##group", ref AuxiliaryViewerSection.mTenpLoadout!._mGroup, 120);
                ImGui.PopItemWidth();
                ImGui.SameLine(); ImGui.Text(" � ");
                ImGui.SameLine(); AuxiliaryViewerSection.mGUIFilter.HeaderRoleSelectables(AuxiliaryViewerSection.mTenpLoadout!._mRole);


                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Description
                ImGui.InputTextMultiline("##description",
                                        ref AuxiliaryViewerSection.mTenpLoadout!._mDescription,
                                        1024,
                                        new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X, ImGui.GetTextLineHeight() * 5));

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Action list text filter
                AuxiliaryViewerSection.GUITextFilterAction(mPlugin);
                ImGui.EndChild();
            }
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
                    AuxiliaryViewerSection.GUISelectableLink(this.mPlugin,
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
                        AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, tFragment.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    AuxiliaryViewerSection.GUISelectableLink(this.mPlugin, $"Forgetten Fragment of {tFragment.mName}", tFragment.GetGenId());
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
                        AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, tFate.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    AuxiliaryViewerSection.GUISelectableLink(this.mPlugin, $"{tFate.mName}", tFate.GetGenId());
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
                        AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, tMob.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    AuxiliaryViewerSection.GUISelectableLink(this.mPlugin, $"{tMob.mName}", tMob.GetGenId());
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
                        AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, tAction.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    AuxiliaryViewerSection.GUISelectableLink(this.mPlugin, $"{tAction.mName}", tAction.GetGenId());
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
                        AuxiliaryViewerSection.GUIButtonLocation(this.mPlugin, tVendor.mLocation);
                        ImGui.SameLine();
                    }
                    // NAME
                    AuxiliaryViewerSection.GUISelectableLink(this.mPlugin, $"{tVendor.mName}\t({tVendor.GetAmountPriceCurrency(pObj.mId).Item1}) {tVendor.GetAmountPriceCurrency(pObj.mId).Item2} {tVendor.GetAmountPriceCurrency(pObj.mId).Item3.ToString()}", tVendor.GetGenId());
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
            AuxiliaryViewerSection.mGenIdToTabFocus = pGenId;
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

        public static void GUISelectableLink(Plugin pPlugin, string pContent, int pTargetGenId, bool pIsWrappedToText = false)
        {
            ImGui.PushID(pTargetGenId);
            if (pIsWrappedToText)
            {
                if (ImGui.Selectable(pContent, 
                                        true, 
                                        ImGuiSelectableFlags.None, 
                                        new System.Numerics.Vector2(ImGui.GetFontSize() * pContent.Length * 0.5f, ImGui.GetFontSize())))
                {
                    if (!AuxiliaryViewerSection.mTabGenIds[pTargetGenId])
                    {
                        AuxiliaryViewerSection.AddTab(pPlugin, pTargetGenId);
                    }
                    else
                    {
                        AuxiliaryViewerSection.mGenIdToTabFocus = pTargetGenId;
                    }
                }
            }
            else if (ImGui.Selectable(pContent))
            {
                if (!AuxiliaryViewerSection.mTabGenIds[pTargetGenId])
                {
                    AuxiliaryViewerSection.AddTab(pPlugin, pTargetGenId);
                }
                else
                {
                    AuxiliaryViewerSection.mGenIdToTabFocus = pTargetGenId;
                }
            }
            ImGui.PopID();
        }
        public static void GUIButtonLocation(Plugin pPlugin, Location pLocation, bool rightAlign = false)
        {
            string tButtonText = $"{pLocation.mAreaFlag.ToString()} ({pLocation.mMapCoordX}, {pLocation.mMapCoordX})";
            if (rightAlign)
            {
                AuxiliaryViewerSection.GUIAlignRight(ImGui.CalcTextSize(tButtonText).X);
            }
            if (ImGui.Button(tButtonText))
            {
                pPlugin.GameGui.OpenMapWithMapLink(
                    new Dalamud.Game.Text.SeStringHandling.Payloads.MapLinkPayload(
                        pLocation.mTerritoryID, 
                        pLocation.mMapID, 
                        (float)pLocation.mMapCoordX, 
                        (float)pLocation.mMapCoordY)
                    );
                //PluginLog.LogInformation($"Showing map: {pLocation.mTerritoryID} - {pLocation.mMapID} - {(float)pLocation.mMapCoordX} - {(float)pLocation.mMapCoordY}");
            }
        }
        public static void GUIAlignRight(float pTargetItemWidth)
        {
            ImGuiStylePtr tStyle = ImGui.GetStyle();
            float tPadding = tStyle.WindowPadding.X + tStyle.FramePadding.X * 2 + tStyle.ScrollbarSize;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetCursorPosX() + ImGui.GetWindowWidth() - pTargetItemWidth - tPadding);
        }
        public static void GUILoadoutEditAdjuster(Plugin pPlugin, int pActionId)
        {
            if (AuxiliaryViewerSection.mTenpLoadout == null) return;
            ImGui.PushID(pActionId);
            if (AuxiliaryViewerSection.mTenpLoadout!.mActionIds.ContainsKey(pActionId))
            {
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.LongArrowAltUp))
                {
                    AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] += 1;
                    AuxiliaryViewerSection.mTenpLoadout!.mWeight += pPlugin.mBBDataManager.mLostActions[pActionId].mWeight;
                }
                ImGui.SameLine();
                ImGui.Text($"{AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId]}");
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.LongArrowAltDown))
                {
                    if (AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] == 1)
                        AuxiliaryViewerSection.mTenpLoadout!.mActionIds.Remove(pActionId);
                    else
                        AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] -= 1;
                    AuxiliaryViewerSection.mTenpLoadout!.mWeight -= pPlugin.mBBDataManager.mLostActions[pActionId].mWeight;
                }
            }
            else
            {
                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.LongArrowAltUp))
                {
                    AuxiliaryViewerSection.mTenpLoadout!.mActionIds[pActionId] = 1;
                    AuxiliaryViewerSection.mTenpLoadout!.mWeight += pPlugin.mBBDataManager.mLostActions[pActionId].mWeight;
                }
                ImGui.SameLine();
                ImGui.Text("-");
                ImGui.SameLine();
                ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.LongArrowAltDown);
            }
            ImGui.PopID();
        }
        public static void GUITextFilterAction(Plugin pPlugin)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
            ImGui.BeginChild("loadout_description_actionfilter");
            unsafe
            {
                AuxiliaryViewerSection.mFilter.Draw("");
                foreach (LostAction iAction in pPlugin.mBBDataManager.mLostActions.Values)
                {
                    if (AuxiliaryViewerSection.mFilter.PassFilter(iAction.mName))
                    {
                        if (AuxiliaryViewerSection.mTenpLoadout != null)
                        {
                            AuxiliaryViewerSection.GUILoadoutEditAdjuster(pPlugin, iAction.mId);
                            ImGui.SameLine();
                        }
                        ImGui.Text($"[{iAction.mWeight}] ");
                        ImGui.SameLine(); AuxiliaryViewerSection.GUISelectableLink(pPlugin, iAction.mName, iAction.GetGenId());
                    }
                }
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
