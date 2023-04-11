using System.Data.SQLite;
using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;

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

            this.mTabColor = new System.Numerics.Vector4(0.89f, 0.92f, 0.61f, 0.4f);

            this.SetUpAuxiliary();
        }
        public override SeString? GetReprItemLink() => SeString.CreateItemLink((uint)mId, false);
        public override string GetReprSynopsis() 
            => $"[{this.mName}] • [Tradable: {(this.mIsClusterBuyable ? "yes" : "no")}]";

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
            this.mDescription = "Description";
        }
    }
}
