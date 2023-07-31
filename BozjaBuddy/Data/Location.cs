using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Data
{
    public class Location
    {
        protected Plugin mPlugin { get; set; }
        public static double[] BOZJA_Z3_BOX = { 0, 0, 26.4, 18.8 };             // TODO: Use actual Area and Subarea for this
        public static double[] BOZJA_Z3_BOX2 = { 0, 0, 13.3, 24.1 };            // TODO TODO: lmao gl remembering to do that
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
            this.mReprString = this.mPlugin.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(this.mTerritoryID)?.PlaceName?.Value?.Name ?? string.Empty;
            this.UpdateAreaFlag();
            this.UpdateStringRepr();
        }
        public Location(Plugin pPlugin, Level pLevel)
        {
            this.mPlugin = pPlugin;
            this.mTerritoryType = pLevel.Territory.Value != null
                                  ? pLevel.Territory.Value.Name
                                  : "";
            this.mTerritoryID = pLevel.Territory.Value != null
                                  ? pLevel.Territory.Value.RowId
                                  : 0;
            this.mMapID = pLevel.Territory.Value != null
                                  ? pLevel.Territory.Value.Map.Row
                                  : 0;
            var tPayload = new Dalamud.Game.Text.SeStringHandling.Payloads.MapLinkPayload(this.mTerritoryID, this.mMapID, (int)(pLevel.X * 1000), (int)(pLevel.Z * 1000));
            this.mMapCoordX = tPayload.XCoord;
            this.mMapCoordY = tPayload.YCoord;
            this.mReprString = string.Format(
                    "{0} ({1})",
                    pLevel.Territory.Value != null
                                  ? pLevel.Territory.Value.PlaceName.Value?.Name ?? ""
                                  : "",
                    pLevel.Territory.Value != null
                                  ? pLevel.Territory.Value.PlaceNameRegion.Value?.Name ?? ""
                                  : ""
                );
            this.UpdateAreaFlag();
            this.UpdateStringRepr();
        }
        /// <summary> X, Y not given, then use aetheryte's coords. If no aetheryte found, then coord is (20, 20) which is supposed to be the middle of the map. </summary>
        public Location(Plugin pPlugin, string pTerritoryType) : this(pPlugin, pTerritoryType, 20, 20)
        {
            var tAetheryteLevel = this.mPlugin.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(this.mTerritoryID)?.Aetheryte?.Value?.Level.FirstOrDefault()?.Value;
            if (tAetheryteLevel != null)
            {
                var tPayload = new Dalamud.Game.Text.SeStringHandling.Payloads.MapLinkPayload(this.mTerritoryID, this.mMapID, (int)(tAetheryteLevel.X * 1000), (int)(tAetheryteLevel.Z * 1000));
                this.mMapCoordX = tPayload.XCoord;
                this.mMapCoordY = tPayload.YCoord;
            }
        }

        public override string ToString() => this.mReprString;
        public string ToStringFull() => $"{this} x:{this.mMapCoordX:0.00} y:{this.mMapCoordY:0.00}";

        private void UpdateAreaFlag()
        {
            if (this.mTerritoryType == "n4b4")
            {
                if (this.CheckIsInsideBox(Location.BOZJA_Z3_BOX) || this.CheckIsInsideBox(Location.BOZJA_Z3_BOX2)) this.mAreaFlag = Area.Bozja_Zone3;
                else if (this.CheckIsInsideBox(Location.BOZJA_Z2_BOX)) this.mAreaFlag = Area.Bozja_Zone2;
                else this.mAreaFlag = Area.Bozja_Zone1;
            }
            else if (this.mTerritoryType == "n4b6")
            {
                if (this.CheckIsInsideBox(Location.ZADNOR_Z3_BOX)) this.mAreaFlag = Area.Zadnor_Zone3;
                else if (this.CheckIsInsideBox(Location.ZADNOR_Z2_BOX)) this.mAreaFlag = Area.Zadnor_Zone2;
                else this.mAreaFlag = Area.Zadnor_Zone1;
            }
        }
        private void UpdateStringRepr()
        {
            switch (this.mAreaFlag)
            {
                case Area.None: 
                    if (this.mReprString == string.Empty) this.mReprString = "None"; break;
                case Area.Bozja_Zone1: this.mReprString = "Bozja Zone 1"; break;
                case Area.Bozja_Zone2: this.mReprString = "Bozja Zone 2"; break;
                case Area.Bozja_Zone3: this.mReprString = "Bozja Zone 3"; break;
                case Area.Zadnor_Zone1: this.mReprString = "Zadnor Zone 1"; break;
                case Area.Zadnor_Zone2: this.mReprString = "Zadnor Zone 2"; break;
                case Area.Zadnor_Zone3: this.mReprString = "Zadnor Zone 3"; break;
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
