using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class Loadout : GeneralObject
    {
        private LoadoutJson mPackageJson;
        public static int IdAutoIncrement = 0;
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Loadout;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        public string mGroup { get; set; } = String.Empty;
        public int mWeight { get; set; } = 0;
        public Dictionary<int, int> mActionIds { get; set; } = new Dictionary<int, int>();
        public RoleFlag mRole { get; set; } = new RoleFlag();
        protected override Plugin mPlugin { get; set; }

        public Loadout(Plugin pPlugin, LoadoutJson pPackageJson, bool pIsNew = false)
        {
            this.mPlugin = pPlugin;
            this.mId = pPackageJson.mId < 0 || pIsNew
                        ? Loadout.IdAutoIncrement + 1 
                        : pPackageJson.mId;
            if (this.mId > Loadout.IdAutoIncrement && this.mId < 10000)
                Loadout.IdAutoIncrement = this.mId;
            this.mName = pPackageJson.mName;
            this.mDescription = pPackageJson.mDescription;
            this.mGroup = pPackageJson.mGroup;
            this.mWeight = pPackageJson.mWeight;
            this.mActionIds = new Dictionary<int, int>(pPackageJson.mActionIds);
            this.mRole = new RoleFlag(pPackageJson.mRoleInt);

            this.mPackageJson = pPackageJson;
            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public Loadout DeepCopy()
        {
            return new Loadout(this.mPlugin, this.mPackageJson);
        }
        public override string GetReprClipboardTooltip()
        {
            string tLoadoutText = "";
            foreach (int iActionid in this.mActionIds.Keys)
            {
                tLoadoutText += $"[{this.mPlugin.mBBDataManager.mLostActions[iActionid].mName} x{this.mActionIds[iActionid]}]";
            }
            return $"[{this.mName}] • [Group: {this.mGroup}] • [Role: {this.mRole}] • {tLoadoutText}";
        }
        protected override string GenReprUiTooltip()
        {
            List<string> tActionTexts = new();
            foreach (int iActionid in this.mActionIds.Keys)
            {
                tActionTexts.Add($"x{this.mActionIds[iActionid]}\t{this.mPlugin.mBBDataManager.mLostActions[iActionid].mName}");
            }
            string tActionText = string.Join(
                "\n",
                tActionTexts
                );

            this.mUiTooltip = $"Name:\t{this.mName}\n"
                            + $"Role:   \t{this.mRole}\n"
                            + $"Group:    {this.mGroup}\n"
                            + $"\n{tActionText}";
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "none";
        }
        protected override void SetUpNodeInfo()
        {

        }
    }
}
