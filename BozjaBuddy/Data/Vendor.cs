using System;
using System.Data.SQLite;
using System.Collections.Generic;

namespace BozjaBuddy.Data
{
    public class Vendor : GeneralObject
    {
        protected override GeneralObject.GeneralObjectSalt mIdSalt { get; set; } = GeneralObjectSalt.Vendor;
        protected override Plugin mPlugin { get; set;}
        private Dictionary<int, (int, int, Currency)> mStock = new Dictionary<int, (int, int, Currency)>();
        public override int mId { get; set; }
        public string mNote { get; set; }
        private string mTerritoryType { get; set; }
        private double mMapCoordX { get; set; }
        private double mMapCoordY { get; set; }

        public Vendor(Plugin pPlugin, SQLiteDataReader pPackage)
        {
            this.mPlugin = pPlugin;
            this.mStock = new Dictionary<int, (int, int, Currency)>();

            this.mId = Convert.ToInt32(pPackage["id"]);
            this.mName = (string)pPackage["name"];
            this.mNote = pPackage["note"] is System.DBNull ? string.Empty : (string)pPackage["note"];
            this.mTerritoryType = (string)pPackage["territoryType"];
            this.mMapCoordX = Convert.ToDouble(pPackage["mapCoordX"] is System.DBNull ? -1 : pPackage["mapCoordX"]);
            this.mMapCoordY = Convert.ToDouble(pPackage["mapCoordY"] is System.DBNull ? -1 : pPackage["mapCoordY"]);

            this.mLocation = new Location(this.mPlugin, this.mTerritoryType, this.mMapCoordX, this.mMapCoordY);
        }

        public void AddItemToStock(int pItemId, int pAmount, int pPrice, int pCurrencyItemId) 
            => this.mStock[pItemId] = (pAmount, pPrice, (Enum.IsDefined<Currency>((Currency)pCurrencyItemId) ? (Currency)pCurrencyItemId : Currency.Cluster));

        public (int, int, Currency) GetAmountPriceCurrency(int pItemId) => this.mStock[pItemId];
        public int GetStockItemCount() => this.mStock.Count;

        protected override void SetUpAuxiliary()
        {
            this.mDetail = "Detail";
            this.mDescription = "Description";
        }
        protected override void SetUpNodeInfo()
        {

        }

        public enum Currency
        {
            None = 0,
            Gil = 1,
            Cluster = 31135
        }
    }
}
