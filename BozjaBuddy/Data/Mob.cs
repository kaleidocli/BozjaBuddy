using System.Data.SQLite;
using System;
using System.Collections.Generic;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class Mob : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Mob;
        public override int mId { get; set; }
        public override string mName { get; set; }
        public MobType mType { get; set; }
        public int mLevel { get; set; }
        public string mNote { get; set; } = string.Empty;
        private string mTerritoryType { get; set; }
        private double mMapCoordX { get; set; }
        private double mMapCoordY { get; set; }
        protected override Plugin mPlugin { get; set; }

        public Mob(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            this.mId = Convert.ToInt32(pPackage["id"]);
            this.mName = (string)pPackage["name"];
            this.mType = (MobType)Convert.ToInt32(pPackage["type"] is System.DBNull ? -1 : pPackage["type"]);
            this.mLevel = Convert.ToInt32(pPackage["level"] is System.DBNull ? -1 : pPackage["level"]);
            this.mNote = pPackage["note"] is System.DBNull ? string.Empty : (string)pPackage["note"];
            this.mTerritoryType = (string)pPackage["territoryType"];
            this.mMapCoordX = Convert.ToDouble(pPackage["mapCoordX"] is System.DBNull ? -1 : pPackage["mapCoordX"]);
            this.mMapCoordY = Convert.ToDouble(pPackage["mapCoordY"] is System.DBNull ? -1 : pPackage["mapCoordY"]);

            this.mLinkFragments = new List<int>();
            this.mLocation = new Location(this.mPlugin, this.mTerritoryType, this.mMapCoordX, this.mMapCoordY);

            this.mTabColor = UtilsGUI.Colors.GenObj_GreenMob;

            this.SetUpAuxiliary();
        }
        public override string GetReprClipboardTooltip()
        {
            return $"[{this.mName}] • [Type: {this.mType}] • [Lcoation: {WeatherBarSection._mTerritories[this.mTerritoryType]} x:{this.mMapCoordX} y:{this.mMapCoordY}] • [{this.mNote}]";
        }
        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"[{this.mType.ToString()}] • [Rank: {this.mLevel}]";
            this.mDescription = this.mNote;
            this.mIGMarkup = new GUI.IGMarkup.IGMarkup(this.mNote);
        }
        protected override string GenReprUiTooltip()
        {
            string tMobText = string.Join(
                "\n\t\t\t\t\t",
                this.mLinkMobs.Select(o => this.mPlugin.mBBDataManager.mMobs.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mMobs[o].mName + $" --- ({this.mPlugin.mBBDataManager.mMobs[o].mLocation?.ToString()} x:{this.mPlugin.mBBDataManager.mMobs[o].mLocation?.mMapCoordX} y:{this.mPlugin.mBBDataManager.mMobs[o].mLocation?.mMapCoordY})"
                                            : "unknown")
                                   .ToList()
                );
            string tFragDrops = string.Join(
                ", ",
                this.mLinkFragments.Select(o => this.mPlugin.mBBDataManager.mFragments.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFragments[o].mName
                                                + (this.mPlugin.mBBDataManager.mFragments[o].mLinkVendors.Count > 0
                                                    ? " (cluster)"
                                                    : "")
                                            : "unknown")
                                   .ToList()
                                    );

            this.mUiTooltip = $"Name:\t{this.mName}"
                            + $"\nFrags: \t{tFragDrops}"
                            + $"\nLoc.:\t\t{this.mLocation?.ToString()} x:{this.mLocation?.mMapCoordX} y:{this.mLocation?.mMapCoordY}";
            return this.mUiTooltip;
        }

        public enum MobType
        {
            None = 0,
            Normal = 61701,
            Legion = 61707,
            Sprite = 61712,
            Ashkin = 63939,
            Boss = 65011
        }
    }
}
