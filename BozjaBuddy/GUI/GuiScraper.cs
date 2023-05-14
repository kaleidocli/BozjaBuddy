using BozjaBuddy.Utils;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI
{
    public class GuiScraper
    {
        private bool mIsActive = true;
        private Thread? mScrapper = null;
        private Plugin mPlugin;

        private GuiScraper() { }
        public GuiScraper(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
        }

        public void Start()
        {
            if (this.mScrapper == null)
            {
                this.mScrapper = new Thread(
                        new ThreadStart(Scraper)
                    );
                this.mScrapper.Start();
            }
        }
        public void Stop()
        {
            this.mIsActive = false;
        }

        private void Scraper()
        {
            unsafe
            {
                while (this.mIsActive)
                {
                    Thread.Sleep(5000);

                    var tCharStats = this.mPlugin.Configuration.mGuiAssistConfig.charStats;

                    // Mettle, ranks, etc.
                    var tAddonMycInfo = (AtkUnitBase*)this.mPlugin.GameGui.GetAddonByName("MYCInfo");
                    if (tAddonMycInfo == null) { continue; }

                    var tNodeRank = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 7 }); if (tNodeRank == null) { continue; }
                    var tNodeMettle = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 14 }); if (tNodeMettle == null) { continue; }
                    var tNodeMettleMax = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 17 }); if (tNodeMettleMax == null) { continue; }
                    var tNodeProof = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 22 }); if (tNodeProof == null) { continue; }
                    var tNodeCluster = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 33 }); if (tNodeCluster == null) { continue; }
                    var tNodeNoto = (AtkTextNode*)UtilsGUI.GetNodeByIdPath(this.mPlugin, "MYCInfo", new int[] { 26 }); if (tNodeNoto == null) { continue; }
                    
                    tCharStats.isInit = true;
                    if (!int.TryParse(tNodeRank->NodeText.ToString(), out tCharStats.rank)) tCharStats.rank = 0;
                    if (!int.TryParse(tNodeMettle->NodeText.ToString().Replace(",", ""), out tCharStats.mettle)) tCharStats.mettle = 0;
                    if (!int.TryParse(tNodeMettleMax->NodeText.ToString().Replace(",", ""), out tCharStats.mettleMax)) tCharStats.mettleMax = 0;
                    if (!int.TryParse(tNodeProof->NodeText.ToString(), out tCharStats.proof)) tCharStats.proof = 0;
                    if (!int.TryParse(tNodeCluster->NodeText.ToString().Split('/').First(), out tCharStats.cluster)) tCharStats.cluster = 0;
                    if (!int.TryParse(tNodeNoto->NodeText.ToString(), out tCharStats.noto)) tCharStats.noto = 0;

                    // Rays
                    if (this.mPlugin.ClientState.LocalPlayer != null)
                    {
                        var tStatusList = this.mPlugin.ClientState.LocalPlayer.StatusList
                                            .Distinct()
                                            .ToDictionary(s => (int)s.StatusId, o => (int)o.StackCount);
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
                    this.mPlugin.Configuration.Save();
                }
            }
        }
    }
}
