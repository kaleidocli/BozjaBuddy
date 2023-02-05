using System.Data.SQLite;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data
{
    public class Loadout : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Loadout;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        public string mGroup { get; set; } = String.Empty;
        public int mWeight { get; set; } = 0;
        public Dictionary<int, int> mActionIds { get; set; } = new Dictionary<int, int>();
        public RoleFlag mRole { get; set; } = new RoleFlag();
        public int _roleInt { get; set; } = 0;
        protected override Plugin mPlugin { get; set; }

        public Loadout(Plugin pPlugin, LoadoutJson pPackageJson)
        {
            this.mPlugin = pPlugin;
            this.mId = pPackageJson.mId;
            this.mName = pPackageJson.mName;
            this.mDescription = pPackageJson.mDescription;
            this.mGroup = pPackageJson.mGroup;
            this.mWeight = pPackageJson.mWeight;
            this.mActionIds = pPackageJson.mActionIds;
            this.mRole = new RoleFlag(pPackageJson.mRoleInt);

            this.SetUpAuxiliary();
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
        }
    }
    public class LoadoutJson
    {
        public int mId { get; set; }
        public string mName { get; set; }
        public string mDescription { get; set; }
        public string mGroup { get; set; }
        public int mWeight { get; set; }
        public Dictionary<int, int> mActionIds { get; set; }
        public int mRoleInt { get; set; }
    }
}
