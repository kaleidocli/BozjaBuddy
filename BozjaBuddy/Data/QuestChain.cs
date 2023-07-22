using BozjaBuddy.GUI.NodeGraphViewer;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using Dalamud.Logging;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;

namespace BozjaBuddy.Data
{
    public class QuestChain : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.QuestChain;
        public override int mId { get; set; } = -1;
        public override string mName { get; set; } = "";
        public override string mDescription { get; set; } = "";
        public HashSet<int> mQuestStarts { get; set; } = new();
        public HashSet<int> mQuestEnds { get; set; } = new();
        private HashSet<int> mQuests = new();
        private NodeCanvas? mCanvas = null;

        protected override Plugin mPlugin { get; set; }
        public Lumina.Excel.GeneratedSheets.JournalGenre? mLumina { get; set; } = new();

        public QuestChain(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            this.mId = Convert.ToInt32(pPackage["id"]);
            this.mName = pPackage["name"] is System.DBNull ? string.Empty : (string)pPackage["name"];
            this.mDescription = pPackage["description"] is System.DBNull ? string.Empty : (string)pPackage["description"];
            this.mQuestStarts = (pPackage["questStarts"] is System.DBNull ? string.Empty : (string)pPackage["questStarts"])
                                                         .Split("-")
                                                         .Where(i => !i.IsNullOrEmpty() && Convert.ToInt32(i) != 0)
                                                         .Select(o => Convert.ToInt32(o))
                                                         .ToHashSet();
            this.mQuestEnds = (pPackage["questEnds"] is System.DBNull ? string.Empty : (string)pPackage["questEnds"])
                                             .Split("-")
                                             .Where(i => !i.IsNullOrEmpty() && Convert.ToInt32(i) != 0)     // leave this be. We need ToInt32() break if something is wrong in the DB.
                                             .Select(o => Convert.ToInt32(o))
                                             .ToHashSet();
            int tJournalGenre = Convert.ToInt32(pPackage["journalGenre"]);

            if (this.mLumina != null && tJournalGenre > 0)
                this.mLumina = this.mPlugin.DataManager.Excel.GetSheet<JournalGenre>()?.GetRow((uint)tJournalGenre);

            this.mUiTooltip = this.mDescription;
        }
        public bool ContainQuest(int pQuestId) => this.mQuests.Contains(pQuestId);

        public override string GetReprClipboardTooltip()
        {
            return "";
        }
        protected override void SetUpAuxiliary()
        {
            this.mDetail = "";
            this.mDescription = "";
            this.mIGMarkup = new GUI.IGMarkup.IGMarkup("");
        }
        protected override void SetUpNodeInfo()
        {

        }
        protected override string GenReprUiTooltip()
        {
            return this.mUiTooltip;
        }
        public string GetCanvasData() => JsonConvert.SerializeObject(this.mCanvas, Formatting.Indented);

        public void SetUpAfterDbLoad(ref Dictionary<int, Quest> pBbdmQuests)
        {
            this.LinkQuestsToChain(ref pBbdmQuests);
            this.SetUpCanvas(ref pBbdmQuests);
        }
        private void LinkQuestsToChain(ref Dictionary<int, Quest> pBbdmQuests)
        {
            foreach (var iStart in this.mQuestStarts)
            {
                this.LinkQuestsToChainDriver(iStart, ref pBbdmQuests);
            }
        }
        private void LinkQuestsToChainDriver(int pQuestId, ref Dictionary<int, Quest> pBbdmQuests)
        {
            // Check deadend
            if (!pBbdmQuests.TryGetValue(pQuestId, out Quest? pQuest)
                || pQuest == null) return;

            // Add
            this.mQuests.Add(pQuest.mId);
            pBbdmQuests[pQuestId].mQuestChains.Add(this.mId);

            // Check endings
            if (this.mQuestEnds.Contains(pQuest.mId)) return;
            // Recur
            foreach (var iChildId in pQuest.mNextQuestIds)
            {
                this.LinkQuestsToChainDriver(iChildId, ref pBbdmQuests);
            }
        }
        private void SetUpCanvas(ref Dictionary<int, Quest> pBbdmQuests)
        {
            HashSet<int> tAdded = new();
            Dictionary<int, int> tQuestIdAndChildCount = new();
            Dictionary<int, int> tQuestIdAndNonfirstStarterCount = new();
            Dictionary<int, string> tQuestIdToNodeId = new();
            // Set up general canvas stuff
            foreach (var iStart in this.mQuestStarts)
            {
                //PluginLog.LogDebug($"> QuestChain.SetUpCanvas(): proccing starter qid={iStart}");
                this.SetUpCanvasDriver(iStart, ref pBbdmQuests, tAdded, tQuestIdAndChildCount, tQuestIdAndNonfirstStarterCount, tQuestIdToNodeId, pIsStarter: true);
            }
            // Set up edges
            if (this.mCanvas != null)
            {
                foreach (var questId in this.mQuests)
                {
                    if (!tQuestIdToNodeId.TryGetValue(questId, out var questNodeId)) continue;
                    if (!pBbdmQuests.TryGetValue(questId, out var quest) || quest == null) continue;
                    foreach (int nextQuestId in quest.mNextQuestIds)
                    {
                        if (!tQuestIdToNodeId.TryGetValue(nextQuestId, out var nextQuestNodeId)) continue;
                        this.mCanvas.AddEdge(questNodeId, nextQuestNodeId);
                    }
                }
            }
        }
        private void SetUpCanvasDriver(int pQuestId, ref Dictionary<int, Quest> pBbdmQuests, HashSet<int> pAdded, Dictionary<int, int> pQuestIdAndChildCount, Dictionary<int, int> pQuestIdAndNonfirstStarterCount, Dictionary<int, string> pQuestIdToNodeId, bool pIsStarter = false)
        {
            // Check drawn
            if (pAdded.Contains(pQuestId)) return;

            // Check deadend
            if (!pBbdmQuests.TryGetValue(pQuestId, out Quest? tQuest)
                || tQuest == null) return;
            //PluginLog.LogDebug($"> Canvas setup: qid={pQuestId} nextQs={tQuest.mNextQuestIds.Count} prevQs={tQuest.mPrevQuestIds.Count} ");

            // Add quest to canvas
            string? tResNodeId = null;
            if (this.mCanvas == null) this.mCanvas = new(-1, $"\"{this.mName}\" quest chain");
            if (pIsStarter && this.mCanvas.mGraph.VertexCount == 0)                                  // Standalone or FIRST chain starter
            {
                tResNodeId = this.mCanvas.AddNodeToAvailableCorner<AuxNode>(
                    new BBNodeContent(this.mPlugin, tQuest.GetGenId(), tQuest.mName),
                    pTag: $"{Utils.Utils.NodeTagPrefix.SYS}{tQuest.GetGenId()}"
                );
            }
            else if (pIsStarter && this.mCanvas.mGraph.VertexCount > 0)                              // Non-first starters (put this higher in terms of Y-axis)
            {
                int idToAttach = tQuest.mNextQuestIds.First();
                if (pQuestIdToNodeId.TryGetValue(idToAttach, out var nodeIdToAttach) && nodeIdToAttach != null)
                {
                    if (!pQuestIdAndNonfirstStarterCount.TryGetValue(idToAttach, out int starterCount))
                    {
                        starterCount = 0;
                        pQuestIdAndNonfirstStarterCount.TryAdd(idToAttach, starterCount);
                    }
                    tResNodeId = this.mCanvas.AddNodeAdjacent(
                        new(AuxNode.nodeType,
                            new BBNodeContent(this.mPlugin, tQuest.GetGenId(), tQuest.mName),
                            ofsToPrevNode: new Vector2(30, -100 * (starterCount + 1))),
                        nodeIdToAttach,
                        pTag: $"{Utils.Utils.NodeTagPrefix.SYS}{tQuest.GetGenId()}"
                    );
                    pQuestIdAndNonfirstStarterCount[idToAttach] = starterCount + 1;
                }
            }
            else if (tQuest.mPrevQuestIds.Count != 0)                                               // Mid-node / End-node
            {
                //PluginLog.LogDebug($"> (p={pQuestId}) ---> add_style 3");
                foreach (int idToAttach in tQuest.mPrevQuestIds)
                {
                    if (!pQuestIdToNodeId.TryGetValue(idToAttach, out var nodeIdToAttach) || nodeIdToAttach == null) continue;
                    //PluginLog.LogDebug($"> (p={pQuestId}) ---> attach_id={nodeIdToAttach}");
                    if (!pQuestIdAndChildCount.TryGetValue(idToAttach, out int childCount))
                    {
                        childCount = 0;
                        pQuestIdAndChildCount.TryAdd(idToAttach, childCount);
                    }
                    tResNodeId = this.mCanvas.AddNodeAdjacent(
                        new(AuxNode.nodeType,
                            new BBNodeContent(this.mPlugin, tQuest.GetGenId(), tQuest.mName),
                            ofsToPrevNode: new Vector2(70, 100 + 10 * childCount)),
                        nodeIdToAttach,
                        pTag: $"{Utils.Utils.NodeTagPrefix.SYS}{tQuest.GetGenId()}"
                    );
                    pQuestIdAndChildCount[idToAttach] = childCount + 1;
                    //PluginLog.LogDebug($"> (p={pQuestId}) ---> tResNodeId={tResNodeId}");
                }
            }
            // Add translation
            if (tResNodeId != null)
            {
                pQuestIdToNodeId.Add(pQuestId, tResNodeId);
            }

            // Check endings
            if (this.mQuestEnds.Contains(tQuest.mId)) return;
            // Recur
            pAdded.Add(pQuestId);
            foreach (var iChildId in tQuest.mNextQuestIds)
            {
                //PluginLog.LogDebug($"> (p={pQuestId}) ---> recur: cqid={iChildId} cExist={pBbdmQuests.ContainsKey(iChildId)}");
                this.SetUpCanvasDriver(iChildId, ref pBbdmQuests, pAdded, pQuestIdAndChildCount, pQuestIdAndNonfirstStarterCount, pQuestIdToNodeId);
            }
        }
    }
}
