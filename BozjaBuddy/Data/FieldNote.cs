using BozjaBuddy.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dalamud.Game.Text.SeStringHandling;

namespace BozjaBuddy.Data
{
    public class FieldNote : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.FieldNote;
        public override int mId { get; set; } = 0;
        public int mRarity { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        protected override Plugin mPlugin { get; set; }

        public FieldNote(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            mId = (int)(long)pPackage["id"];
            mName = (string)pPackage["name"];
            mRarity = (int)(long)pPackage["rarity"];
            mDescription = pPackage["description"] is System.DBNull ? string.Empty : (string)pPackage["description"];

            this.mTabColor = UtilsGUI.Colors.GenObj_YellowFragment;

            this.SetUpAuxiliary();
        }
        public override SeString? GetReprItemLink() => null;// SeString.CreateItemLink((uint)mId, false);
        public override string GetReprClipboardTooltip()
            => $"[{this.mName}] • [Rarity: {this.mRarity}] • [{this.mDescription}]";

        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"Rarity: {this.mRarity}";
        }
    }
}
