using BozjaBuddy.Data;
using BozjaBuddy.GUI.NodeGraphViewer.utils;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using System.Text.Json;
using Lumina.Excel.GeneratedSheets;


namespace BozjaBuddy.GUI.NodeGraphViewer.ext
{
    /// <summary>
    /// Represents a node that display info from a BozjaBuddy's Auxiliary tab.
    /// </summary>
    public class AuxNode : BBNode
    {
        public new const string nodeType = "AuxNode";
        public new static Vector2 minHandleSize = new(Node.minHandleSize.X * 4.5f, Node.minHandleSize.Y);
        public override string mType { get; } = AuxNode.nodeType;
        private InnerBodyTab _currTab = InnerBodyTab.Links;

        private static LostActionTableSection? _lostActionTableSection = null;

        public AuxNode() : base()
        {
            mStyle.colorUnique = UtilsGUI.Colors.NormalBar_Grey;
        }
        public new void SetSeed(NodeCanvas.Seed pSeed) => base.SetSeed(pSeed);
        protected override NodeInteractionFlags DrawBody(Vector2 pNodeOSP, float pCanvasScaling)
        {
            var tRes = NodeInteractionFlags.None;
            if (this.mPlugin == null && BBNode.kPlugin != null) this.mPlugin = BBNode.kPlugin;
            if (this.mPlugin == null || !this.mGenId.HasValue) return tRes;
            if (!this.mPlugin.mBBDataManager.mGeneralObjects.TryGetValue(this.mGenId.Value, out var tGenObj) || tGenObj == null) return tRes;

            var tStyle = ImGui.GetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, tStyle.ItemSpacing * pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, tStyle.ItemInnerSpacing * pCanvasScaling);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, tStyle.FramePadding * pCanvasScaling);
            ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.NodeText);
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, 0.2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, 0.6f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, 1));
            ImGui.PushStyleColor(ImGuiCol.Tab, UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, 0.2f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, 0.35f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, UtilsGUI.AdjustTransparency(this.mStyle.colorUnique, 0.45f));

            if (tGenObj is Loadout)
            {
                this.DrawInnerHeader_Loadout(tGenObj, pCanvasScaling);
                if (AuxiliaryViewerSection.mTenpLoadout == null)
                    this.DrawInnerContent_Loadout(tGenObj, pCanvasScaling);
                else
                    this.DrawInnerContent_LoadoutEdit(tGenObj, pCanvasScaling);
            }
            else
            {
                this.DrawInnerHeader(tGenObj, pCanvasScaling);
                tRes |= this._currTab == InnerBodyTab.Overview
                        ? this.DrawInnerContent_Overview(tGenObj, pCanvasScaling)
                        : this.DrawInnerContent_Links(tGenObj, pCanvasScaling);
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            return tRes;
        }
        protected virtual NodeInteractionFlags DrawInnerHeader(GeneralObject pObj, float pCanvasScaling)
        {
            if (this.mPlugin == null) return NodeInteractionFlags.None;

            // Icon
            TextureWrap? tIconWrap;
            float tExtraScaling = 1;
            switch (pObj.GetSalt())
            {
                case GeneralObject.GeneralObjectSalt.Fragment:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.Item, true);
                    break;
                case GeneralObject.GeneralObjectSalt.Item:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.Item, true);
                    break;
                case GeneralObject.GeneralObjectSalt.Fate:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetStandardTexture((uint)this.mPlugin.mBBDataManager.mFates[pObj.mId].mType);
                    break;
                case GeneralObject.GeneralObjectSalt.Mob:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetStandardTexture((uint)this.mPlugin.mBBDataManager.mMobs[pObj.mId].mType);
                    break;
                case GeneralObject.GeneralObjectSalt.FieldNote:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), TextureCollection.Sheet.FieldNote, true);
                    if (tIconWrap != null) tExtraScaling = 1.24f;
                    break;
                case GeneralObject.GeneralObjectSalt.Quest:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetStandardTexture(
                            ((BozjaBuddy.Data.Quest)pObj).mType switch
                            {
                                BozjaBuddy.Data.Quest.QuestType.Msq => TextureCollection.StandardIcon.QuestMSQ,
                                BozjaBuddy.Data.Quest.QuestType.Side => TextureCollection.StandardIcon.QuestSide,
                                BozjaBuddy.Data.Quest.QuestType.Key => TextureCollection.StandardIcon.QuestKey,
                                BozjaBuddy.Data.Quest.QuestType.Repeatable => TextureCollection.StandardIcon.QuestRepeatable,
                                _ => TextureCollection.StandardIcon.QuestSide
                            }
                        );
                    if (tIconWrap != null) tExtraScaling = 1.3f;
                    break;
                default:
                    tIconWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(pObj.mId), pTryLoadTexIfFailed: true);
                    break;
            }
            if (tIconWrap != null)
            {
                float tLinkHeight = 47;
                UtilsGUI.SelectableLink_Image(this.mPlugin, pObj.GetGenId(), tIconWrap, pImageScaling: (2 - (float)tIconWrap.Height / tLinkHeight) * pCanvasScaling * tExtraScaling, pAuxNode: this);
                ImGui.SameLine();           // Do not Sameline() if there's no image, since it'll Sameline() to the TabItem above
            }
            // Details and location/alarm
            ImGui.BeginGroup();
            this.DrawDetailPackage(pObj, pCanvasScaling);
            // Tabs
            if (ImGui.BeginTabBar(pObj.mName))
            {
                // Links
                if (ImGui.BeginTabItem("Links"))
                {
                    this._currTab = InnerBodyTab.Links;
                    ImGui.EndTabItem();
                }
                // Description
                if (ImGui.BeginTabItem("Overview"))
                {
                    this._currTab = InnerBodyTab.Overview;
                    ImGui.EndTabItem();
                }
                // Alarm and Location button
                if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Fate)
                {
                    ImGui.SameLine();
                    //ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
                    UtilsGUI.ACPUFateCeButton(this.mPlugin, pObj.mId, pObj.mName);
                    //ImGui.PopStyleVar();
                }
                if (pObj.mLocation != null)
                {
                    ImGui.SameLine();
                    UtilsGUI.LocationLinkButton(this.mPlugin, pObj.mLocation, pScaling: pCanvasScaling, pUseIcon: true);
                }
                else if (pObj is BozjaBuddy.Data.Quest)
                {
                    var tQuest = ((BozjaBuddy.Data.Quest)pObj);
                    if (tQuest.mIssuerLocation != null)
                    {
                        ImGui.SameLine();
                        UtilsGUI.LocationLinkButton(this.mPlugin, tQuest.mIssuerLocation, pScaling: pCanvasScaling, pUseIcon: true);
                    }
                }

                ImGui.EndTabBar();
            }
            ImGui.EndGroup();
            return NodeInteractionFlags.None;
        }
        protected virtual NodeInteractionFlags DrawInnerContent_Links(GeneralObject pObj, float pCanvasScaling)
        {
            var tRes = NodeInteractionFlags.None;
            if (this.mPlugin == null) return tRes;

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
                if (ImGui.BeginMenu("Included in Fate chain:"))
                {
                    int tCounter = 1;
                    do
                    {
                        if (tCounter > 1) ImGui.Text(new string('\t', tCounter - 1) + "|__");
                        ImGui.SameLine();
                        UtilsGUI.SelectableLink_WithPopup(this.mPlugin,
                            this.mPlugin.mBBDataManager.mFates[iCurrFateId].mName,
                            this.mPlugin.mBBDataManager.mFates[iCurrFateId].GetGenId(),
                            true,
                            pAuxNode: this);
                        iCurrFateId = this.mPlugin.mBBDataManager.mFates[iCurrFateId].mChainFateNext;
                        tCounter++;
                    }
                    while (iCurrFateId != -1);
                    ImGui.EndMenu();
                }
                ImGui.Separator();
            }
            // Quest's prev 'n next
            if (pObj.GetSalt() == GeneralObject.GeneralObjectSalt.Quest)
            {
                var tQuest = (BozjaBuddy.Data.Quest)pObj;
                // Prev
                UtilsGUI.TextDescriptionForWidget("<<< Previous quests");
                foreach (var prevId in tQuest.mPrevQuestIds)
                {
                    if (!this.mPlugin.mBBDataManager.mGeneralObjects.TryGetValue(GeneralObject.IdToGenId(prevId, (int)pObj.GetSalt()), out var tPrevObj)
                        || tPrevObj == null)
                    {
                        continue;
                    }
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tPrevObj.mName}", tPrevObj.GetGenId(), pAuxNode: this);
                }
                // Next
                ImGui.Separator();
                Utils.AlignRight(ImGui.CalcTextSize("Next quests >>>").X);
                UtilsGUI.TextDescriptionForWidget("Next quests >>>");
                foreach (var nextId in tQuest.mNextQuestIds)
                {
                    if (!this.mPlugin.mBBDataManager.mGeneralObjects.TryGetValue(GeneralObject.IdToGenId(nextId, (int)pObj.GetSalt()), out var tNextObj)
                        || tNextObj == null)
                    {
                        continue;
                    }
                    Utils.AlignRight(ImGui.CalcTextSize($"{tNextObj.mName}").X + 11.5f * pCanvasScaling);
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tNextObj.mName}", tNextObj.GetGenId(), pAuxNode: this);
                }
            }

            if (pObj.mLinkFragments.Count != 0 && ImGui.CollapsingHeader($"Fragment ({pObj.mLinkFragments.Count})"))
            {
                foreach (int iId in pObj.mLinkFragments)
                {
                    Fragment tFragment = this.mPlugin.mBBDataManager.mFragments[iId];
                    // LOCATION
                    if (tFragment.mLocation != null)
                    {
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFragment.mLocation, pUseIcon: true);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"Fragment: {tFragment.mName}", tFragment.GetGenId(), pAuxNode: this);
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
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFate.mLocation, pUseIcon: true);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tFate.mName}", tFate.GetGenId(), pAuxNode: this);
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
                        UtilsGUI.LocationLinkButton(this.mPlugin, tMob.mLocation, pUseIcon: true);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tMob.mName}", tMob.GetGenId(), pAuxNode: this);
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
                        UtilsGUI.LocationLinkButton(this.mPlugin, tAction.mLocation, pUseIcon: true);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tAction.mName}", tAction.GetGenId(), pAuxNode: this);
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
                        UtilsGUI.LocationLinkButton(this.mPlugin, tVendor.mLocation, pUseIcon: true);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tVendor.mName}\t({tVendor.GetAmountPriceCurrency(pObj.mId).Item1}) {tVendor.GetAmountPriceCurrency(pObj.mId).Item2} {tVendor.GetAmountPriceCurrency(pObj.mId).Item3.ToString()}", tVendor.GetGenId(), pAuxNode: this);
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
                        UtilsGUI.LocationLinkButton(this.mPlugin, tFieldNote.mLocation, pUseIcon: true);
                        ImGui.SameLine();
                    }
                    // NAME
                    UtilsGUI.SelectableLink_WithPopup(this.mPlugin, $"{tFieldNote.mName}", tFieldNote.GetGenId(), pAuxNode: this);
                    ImGui.Separator();
                }
            }

            return tRes;
        }
        protected virtual NodeInteractionFlags DrawInnerContent_Overview(GeneralObject pObj, float pCanvasScaling)
        {
            var tRes = NodeInteractionFlags.None;
            if (this.mPlugin == null) return tRes;

            ImGui.PushTextWrapPos(0);
            if (pObj.mIGMarkup == null)
                ImGui.TextUnformatted(pObj.mDescription);
            else
                pObj.mIGMarkup!.DrawGUI();
            ImGui.PopTextWrapPos();

            return tRes;
        }
        protected virtual void DrawDetailPackage(GeneralObject pObj, float pCanvasScaling)
        {
            Dictionary<TextureCollection.StandardIcon, string> tPackage = pObj.mDetailPackage;

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, ImGui.GetStyle().ItemSpacing.Y));
            // Rarity
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Rarity, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(FontAwesomeIcon.Star);
                    UtilsGUI.SetTooltipForLastItem("Rank/Rarity");
                    ImGui.SameLine();
                    ImGui.Text(v + "   ");
                    UtilsGUI.SetTooltipForLastItem("Rank/Rarity");
                    ImGui.SameLine();
                }
            }
            // Uses
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Uses, out var v) && v != null)
                {
                    ImGui.Text("[" + v + "]    ");
                    UtilsGUI.SetTooltipForLastItem("Charges");
                    ImGui.SameLine();
                }
            }
            // Weight
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Weight, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(FontAwesomeIcon.WeightHanging);
                    UtilsGUI.SetTooltipForLastItem("Weight");
                    ImGui.SameLine();
                    ImGui.Text(" " + v + "    ");
                    UtilsGUI.SetTooltipForLastItem("Weight");
                    ImGui.SameLine();
                }
            }
            // Cast
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Cast, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(FontAwesomeIcon.Clock);
                    UtilsGUI.SetTooltipForLastItem("Cast time");
                    ImGui.SameLine();
                    ImGui.Text(" " + v + " ");
                    UtilsGUI.SetTooltipForLastItem("Cast time");
                    ImGui.SameLine();
                }
            }
            // Recast
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Recast, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(FontAwesomeIcon.HourglassHalf);
                    UtilsGUI.SetTooltipForLastItem("Recast time");
                    ImGui.SameLine();
                    ImGui.Text(" " + v + "    ");
                    UtilsGUI.SetTooltipForLastItem("Recast time");
                    ImGui.SameLine();
                }
            }
            // Cluster buyable
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Cluster, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(TextureCollection.StandardIcon.Cluster);
                    UtilsGUI.SetTooltipForLastItem("Can be obtained using Cluster?");
                    ImGui.SameLine();
                    ImGui.Text(v + "  ");
                    UtilsGUI.SetTooltipForLastItem("Can be obtained using Cluster?");
                    ImGui.SameLine();
                }
            }
            // Poetic
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Poetic, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(TextureCollection.StandardIcon.Poetic);
                    UtilsGUI.SetTooltipForLastItem("Poetic reward");
                    ImGui.SameLine();
                    ImGui.Text(v + "   ");
                    UtilsGUI.SetTooltipForLastItem("Poetic reward");
                    ImGui.SameLine();
                }
            }
            // Mettle
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Mettle, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(TextureCollection.StandardIcon.Mettle);
                    UtilsGUI.SetTooltipForLastItem("Mettle reward");
                    ImGui.SameLine();
                    ImGui.Text(v + "   ");
                    UtilsGUI.SetTooltipForLastItem("Mettle reward");
                    ImGui.SameLine();
                }
            }
            // Exp
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.Exp, out var v) && v != null)
                {
                    UtilsGUI.DrawIcon(TextureCollection.StandardIcon.Exp);
                    UtilsGUI.SetTooltipForLastItem("EXP reward");
                    ImGui.SameLine();
                    ImGui.Text(v + "   ");
                    UtilsGUI.SetTooltipForLastItem("EXP reward");
                    ImGui.SameLine();
                }
            }
            // None 
            {
                if (tPackage.TryGetValue(TextureCollection.StandardIcon.None, out var v) && v != null)
                {
                    ImGui.Text(v + "  ");
                    ImGui.SameLine();
                }
            }
            ImGui.Text("");
            ImGui.PopStyleVar();
        }

        // The loadout stuff are copy-pasted straight from AuxiliaryViewerSection
        protected virtual NodeInteractionFlags DrawInnerHeader_Loadout(GeneralObject pObj, float pCanvasScaling)
        {
            if (this.mPlugin == null) return NodeInteractionFlags.None;
            var tRes = NodeInteractionFlags.None;

            ImGuiIOPtr io = ImGui.GetIO();
            Loadout tLoadout = this.mPlugin.mBBDataManager.mLoadouts[pObj.mId];

            // DELETE button
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash, UtilsGUI.AdjustTransparency(ImGuiColors.DalamudRed, 0.4f)) && io.KeyShift)
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
            // Instruction
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("To edit your Custom Loadout, press the Pen icon button on the right.\n=========== WHILE EDITING ===========\n- There is an Action table below to add/remove actions from loadout.\n- Similar to in-game loadout, [Shift+LMB/RMB] on action's icon to add/remove action from loadout.\n- The grey number on the right of action's name is its weight.");

            ImGui.SameLine();
            string tTemp = $"[{tLoadout.mGroup}] • [{tLoadout.mRole}]";
            AuxiliaryViewerSection.GUIAlignRight(tTemp); ImGui.TextUnformatted(tTemp);

            ImGui.Separator();

            return tRes;
        }
        protected virtual NodeInteractionFlags DrawInnerContent_Loadout(GeneralObject pObj, float pCanvasScaling)
        {
            if (this.mPlugin == null) return NodeInteractionFlags.None;
            var tRes = NodeInteractionFlags.None;

            Loadout tLoadout = this.mPlugin.mBBDataManager.mLoadouts[pObj.mId];
            // Action List
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
                ImGui.BeginChild("loadout_actionlist",
                    new Vector2(ImGui.GetWindowWidth() / 5 * 3 - ImGui.GetStyle().FramePadding.X, ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - ImGui.GetStyle().FramePadding.Y),
                    true,
                    ImGuiWindowFlags.MenuBar);
                var tOriScale = Utils.GetCurrFontScale();
                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);

                if (ImGui.BeginMenuBar())
                {
                    ImGui.TextColored(tLoadout.mWeight > 99 ? UtilsGUI.Colors.NormalText_Red : UtilsGUI.Colors.BackgroundText_Grey, $"WEIGHT: {tLoadout.mWeight} / 99");
                    ImGui.EndMenuBar();
                }
                foreach (int iActionId in tLoadout.mActionIds.Keys)
                {
                    // icon
                    UtilsGameData.kTextureCollection?.AddTextureFromItemId(Convert.ToUInt32(iActionId));
                    TextureWrap? tIconWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(iActionId));
                    if (tIconWrap != null)
                    {
                        UtilsGUI.SelectableLink_Image(
                                this.mPlugin,
                                this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                                tIconWrap,
                                pIsShowingCacheAmount: true,
                                pImageScaling: 0.6f * pCanvasScaling,
                                pAuxNode: this
                            );
                    }
                    // link
                    ImGui.SameLine();
                    UtilsGUI.SelectableLink_WithPopup(
                        this.mPlugin,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].mName,
                        this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                        pAuxNode: this
                        );
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUIAlignRight(-15);
                    ImGui.Text($"{tLoadout.mActionIds[iActionId]}");
                }

                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            // Description
            ImGui.SameLine();
            {
                ImGui.BeginChild("loadout_description", new Vector2(ImGui.GetWindowWidth() / 5 * 2 - ImGui.GetStyle().FramePadding.X, ImGui.GetWindowHeight() - ImGui.GetCursorPosY()));
                var tOriScale = Utils.GetCurrFontScale();
                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushTextWrapPos();
                if (pObj.mIGMarkup == null)
                    ImGui.TextUnformatted(pObj.mDescription);
                else
                    pObj.mIGMarkup!.DrawGUI();
                ImGui.PopTextWrapPos();

                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);
                ImGui.EndChild();
            }

            return tRes;
        }
        protected virtual NodeInteractionFlags DrawInnerContent_LoadoutEdit(GeneralObject pObj, float pCanvasScaling)
        {
            if (this.mPlugin == null) return NodeInteractionFlags.None;
            var tRes = NodeInteractionFlags.None;

            LoadoutJson tLoadout = AuxiliaryViewerSection.mTenpLoadout!;
            // Action List
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
                ImGui.BeginChild("loadout_actionlist",
                    new System.Numerics.Vector2(ImGui.GetWindowWidth() / 5 * 2 - ImGui.GetStyle().FramePadding.X, 241 * pCanvasScaling),
                    true,
                    ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar);
                var tOriScale = Utils.GetCurrFontScale();
                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);

                if (ImGui.BeginMenuBar())
                {
                    ImGui.TextColored(tLoadout.mWeight > 99 ? UtilsGUI.Colors.NormalText_Red : UtilsGUI.Colors.BackgroundText_Grey, $"WEIGHT: {tLoadout.mWeight} / 99");
                    ImGui.SameLine();
                    // Lost action grid popup
                    //ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + (ImGui.GetWindowWidth() / 5 * 2 + 10), 0));
                    AuxiliaryViewerSection.GUIAlignRight(-5);
                    if (ImGui.Button(" + "))
                    {
                        ImGui.OpenPopup("##lagrid");
                    }
                    if (ImGui.BeginPopup("##lagrid"))
                    {
                        AuxNode.GetLostActionTableSection()?.DrawTable_GridOnly();
                        ImGui.EndPopup();
                    }
                    ImGui.EndMenuBar();
                }
                UtilsGUI.InputPayload tInputPayload = new();
                foreach (int iActionId in tLoadout.mActionIds.Keys)
                {
                    // icon
                    UtilsGameData.kTextureCollection?.AddTextureFromItemId(Convert.ToUInt32(iActionId));
                    TextureWrap? tIconWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(Convert.ToUInt32(iActionId));
                    if (tIconWrap != null
                        && UtilsGUI.SelectableLink_Image(
                                this.mPlugin,
                                this.mPlugin.mBBDataManager.mLostActions[iActionId].GetGenId(),
                                tIconWrap,
                                pIsAuxiLinked: !ImGui.GetIO().KeyShift,
                                pIsShowingCacheAmount: true,
                                pImageScaling: 0.6f * pCanvasScaling,
                                pInputPayload: tInputPayload,
                                pAdditionalHoverText: $"[Shift+LMB/RMB] Add/remove from loadout\n",
                                pAuxNode: this
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
                        true,
                        pAuxNode: this
                        );
                    ImGui.SameLine();
                    ImGui.TextColored(UtilsGUI.Colors.BackgroundText_Grey, $"+{this.mPlugin.mBBDataManager.mLostActions[iActionId].mWeight}");
                    ImGui.SameLine();
                    AuxiliaryViewerSection.GUIAlignRight(-15);
                    // adjuster
                    ImGui.Text(tLoadout.mActionIds[iActionId].ToString());
                }

                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }

            // Description
            ImGui.SameLine();
            {
                ImGui.BeginChild("loadout_description", new System.Numerics.Vector2(ImGui.GetWindowWidth() / 5 * 3 - ImGui.GetStyle().FramePadding.X, 241 * pCanvasScaling));
                var tOriScale = Utils.GetCurrFontScale();
                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);

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

                Utils.PopFontScale();
                Utils.PushFontScale(tOriScale);
                ImGui.EndChild();
            }

            // Action table
            AuxNode.GetLostActionTableSection()?.DrawTable_GridOnly(pScaling: pCanvasScaling);

            return tRes;
        }

        public override void Dispose()
        {

        }

        public enum InnerBodyTab
        {
            None = 0,
            Links = 1,
            Overview = 2
        }


        private static LostActionTableSection? GetLostActionTableSection()
        {
            if (AuxNode._lostActionTableSection == null && BBNode.kPlugin != null)
            {
                AuxNode._lostActionTableSection = new(BBNode.kPlugin);
            }
            return AuxNode._lostActionTableSection;
        }
    }
}
