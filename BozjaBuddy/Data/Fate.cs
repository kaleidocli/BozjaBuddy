using System.Data.SQLite;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Logging;

namespace BozjaBuddy.Data
{
    public class Fate : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Fate;
        public override int mId { get; set; }
        public override string mName { get; set; }
        public FateType mType { get; set; }
        public string mNote { get; set; } = string.Empty;
        public int mRewardMettleMin { get; set; }
        public int mRewardMettleMax { get; set; }
        public int mRewardExpMin { get; set; }
        public int mRewardExpMax { get; set; }
        public int mRewardTome { get; set; }
        public int mChainFatePrev { get; set; }
        public int mChainFateNext { get; set; }
        private string mTerritoryType { get; set; } = string.Empty;
        private double mMapCoordX { get; set; }
        private double mMapCoordY { get; set; }
        private Dalamud.Game.ClientState.Fates.Fate? _mCSFate = null;
        public Dalamud.Game.ClientState.Fates.Fate? mCSFate
        {
            get
            {
                if (this._mCSFate is not null && this._mCSFate.IsValid() && (this._mCSFate.State == FateState.Running || this._mCSFate.State == FateState.Preparation))
                    return this._mCSFate;
                return null;
            }
            set { this._mCSFate = value; }
        }
        protected override Plugin mPlugin { get; set; }

        public Fate(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            this.mId = Convert.ToInt32(pPackage["id"]);
            this.mName = (string)pPackage["name"];
            this.mType = (FateType)Convert.ToInt32(pPackage["type"] is System.DBNull ? -1 : pPackage["type"]);
            this.mNote = pPackage["note"] is System.DBNull ? string.Empty : (string)pPackage["note"];
            this.mRewardMettleMin = Convert.ToInt32(pPackage["rewardMettleMin"] is System.DBNull ? -1 : pPackage["rewardExpMin"]);
            this.mRewardMettleMax = Convert.ToInt32(pPackage["rewardMettleMax"] is System.DBNull ? -1 : pPackage["rewardExpMax"]);
            this.mRewardExpMin = Convert.ToInt32(pPackage["rewardExpMin"] is System.DBNull ? -1 : pPackage["rewardExpMin"]);
            this.mRewardExpMax = Convert.ToInt32(pPackage["rewardExpMax"] is System.DBNull ? -1 : pPackage["rewardExpMax"]);
            this.mRewardTome = Convert.ToInt32(pPackage["rewardTome"] is System.DBNull ? -1 : pPackage["rewardTome"]);
            this.mChainFatePrev = Convert.ToInt32(
                                    pPackage["chainFateIdPrev"] is System.DBNull || Convert.ToInt32(pPackage["chainFateIdPrev"]) == 0
                                    ? -1 : pPackage["chainFateIdPrev"]);
            this.mChainFateNext = Convert.ToInt32(
                                    pPackage["chainFateIdNext"] is System.DBNull || Convert.ToInt32(pPackage["chainFateIdNext"]) == 0
                                    ? -1 : pPackage["chainFateIdNext"]);
            this.mTerritoryType = (string)pPackage["territoryType"];
            this.mMapCoordX = Convert.ToDouble(pPackage["mapCoordX"] is System.DBNull ? -1 : pPackage["mapCoordX"]);
            this.mMapCoordY = Convert.ToDouble(pPackage["mapCoordY"] is System.DBNull ? -1 : pPackage["mapCoordY"]);

            this.mLinkFragments = new List<int>();
            this.mLocation = new Location(this.mPlugin, this.mTerritoryType, this.mMapCoordX, this.mMapCoordY);
            this.mCSFate = null;
            this.mTabColor = new System.Numerics.Vector4(0.9f, 0.61f, 0.9f, 0.2f);

            this.SetUpAuxiliary();
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"[Tome: {this.mRewardTome}] \t•\t[Mettle: {this.mRewardMettleMin} - {this.mRewardMettleMax}]\t•\t[Exp: {this.mRewardExpMin} - {this.mRewardExpMax}]";
            this.mDescription = this.mNote;
            this.mIGMarkup = new GUI.IGMarkup.IGMarkup(this.mNote);
        }

        public enum FateType
        {
            None = 0,
            Fate = 63914,
            CriticalEngagement = 63909,
            Duel = 63910,
            Raid = 63912
        }
    }
}
