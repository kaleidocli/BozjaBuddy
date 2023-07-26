using System.Data.SQLite;
using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using BozjaBuddy.Utils;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class Fragment : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Fragment;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        public bool mIsClusterBuyable = false;
        protected override Plugin mPlugin { get; set; }

        public Fragment(Plugin pPlugin, SQLiteDataReader pPackage) { 
            this.mPlugin = pPlugin;
            mId = (int)(long)pPackage["id"];
            mName = (string)pPackage["name"];
            mIsClusterBuyable = (int)(long)pPackage["isClusterBuyable"] == 1 ? true : false;

            this.mTabColor = UtilsGUI.Colors.GenObj_YellowFragment;

            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public override SeString? GetReprItemLink() => SeString.CreateItemLink((uint)mId, false);
        public override string GetReprClipboardTooltip() 
            => $"[{this.mName}] • [Tradable: {(this.mIsClusterBuyable ? "yes" : "no")}]";
        protected override string GenReprUiTooltip()
        {
            string tActionText = string.Join(
                "\n\t\t\t\t\t\t\t",
                this.mLinkActions.Select(o => this.mPlugin.mBBDataManager.mLostActions.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mLostActions[o].mName
                                            : "unknown")
                                    .ToList()
                );
            string tFateText = string.Join(
                "\n\t\t\t\t\t\t\t",
                this.mLinkFates.Select(o => this.mPlugin.mBBDataManager.mFates.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFates[o].mName + $" --- ({this.mPlugin.mBBDataManager.mFates[o].mLocation?.ToStringFull()})"
                                            : "unknown")
                                   .ToList()
                );
            string tMobText = string.Join(
                "\n\t\t\t\t\t\t\t",
                this.mLinkMobs.Select(o => this.mPlugin.mBBDataManager.mMobs.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mMobs[o].mName + $" --- ({this.mPlugin.mBBDataManager.mMobs[o].mLocation?.ToString()} x:{this.mPlugin.mBBDataManager.mMobs[o].mLocation?.mMapCoordX} y:{this.mPlugin.mBBDataManager.mMobs[o].mLocation?.mMapCoordY})"
                                            : "unknown")
                                   .ToList()
                );

            this.mUiTooltip = $"Name:\t\t\t   {this.mName}"
                            + $"\nCluster:\t\t\t {(this.mLinkVendors.Count > 0 ? "Yes" : "No")}"
                            + $"\nLost Actions:\t{tActionText}"
                            + $"\nFATE:  \t\t\t\t{tFateText}"
                            + $"\nMobs: \t\t\t\t{tMobText}";
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
            this.mDescription = "This is Thancred.";
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new()
            {
                { TextureCollection.StandardIcon.Cluster, this.mIsClusterBuyable ? "Yes" : "No" }
            };
        }
    }
}
