using Lumina.Excel.GeneratedSheets;
using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            this.mTabColor = new System.Numerics.Vector4(0.89f, 0.92f, 0.61f, 0.2f);

            this.SetUpAuxiliary();
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
            this.mDescription = "Description";
        }
    }
}
