using Dalamud.Game.ClientState.JobGauge.Types;
using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Drawing;
using BozjaBuddy.GUI.Sections;
using System.Text.Json;

namespace BozjaBuddy.Data
{
    public class BBDataManager
    {
        private Plugin mPlugin;
        private string mCsLostAction;
        public Dictionary<int, GeneralObject> mGeneralObjects;
        public Dictionary<int, Fragment> mFragments;
        public Dictionary<int, LostAction> mLostActions;
        public Dictionary<int, Fate> mFates;
        public Dictionary<int, Mob> mMobs;
        public Dictionary<int, Vendor> mVendors;
        public Dictionary<int, Loadout> mLoadouts;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Action>? mSheetAction;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? mSheetItem;

        public BBDataManager(Plugin pPlugin) 
        {
            this.mPlugin = pPlugin;
            this.mFragments = new Dictionary<int, Fragment>();
            this.mLostActions = new Dictionary<int, LostAction>();
            this.mFates = new Dictionary<int, Fate>();
            this.mMobs = new Dictionary<int, Mob>();
            this.mVendors = new Dictionary<int, Vendor>();
            this.mLoadouts = new Dictionary<int, Loadout>();
            this.mGeneralObjects = new Dictionary<int, GeneralObject>();

            // db
            this.mCsLostAction = String.Format("Data Source={0}", this.mPlugin.DATA_PATHS["db"]);
            using (SQLiteConnection mConnLostAction = new SQLiteConnection(this.mCsLostAction))
            {
                mConnLostAction.Open();
                SQLiteCommand tCommand = mConnLostAction.CreateCommand();

                this.DataSetUpFragment(tCommand);
                this.DataSetUpAction(tCommand);
                this.DataSetUpFate(tCommand);
                this.DataSetUpMob(tCommand);
                this.DataSetUpVendor(tCommand);
            }

            // json
            LoadoutListJson? tRawLoadouts = JsonSerializer.Deserialize<LoadoutListJson>(
                        File.ReadAllText(this.mPlugin.DATA_PATHS["loadout.json"])
                    );
            if (tRawLoadouts != null)
            {
                foreach (LoadoutJson iLoadout in tRawLoadouts.mLoadouts)
                {
                    this.mLoadouts[iLoadout.mId] = new Loadout(this.mPlugin, iLoadout);
                }
            }

            // lumina
            this.mSheetAction = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>();
            this.mSheetItem = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Item>();
            this.SetUpGeneralObjects();
        }

        private Dictionary<int, TDbObj> DbLoader<TDbObj> (out Dictionary<int, TDbObj> pDict, SQLiteCommand pCommand, string pQuery, Func<Plugin, SQLiteDataReader, TDbObj> tDel, string pKeyCollumn = "id")
        {
            pDict = new Dictionary<int, TDbObj>();
            pCommand.CommandText = pQuery;
            SQLiteDataReader tReader = pCommand.ExecuteReader();

            while (tReader.Read())
            {
                pDict.Add(
                    (int)(long)tReader[pKeyCollumn],
                    (TDbObj)tDel(this.mPlugin, tReader)
                );
            }
            tReader.Close();

            return pDict;
        }

        private void DataSetUpFragment(SQLiteCommand pCommand)
        {
            DbLoader<Fragment>(out this.mFragments, pCommand, "SELECT * FROM Fragment;", (p, t) => new Fragment(p, t));
            foreach (int id in this.mFragments.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "idAction";
                pCommand.CommandText = $@"SELECT FragmentToAction.{iLinkCol} 
                                            FROM FragmentToAction 
                                            WHERE FragmentToAction.id = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkActions.Add((int)(long)tReader[iLinkCol]);
                }
                tReader.Close();

                // Linking idFate
                iLinkCol = "idFate";
                pCommand.CommandText = $@"SELECT DISTINCT FragmentToFate.{iLinkCol}
                                            FROM FragmentToFate
                                            WHERE FragmentToFate.id = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkFates.Add((int)(long)tReader[iLinkCol]);
                }
                tReader.Close();

                // Linking idMob
                iLinkCol = "idMob";
                pCommand.CommandText = $@"SELECT DISTINCT FragmentToMob.{iLinkCol}
                                            FROM FragmentToMob
                                            WHERE FragmentToMob.id = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkMobs.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
                // Linking idVendor
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT VendorToItem.{iLinkCol}
                                            FROM VendorToItem
                                            WHERE VendorToItem.idItem = {id};";
                tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFragments[id].mLinkVendors.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpAction(SQLiteCommand pCommand)
        {
            DbLoader<LostAction>(out this.mLostActions, pCommand, "SELECT * FROM LostAction;", (p, t) => new LostAction(p, t));

            foreach (int id in this.mLostActions.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FragmentToAction.{iLinkCol} 
                                            FROM FragmentToAction 
                                            WHERE FragmentToAction.idAction = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mLostActions[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpFate(SQLiteCommand pCommand)
        {
            DbLoader<Fate>(out this.mFates, pCommand, "SELECT * FROM Fate;", (p, t) => new Fate(p, t));

            foreach (int id in this.mFates.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FragmentToFate.{iLinkCol} 
                                            FROM FragmentToFate 
                                            WHERE FragmentToFate.idFate = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFates[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpMob(SQLiteCommand pCommand)
        {
            DbLoader<Mob>(out this.mMobs, pCommand, "SELECT * FROM Mob;", (p, t) => new Mob(p, t));

            foreach (int id in this.mMobs.Keys)
            {
                string iLinkCol;

                // Linking idAction
                iLinkCol = "id";
                pCommand.CommandText = $@"SELECT FragmentToMob.{iLinkCol} 
                                            FROM FragmentToMob
                                            WHERE FragmentToMob.idMob = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mMobs[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                }

                tReader.Close();
            }
        }
        private void DataSetUpVendor(SQLiteCommand pCommand)
        {
            DbLoader<Vendor>(out this.mVendors, pCommand, "SELECT * FROM Vendor;", (p, t) => new Vendor(p, t));
            foreach (int id in this.mVendors.Keys)
            {
                string iLinkCol;

                // Linking idFragment
                iLinkCol = "idItem";
                pCommand.CommandText = $@"SELECT VendorToItem.{iLinkCol}, VendorToItem.idItem, VendorToItem.amount, VendorToItem.price, VendorToItem.idCurrencyItem
                                            FROM VendorToItem
                                            WHERE VendorToItem.id = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mVendors[id].mLinkFragments.Add((int)(long)tReader[iLinkCol]);
                    this.mVendors[id].AddItemToStock((int)(long)tReader["idItem"], (int)(long)tReader["amount"], (int)(long)tReader["price"], (int)(long)tReader["idCurrencyItem"]);
                }

                tReader.Close();
            }
        }

        private void SetUpGeneralObjects()
        {
            foreach (int id in this.mFragments.Keys)
                this.mGeneralObjects[this.mFragments[id].GetGenId()] = this.mFragments[id];
            foreach (int id in this.mMobs.Keys)
                this.mGeneralObjects[this.mMobs[id].GetGenId()] = this.mMobs[id];
            foreach (int id in this.mFates.Keys)
                this.mGeneralObjects[this.mFates[id].GetGenId()] = this.mFates[id];
            foreach (int id in this.mLostActions.Keys)
                this.mGeneralObjects[this.mLostActions[id].GetGenId()] = this.mLostActions[id];
            foreach (int id in this.mVendors.Keys)
                this.mGeneralObjects[this.mVendors[id].GetGenId()] = this.mVendors[id];
            foreach (int id in this.mLoadouts.Keys)
                this.mGeneralObjects[this.mLoadouts[id].GetGenId()] = this.mLoadouts[id];
        }

        public void SetUpAuxiliary()
        {
            foreach (int id in this.mFragments.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mFragments[id].GetGenId());
            foreach (int id in this.mMobs.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mMobs[id].GetGenId());
            foreach (int id in this.mFates.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mFates[id].GetGenId());
            foreach (int id in this.mLostActions.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mLostActions[id].GetGenId());
            foreach (int id in this.mVendors.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mVendors[id].GetGenId());
            foreach (int id in this.mLoadouts.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mLoadouts[id].GetGenId());
        }
    }

    public enum LostActionType
    {
        None,
        Offensive,
        Defensive,
        Restorative,
        Beneficial,
        Tactical,
        Detrimental,
        Item
    }
    public class LoadoutListJson
    {
        public List<LoadoutJson> mLoadouts { get; set; }
    }
}
