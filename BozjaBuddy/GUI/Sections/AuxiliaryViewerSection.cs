using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using ImGuiScene;
using SamplePlugin.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static SamplePlugin.Data.TextureCollection;

namespace SamplePlugin.GUI.Sections
{
    internal class AuxiliaryViewerSection : Section
    {
        public static Dictionary<int, bool> mTabGenIds = new Dictionary<int, bool>();
        public static List<int> mTabGenIdsToDraw = new List<int>();
        private static TextureCollection? mTextureCollection = null;
        private static ImGuiTabBarFlags AUXILIARY_TAB_FLAGS = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton;

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

            //this.DrawGUIDebug();

            return true;
        }

        private void DrawTab(GeneralObject pObj)
        {
            bool[] tIsOpened = { true };
            if (ImGui.BeginTabItem(pObj.mName, ref tIsOpened[0]))
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
                if (ImGui.BeginTabBar(pObj.mName))
                {
                    // Description
                    if (ImGui.BeginTabItem("Description"))
                    {
                        //ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.HorizontalScrollbar);
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
                        ImGui.PushTextWrapPos();
                        ImGui.TextUnformatted(pObj.mDescription);
                        ImGui.PopTextWrapPos();
                        //ImGui.EndChild();
                        ImGui.EndTabItem();
                    }
                    // Sources
                    if (ImGui.BeginTabItem("Sources/Drop"))
                    {
                        ImGui.BeginChild("", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.HorizontalScrollbar);
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
                                Fate tFate = this.mPlugin.mBBDataManager.mFates[iId];
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
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }
            if (!tIsOpened[0])
            {
                AuxiliaryViewerSection.RemoveTab(this.mPlugin, pObj.GetGenId());
            }
        }

        /// <summary>
        /// For first-time binding a GeneralObject to an Auxiliary Tab. Do nothing if the object has already been bound.
        /// </summary>
        /// <param name="tPlugin"></param>
        /// <param name="pGenId"></param>
        public static void BindToGenObj(Plugin pPlugin, int pGenId)
        {
            if (AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            AuxiliaryViewerSection.mTabGenIds.Add(pGenId, false);
        }
        public static void AddTab(Plugin pPlugin, int pGenId)                               //FIXME
        {
            if (!AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            GeneralObject.GeneralObjectSalt tSalt = pPlugin.mBBDataManager.mGeneralObjects[pGenId].GetSalt();
            AuxiliaryViewerSection.mTabGenIds[pGenId] = true;
            AuxiliaryViewerSection.mTabGenIdsToDraw.Add(pGenId);
            AuxiliaryViewerSection.mTextureCollection ??= new TextureCollection(pPlugin);
            AuxiliaryViewerSection.mTextureCollection.AddTextureFromItemId(Convert.ToUInt32(tId), tSalt);
        }
        public static void RemoveTab(Plugin pPlugin, int pGenId)                            //FIXME
        {
            if (!AuxiliaryViewerSection.mTabGenIds.ContainsKey(pGenId)) return;
            int tId = pPlugin.mBBDataManager.mGeneralObjects[pGenId].mId;
            GeneralObject.GeneralObjectSalt tSalt = pPlugin.mBBDataManager.mGeneralObjects[pGenId].GetSalt();
            AuxiliaryViewerSection.mTabGenIds[pGenId] = false;
            AuxiliaryViewerSection.mTabGenIdsToDraw.Remove(pGenId);
            AuxiliaryViewerSection.mTextureCollection?.RemoveTextureFromItemId(Convert.ToUInt32(tId), tSalt);
        }
        public static void GUISelectableLink(Plugin pPlugin, string pContent, int pTargetGenId, bool pIsWrappedToText = false)
        {
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
                        AuxiliaryViewerSection.RemoveTab(pPlugin, pTargetGenId);
                        //PluginLog.LogInformation("Removing Tab");
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
                    AuxiliaryViewerSection.RemoveTab(pPlugin, pTargetGenId);
                    //PluginLog.LogInformation("Removing Tab");
                }
            }
        }
        public static void GUIButtonLocation(Plugin pPlugin, Location pLocation, bool rightAlign = false)
        {
            string tButtonText = $"{pLocation.mAreaFlag.ToString()} ({pLocation.mMapCoordX}, {pLocation.mMapCoordX})";
            if (rightAlign)
            {
                ImGuiStylePtr tStyle = ImGui.GetStyle();
                float tPadding = tStyle.WindowPadding.X + tStyle.FramePadding.X * 2 + tStyle.ScrollbarSize;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetCursorPosX() + ImGui.GetWindowWidth() - ImGui.CalcTextSize(tButtonText).X - tPadding);
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

        public override void DrawGUIDebug()
        {
            string tRes = "";
            foreach (int i in AuxiliaryViewerSection.mTabGenIdsToDraw) tRes += i.ToString();
            ImGui.Text($"Aux: {tRes}");
        }
        public override void Dispose()
        {
        }
    }
}
