using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class LostAction : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.LostAction;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        public RoleFlag mRole { get; set; } = new RoleFlag();
        public string mDescription_semi { get; set; } = String.Empty;
        public string mUsage { get; set; } = String.Empty;
        public string mDescription_full { get; set; } = String.Empty;
        public int mWeight { get; set; } = 0;
        public LostActionType mType { get; set; } = LostActionType.Item;
        public double mCast { get; set; } = 0;
        public double mRecast { get; set; } = 0;
        public int mCharges { get; set; } = 0;
        protected override Plugin mPlugin { get; set; }

        public LostAction(Plugin pPlugin, SQLiteDataReader pPackage) {
            this.mPlugin = pPlugin;
            mId = Convert.ToInt32(pPackage["id"]);
            mName = (string)pPackage["name"];
            mRole = new RoleFlag(Convert.ToInt32(pPackage["role"]));
            mDescription_semi = (string)pPackage["effectSemi"];
            mUsage = (string)pPackage["usage"];
            mDescription_full = (string)pPackage["effectFull"];
            mLinkFragments = new List<int>(); //pPackage["fragment"];
            mWeight = Convert.ToInt32(pPackage["weight"] is System.DBNull ? (long)-1 : pPackage["weight"]);
            mType = (LostActionType)(int)(long)(pPackage["type"] is System.DBNull ? (long)6 : pPackage["type"]);
            mCast = Convert.ToDouble(pPackage["cast"] is System.DBNull ? -1 : pPackage["cast"]);
            mRecast = Convert.ToDouble(pPackage["cooldown"] is System.DBNull ? -1 : pPackage["cooldown"]);
            mCharges = Convert.ToInt32(pPackage["charges"] is System.DBNull ? -1 : pPackage["charges"]);

            this.mTabColor = new System.Numerics.Vector4(0.61f, 0.79f, 0.92f, 0.4f);

            this.SetUpAuxiliary();
        }

        public override string GetReprSynopsis()
            => $"[{this.mName}] • [Role: {this.mRole}] • [Charges: {this.mCharges}] • [Weight: {this.mWeight}] • [Cast/Recast: {this.mCast}s/{this.mRecast}s] • [{this.mDescription_semi.Replace("\n", ". ")}] • [Frags: {String.Join(", ", this.mLinkFragments.Select(id => this.mPlugin.mBBDataManager.mFragments[id].mName))}]";
        
        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"[{this.mCharges}/{this.mCharges}]\t•\t[Weight: {this.mWeight}]\t•\t[Cast: {this.mCast}s]\t•\t[Recast: {this.mRecast}]";
            this.mDescription = $"{this.mDescription_full}";
            this.mIGMarkup = new GUI.IGMarkup.IGMarkup(this.mDescription_full);
        }
    }
}
