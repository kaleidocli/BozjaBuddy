using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BozjaBuddy.Data.Mob;

namespace BozjaBuddy.Data
{
    public class Quest : GeneralObject
    {

        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Quest;
        public override int mId { get; set; } = -1;
        public override string mName { get; set; } = "";
        public override string mDescription { get; set; } = "";
        public Quest.QuestType mType { get; set; } = Quest.QuestType.None;
        public string mIssuerName { get; set; } = string.Empty;
        public Location? mIssuerLocation { get; set; } = null;
        public string mQuestChainName = string.Empty;
        public HashSet<int> mPrevQuestIds { get; set; } = new();
        public HashSet<int> mNextQuestIds { get; set; } = new();
        public HashSet<int> mQuestChains { get; set; } = new();
        public Lumina.Excel.GeneratedSheets.Quest mLumina { get; set; } = new();

        protected override Plugin mPlugin { get; set; }

        /// <summary>
        /// A linker tuple (obj, linkPrev)
        /// </summary>
        public Quest(Plugin pPlugin, Lumina.Excel.GeneratedSheets.Quest pQuest, ref HashSet<Tuple<int, int>>? pLinkers)
        {
            this.mPlugin = pPlugin;
            this.mLumina = pQuest;

            // Lumina
            this.SetUpLumina(ref pLinkers);

            this.mTabColor = UtilsGUI.Colors.GenObj_GreenMob;
            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public void SetUpDb(SQLiteDataReader? pPackage)
        {
            if (pPackage != null)
            {
                if (pPackage["name"] != null && (string)pPackage["name"] != "")
                    this.mName = (string)pPackage["name"];
                this.mDescription = (string)pPackage["description"];
            }
        }
        public void SetUpLumina(ref HashSet<Tuple<int, int>>? pLinkers)
        {
            this.mId = (int)this.mLumina.RowId;
            this.mName = this.mLumina.Name;
            this.mType = this.mLumina.EventIconType.Value != null
                         ? (QuestType)this.mLumina.EventIconType.Value.RowId
                         : QuestType.None;
            this.mType = this.mLumina.IsRepeatable ? QuestType.Repeatable : this.mType;
            var tNpc = this.mPlugin.DataManager.Excel.GetSheet<ENpcResident>()?.GetRow(this.mLumina.IssuerStart);
            this.mIssuerName = tNpc == null ? "unknown" : tNpc.Singular;
            if (this.mLumina.IssuerLocation.Value != null)
            {
                this.mIssuerLocation = new(this.mPlugin, this.mLumina.IssuerLocation.Value);
            }

            // quest chain set up (part 1 of 2)
            this.mPrevQuestIds = this.mLumina.PreviousQuest.Where(i => i.Value != null)
                                                    .Select(o => (int)o.Value!.RowId)
                                                    .ToHashSet();
            // add to linkers
            this.AddToLinkers(ref pLinkers);
        }
        private void AddToLinkers(ref HashSet<Tuple<int, int>>? pLinkers)
        {
            // Add links to the Linkers
            if (pLinkers != null)
            {
                foreach (var iPrevId in this.mPrevQuestIds)
                {
                    pLinkers.Add(new(this.mId, iPrevId));
                }
            }
        }
        public override string GetReprClipboardTooltip()
        {
            return "";
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new()
            {
                { TextureCollection.StandardIcon.None, $"NPC: {this.mIssuerName}" }
            };
        }
        protected override void SetUpAuxiliary()
        {
            this.mDetail = "";
            this.mDescription = "This is Thancred.";
            this.mIGMarkup = new GUI.IGMarkup.IGMarkup("");
        }
        protected override string GenReprUiTooltip()
        {
            return this.mUiTooltip;
        }

        // quest chain set up (part 2 of 2)
        // A linker tuple (obj, linkPrev)
        // pLinkers is a collection of a pair of Quest and its prev quest.
        // LinkQuest() will match if pQuest is a prev quest of any Quest (called A).
        // If true, A will be added as pQuest's next quest.
        public static void LinkNextQuest(Quest pQuest, ref HashSet<Tuple<int, int>>? pLinkers)
        {
            if (pLinkers == null) return;
            HashSet<Tuple<int, int>> tNewLinkers = new();
            foreach (var iLinker in pLinkers)
            {
                if (iLinker.Item2 == pQuest.mId)
                {
                    pQuest.mNextQuestIds.Add(iLinker.Item1);
                }
                else
                {
                    tNewLinkers.Add(iLinker);
                }
            }
            pLinkers = tNewLinkers;
        }

        public enum QuestType       // EventIconType# // side and repeatable quests are same iconType, but diff in isRepeatable
        {                           // unreliable. pls refer to: https://discord.com/channels/581875019861328007/653504487352303619/1114317613359714354
            None = 0,
            Msq = 3,
            Side = 1,
            Key = 8,
            Repeatable = 100
        }
    }
}
