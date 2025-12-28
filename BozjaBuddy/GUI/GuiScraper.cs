using BozjaBuddy.Utils;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using static Lumina.Data.Parsing.Uld.NodeData;

namespace BozjaBuddy.GUI
{
    public class GuiScraper
    {
        private bool mIsActive = true;
        private Thread? mScrapper = null;
        private Plugin mPlugin;
        private Cycles mCycles = new();

        private GuiScraper() { }
        public GuiScraper(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        /// <summary> DEPRECATED as threading is not advised when interacting with the game. </summary>
        private void Start()
        {
            if (this.mScrapper == null)
            {
                this.mScrapper = new Thread(
                        new ThreadStart(Scraper)
                    );
                this.mScrapper.Start();
            }
        }
        /// <summary> DEPRECATED as threading is not advised when interacting with the game. </summary>
        private void Stop()
        {
            this.mIsActive = false;
        }
        /// <summary> Main thread scraping. Put this in Framework.OnUpdate. </summary>
        public void Scrape() 
        {
            unsafe
            {
                if (!this.mCycles.CheckCycle("masterKey", 0.2f)) return;                                  // originally key='mycInfo'
                if (AtkStage.Instance() == null || AtkStage.Instance()->RaptureAtkUnitManager == null) return;      // null on logging out
                if (this.mCycles.CheckCycle("mycInfo", 5))
                {
                    this.Scraper_MycInfo();
                }
                if (this.mCycles.CheckCycle("mycWarResultNotebook", 0.2f)) this.Scraper_MycWarResultNotebook();
                if (this.mCycles.CheckCycle("save", 60)) this.mPlugin.Configuration.Save();
            }
        }

        private void Scraper()
        {
            unsafe
            {
                while (this.mIsActive)
                {
                    Thread.Sleep(200);
                    if (AtkStage.Instance() == null || AtkStage.Instance()->RaptureAtkUnitManager == null) return;      // null on logging out
                    if (this.mCycles.CheckCycle("mycInfo", 5)) this.Scraper_MycInfo();
                    if (this.mCycles.CheckCycle("mycWarResultNotebook", 0.2f)) this.Scraper_MycWarResultNotebook();
                    if (this.mCycles.CheckCycle("save", 60)) this.mPlugin.Configuration.Save();
                }
            }
        }
        private void Scraper_MycInfo()
        {
            unsafe
            {
                var tCharStats = this.mPlugin.Configuration.mGuiAssistConfig.charStats;

                // Mettle, ranks, etc.
                var tAddonMycInfo = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCInfo").Address;
                if (tAddonMycInfo == null) { return; }

                var tNodeRank = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 7 }); 
                if (tNodeRank == null) 
                {
                    tCharStats.noto = -1;
                    return; 
                }
                var tNodeMettle = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 14 }); 
                if (tNodeMettle == null)
                {
                    tCharStats.noto = -1;
                    return;
                }
                var tNodeMettleMax = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 17 }); 
                if (tNodeMettleMax == null)
                {
                    tCharStats.noto = -1;
                    return;
                }
                var tNodeProof = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 22 }); 
                if (tNodeProof == null)
                {
                    tCharStats.noto = -1;
                    return;
                }
                var tNodeCluster = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 33 }); 
                if (tNodeCluster == null)
                {
                    tCharStats.noto = -1;
                    return;
                }
                var tNodeNoto = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 26 }); 
                if (tNodeNoto == null)
                {
                    tCharStats.noto = -1;
                    return;
                }

                tCharStats.isInit = true;
                if (!int.TryParse(tNodeRank->NodeText.ToString(), out tCharStats.rank)) tCharStats.rank = 0;
                if (!int.TryParse(tNodeMettle->NodeText.ToString().Replace(",", ""), out tCharStats.mettle)) tCharStats.mettle = 0;
                if (!int.TryParse(tNodeMettleMax->NodeText.ToString().Replace(",", ""), out tCharStats.mettleMax)) tCharStats.mettleMax = 0;
                if (!int.TryParse(tNodeProof->NodeText.ToString(), out tCharStats.proof)) tCharStats.proof = 0;
                if (!int.TryParse(tNodeCluster->NodeText.ToString().Split('/').First(), out tCharStats.cluster)) tCharStats.cluster = 0;
                if (!int.TryParse(tNodeNoto->NodeText.ToString(), out tCharStats.noto)) tCharStats.noto = 0;

                // Rays
                if (this.mPlugin.ObjectTable.LocalPlayer != null)
                {
                    Dictionary<int, int> tStatusList = new();
                    foreach (Dalamud.Game.ClientState.Statuses.IStatus s in this.mPlugin.ObjectTable.LocalPlayer.StatusList)
                    {
                        tStatusList.TryAdd((int)s.StatusId, (int)s.Param);
                    }
                    foreach (int iStatusId in new int[] { 2625, 2626, 2627 })
                    {
                        if (tStatusList.TryGetValue(iStatusId, out int tTemp))
                        {
                            switch (iStatusId)
                            {
                                case 2625: tCharStats.rayFortitude = tTemp == 0 ? tCharStats.rayFortitude : tTemp; break;
                                case 2626: tCharStats.rayValor = tTemp == 0 ? tCharStats.rayValor : tTemp; break;
                                case 2627: tCharStats.raySuccor = tTemp == 0 ? tCharStats.raySuccor : tTemp; break;
                            }
                        }
                    }
                }
                this.mPlugin.Configuration.mGuiAssistConfig.charStats = tCharStats;
            }
        }
        /// <summary>
        /// https://github.com/goatcorp/Dalamud/blob/fa73ccd3eeb3d7c08a07031e5485bc5aa29d13d8/Dalamud/Interface/Internal/UiDebug.cs#L260
        /// </summary>
        private void Scraper_MycWarResultNotebook()
        {
            unsafe
            {
                var tAddon = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCWarResultNotebook").Address;
                if (tAddon == null) return;
                HashSet<int> tUserFieldNotes = this.mPlugin.Configuration.mUserFieldNotes;

                // Page
                int tCurrPage = 1;
                int[] tPageNodeIds = { 19, 20, 21, 22, 23 };
                foreach (int id in tPageNodeIds)
                {
                    var tPageImgNode = UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCWarResultNotebook", new int[] { id, 5 });     // id of the frame around the page number
                    if (tPageImgNode == null) continue;
                    if (tPageImgNode->IsVisible()) { tCurrPage = id - 18; break; }
                }
                // Note
                int[] tNoteNodeIds = { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
                foreach (int id in tNoteNodeIds)
                {
                    var tNoteImgNode = (AtkImageNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCWarResultNotebook", new int[] { id, 6 }); // id of the note's character icon image
                    if (tNoteImgNode == null
                        || tNoteImgNode->PartsList == null
                        || tNoteImgNode->PartId > tNoteImgNode->PartsList->PartCount) continue;

                    var tTexInfo = tNoteImgNode->PartsList->Parts[tNoteImgNode->PartId].UldAsset;
                    if (tTexInfo == null) continue;
                    var tTexType = tTexInfo->AtkTexture.TextureType;
                    if (tTexType != TextureType.Resource) continue;
                    var texFileNameStdString = &tTexInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
                    var texString = texFileNameStdString->Length < 16
                        ? Marshal.PtrToStringAnsi((IntPtr)texFileNameStdString->Buffer)
                        : Marshal.PtrToStringAnsi((IntPtr)texFileNameStdString->BufferPtr);
                    if (texString == null) continue;

                    if (texString == "ui/icon/072000/072576_hr1.tex"
                        || texString == "ui/icon/072000/072576.tex"
                        || texString == "ui/icon/072000/072608_hr1.tex"
                        || texString == "ui/icon/072000/072608.tex")
                        tUserFieldNotes.Remove((id - 7) + ((tCurrPage - 1) * 10));
                    else 
                        tUserFieldNotes.Add((id - 7) + ((tCurrPage - 1) * 10));
                }
            }
        }

        private struct Cycles
        {
            public DateTime save = DateTime.Now;
            public DateTime mycInfo = DateTime.Now;
            public Dictionary<string, DateTime> cycles = new();

            public Cycles() { }

            public bool CheckCycle(string pKey, float pThreshold)
            {
                if (!this.cycles.ContainsKey(pKey))
                {
                    this.cycles[pKey] = DateTime.Now;
                }
                if ((DateTime.Now - this.cycles[pKey]).TotalSeconds > pThreshold)
                {
                    this.cycles[pKey] = DateTime.Now;
                    return true;
                }
                return false;
            }
        }
    }
}
