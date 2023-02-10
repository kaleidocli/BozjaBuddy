using System.Data.SQLite;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data
{
    public class Loadout : GeneralObject
    {
        public static int IdAutoIncrement = 0;
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Loadout;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        public string mGroup { get; set; } = String.Empty;
        public int mWeight { get; set; } = 0;
        public Dictionary<int, int> mActionIds { get; set; } = new Dictionary<int, int>();
        public RoleFlag mRole { get; set; } = new RoleFlag();
        public int _roleInt { get; set; } = 0;
        protected override Plugin mPlugin { get; set; }

        public Loadout(Plugin pPlugin, LoadoutJson pPackageJson, bool pIsNew = false)
        {
            this.mPlugin = pPlugin;
            this.mId = pPackageJson.mId < 0 || pIsNew
                        ? Loadout.IdAutoIncrement + 1 
                        : pPackageJson.mId;
            if (this.mId > Loadout.IdAutoIncrement)
                Loadout.IdAutoIncrement = this.mId;
            this.mName = pPackageJson.mName;
            this.mDescription = pPackageJson.mDescription;
            this.mGroup = pPackageJson.mGroup;
            this.mWeight = pPackageJson.mWeight;
            this.mActionIds = new Dictionary<int, int>(pPackageJson.mActionIds);
            this.mRole = new RoleFlag(pPackageJson.mRoleInt);

            this.SetUpAuxiliary();
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
        }
    }
}
