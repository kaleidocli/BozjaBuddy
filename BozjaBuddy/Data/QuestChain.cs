using Dalamud.Logging;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static void LinkQuestsToChain(int pQuestChainId, ref Dictionary<int, Quest> pBbdmQuests, ref Dictionary<int, QuestChain> pBbdmChains)
        {
            if (pBbdmChains.TryGetValue(pQuestChainId, out QuestChain? pChain)
                || pChain == null) return;
            foreach (var iStart in pChain.mQuestStarts)
            {
                QuestChain.LinkQuestsToChainDriver(pChain, iStart, ref pBbdmQuests, ref pBbdmChains);
            }
        }
        private static void LinkQuestsToChainDriver(QuestChain pQuestChain, int pQuestId, ref Dictionary<int, Quest> pBbdmQuests, ref Dictionary<int, QuestChain> pBbdmChains)
        {
            // Check deadend
            if (pBbdmQuests.TryGetValue(pQuestId, out Quest? pQuest)
                || pQuest == null) return;
            // Add
            pQuestChain.mQuests.Add(pQuest.mId);
            pBbdmQuests[pQuestId].mQuestChains.Add(pQuestChain.mId);
            PluginLog.LogDebug($"> c={pQuestChain.mName} q={pQuest.mName}");
            // Check endings
            if (pQuestChain.mQuestEnds.Contains(pQuest.mId)) return;
            // Recur
            foreach (var iChildId in pQuest.mNextQuestIds)
            {
                QuestChain.LinkQuestsToChainDriver(pQuestChain, iChildId, ref pBbdmQuests, ref pBbdmChains);
            }
        }
    }
}
