using BozjaBuddy.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dalamud.Game.Text.SeStringHandling;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class FieldNote : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.FieldNote;
        public override int mId { get; set; } = 0;
        public int mRarity { get; set; } = 0;
        public int mNumber { get; set; } = 0;
        public int mItemLinkId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        protected override Plugin mPlugin { get; set; }

        public FieldNote(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            mId = (int)(long)pPackage["id"];
            mName = (string)pPackage["name"];
            mRarity = (int)(long)pPackage["rarity"];
            mDescription = pPackage["description"] is System.DBNull ? string.Empty : (string)pPackage["description"];
            mItemLinkId = (int)(long)pPackage["itemLinkId"];

            var tSheet = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.MYCWarResultNotebook>();
            if (tSheet != null)
            {
                var tLuminaObj = tSheet.FirstOrDefault(o => o.RowId == mId);
                if (tLuminaObj != null) mNumber = (int)tLuminaObj.Number;
            }

            this.mTabColor = UtilsGUI.Colors.GenObj_BrownFieldNote;

            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public override SeString? GetReprItemLink() => SeString.CreateItemLink((uint)this.mItemLinkId, false);
        public override string GetReprClipboardTooltip()
        {
            string tFateText = string.Join(
                ", ",
                this.mLinkFates.Select(o => this.mPlugin.mBBDataManager.mFates.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFates[o].mName + $" --- ({this.mPlugin.mBBDataManager.mFates[o].mLocation?.ToStringFull()})"
                                            : "unknown")
                                   .ToList()
                );
            return $"[{this.mName}] • [Rarity: {this.mRarity}] • [{this.mDescription}] • [FATE: {tFateText}]";
        }
        protected override string GenReprUiTooltip()
        {
            string tFateText = string.Join(
                "\n\t\t\t    ",
                this.mLinkFates.Select(o => this.mPlugin.mBBDataManager.mFates.ContainsKey(o)
                                            ? this.mPlugin.mBBDataManager.mFates[o].mName + $" --- ({this.mPlugin.mBBDataManager.mFates[o].mLocation?.ToStringFull()})"
                                            : "unknown")
                                   .ToList()
                );

            this.mUiTooltip = $"Name:\t{this.mName}"
                            + $"\nDesc:  \t{this.mDescription}"
                            + $"\nFATE:  \t{tFateText}";
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = $"Rarity: {this.mRarity}";
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new()
            {
                { TextureCollection.StandardIcon.Rarity, this.mRarity.ToString() }
            };
        }
    }
}
