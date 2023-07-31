using BozjaBuddy.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using static BozjaBuddy.Data.GeneralObject;

namespace BozjaBuddy.Data
{
    public class Item : GeneralObject
    {
        protected override GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Item;
        public override int mId { get; set; } = 0;
        public override string mName { get; set; } = String.Empty;
        protected override Plugin mPlugin { get; set; }

        /// <summary> Represents a Lumina row in Lumina Item sheet (excl. Fragment) </summary>
        public Item(Plugin pPlugin, Lumina.Excel.GeneratedSheets.Item pLuminaItem)
        {
            this.mPlugin = pPlugin;
            this.mId = Convert.ToInt32(pLuminaItem.RowId);
            this.mName = pLuminaItem.Name;

            this.mTabColor = UtilsGUI.Colors.GenObj_YellowFragment;

            this.SetUpAuxiliary();
            this.SetUpNodeInfo();
        }
        public override SeString? GetReprItemLink() => SeString.CreateItemLink((uint)mId, false);
        public override string GetReprClipboardTooltip()
            => $"[{this.mName}]";
        protected override string GenReprUiTooltip()
        {
            return this.mUiTooltip;
        }

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
            this.mDescription = "This is Thancred.";
        }
        protected override void SetUpNodeInfo()
        {
            this.mDetailPackage = new();
        }
    }
}
