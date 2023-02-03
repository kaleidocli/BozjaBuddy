using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Data
{
    public class Location
    {
        protected Plugin mPlugin { get; set; }
        public static double[] BOZJA_Z3_BOX = { 0, 0, 26.4, 18.8 };             // TODO: Use actual Area and Subarea for this
        public static double[] BOZJA_Z2_BOX = { 13.0, 14.6, 33.2, 25.3 };
        public static double[] BOZJA_Z1_BOX = { 13.1, 23.1, 37.3, 32.2 };
        public static double[] ZADNOR_Z3_BOX = { 0, 0, 27.7, 18.1 };
        public static double[] ZADNOR_Z2_BOX = { 2.4, 16.9, 15.2, 39.2 };
        public static double[] ZADNOR_Z1_BOX = { 15.2, 27.6, 34.7, 40.6 };

        private string mReprString = string.Empty;

        public Area mAreaFlag { get; set; } = Area.None;
        public double mMapCoordX { get; set; }
        public double mMapCoordY { get; set; }
        public string mTerritoryType { get; set; }
        public uint mTerritoryID { get; set; }
        public uint mMapID { get; set; }

        public Location(Plugin pPlugin, string tTerritoryType, double x, double y)
        {
            this.mPlugin = pPlugin;
            this.mMapCoordX = x;
            this.mMapCoordY = y;
            this.mTerritoryType = tTerritoryType;

            List<uint> tTemp = this.mPlugin.DataManager.GetExcelSheet<TerritoryType>()!
                            .Where(t => t.Name.ToDalamudString().TextValue == this.mTerritoryType)
                            .Select(t => t.RowId).ToList();
            this.mTerritoryID = tTemp.Count == 0 ? 0 : tTemp[0];
            tTemp = this.mPlugin.DataManager.GetExcelSheet<Map>()!
                            .Where(t => (t.TerritoryType.Value?.ToString() ?? "")  == this.mTerritoryType)
                            .Select(t => t.RowId).ToList();
            uint? tTemp2 = this.mPlugin.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(this.mTerritoryID)?.Map.Row;
            this.mMapID = tTemp2 ?? 0;
            this.UpdateAreaFlag();
            this.UpdateStringRepr();
        }

        public override string ToString() => this.mReprString;

        private void UpdateAreaFlag()
        {
            if (this.mTerritoryType == "n4b4")
            {
                if (this.CheckIsInsideBox(Location.BOZJA_Z3_BOX)) this.mAreaFlag = Area.Bozja_Zone3;
                else if (this.CheckIsInsideBox(Location.BOZJA_Z2_BOX)) this.mAreaFlag = Area.Bozja_Zone2;
                else this.mAreaFlag = Area.Bozja_Zone1;
            }
            else if (this.mTerritoryType == "n4b6")
            {
                if (this.CheckIsInsideBox(Location.ZADNOR_Z3_BOX)) this.mAreaFlag = Area.Zadnor_Zone3;
                else if (this.CheckIsInsideBox(Location.BOZJA_Z2_BOX)) this.mAreaFlag = Area.Zadnor_Zone2;
                else this.mAreaFlag = Area.Zadnor_Zone1;
            }
        }
        private void UpdateStringRepr()
        {
            switch (this.mAreaFlag)
            {
                case Area.None: this.mReprString = "None"; break;
                case Area.Bozja_Zone1: this.mReprString = "Bozja Z1"; break;
                case Area.Bozja_Zone2: this.mReprString = "Bozja Z2"; break;
                case Area.Bozja_Zone3: this.mReprString = "Bozja Z3"; break;
                case Area.Zadnor_Zone1: this.mReprString = "Zadnor Z1"; break;
                case Area.Zadnor_Zone2: this.mReprString = "Zadnor Z2"; break;
                case Area.Zadnor_Zone3: this.mReprString = "Zadnor Z3"; break;
                case Area.Castrum: this.mReprString = "Castrum Lacus Litore"; break;
                case Area.Dalriada: this.mReprString = "The Dalriada"; break;
                case Area.Delubrum: this.mReprString = "Delubrum Reginae"; break;
                case Area.DelubrumSavage: this.mReprString = "Delubrum Reginae (Savage)"; break;
            }
        }
        private bool CheckIsInsideBox(double x, double y, double[] tBox)
            => x >= tBox[0] && y >= tBox[1] && x <= tBox[2] && y <= tBox[3];
        private bool CheckIsInsideBox(double[] tBox)
            => this.CheckIsInsideBox(this.mMapCoordX, this.mMapCoordY, tBox);
        public enum Area : short
        {
            None = 0,
            Bozja_Zone1 = 1,
            Bozja_Zone2 = 2,
            Bozja_Zone3 = 3,
            Zadnor_Zone1 = 4,
            Zadnor_Zone2 = 5,
            Zadnor_Zone3 = 6,
            Castrum = 7,
            Dalriada = 8,
            Delubrum = 9,
            DelubrumSavage = 10
        }
    }
}
