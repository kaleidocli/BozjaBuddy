using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using BozjaBuddy.GUI.Sections;
using System.Text.Json;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.GeneratedSheets;
using BozjaBuddy.Data.Alarm;
using BozjaBuddy.Utils;
using System.Runtime.CompilerServices;

namespace BozjaBuddy.Data
{
    public class BBDataManager
    {
        private const int kFateTableUpdateInterval = 1;
        private static DateTime kFateTableLastUpdate = DateTime.MinValue;

        private Plugin mPlugin;
        private string mCsLostAction;
        public Dictionary<int, GeneralObject> mGeneralObjects;
        public Dictionary<int, Fragment> mFragments;
        public Dictionary<int, LostAction> mLostActions;
        public Dictionary<int, Fate> mFates;
        public Dictionary<int, Mob> mMobs;
        public Dictionary<int, Vendor> mVendors;
        public Dictionary<int, Loadout> mLoadouts;
        public Dictionary<int, Loadout> mLoadoutsPreset;
        public Dictionary<int, FieldNote> mFieldNotes;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Action>? mSheetAction;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? mSheetItem;
        public Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.MYCWarResultNotebook>? mSheetMycWarResultNotebook;

        public BBDataManager(Plugin pPlugin) 
        {
            this.mPlugin = pPlugin;
            this.mFragments = new Dictionary<int, Fragment>();
            this.mLostActions = new Dictionary<int, LostAction>();
            this.mFates = new Dictionary<int, Fate>();
            this.mMobs = new Dictionary<int, Mob>();
            this.mVendors = new Dictionary<int, Vendor>();
            this.mLoadouts = new Dictionary<int, Loadout>();
            this.mFieldNotes = new Dictionary<int, FieldNote>();
            this.mLoadoutsPreset = new Dictionary<int, Loadout>();
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
                this.DataSetUpFieldNote(tCommand);
            }

            // json
            LoadoutListJson? tRawLoadouts = this.mPlugin.Configuration.UserLoadouts;
            if (tRawLoadouts != null)
            {
                foreach (LoadoutJson iLoadout in tRawLoadouts.mLoadouts)
                {
                    this.mLoadouts[iLoadout.mId] = new Loadout(this.mPlugin, iLoadout);
                }
            }
            LoadoutListJson? tRawLoadoutsPreset = JsonSerializer.Deserialize<LoadoutListJson>(
                        File.ReadAllText(this.mPlugin.DATA_PATHS["loadout_preset.json"])
                    );
            if (tRawLoadoutsPreset != null)
            {
                foreach (LoadoutJson iLoadout in tRawLoadoutsPreset.mLoadouts)
                {
                    this.mLoadoutsPreset[iLoadout.mId] = new Loadout(this.mPlugin, iLoadout);
                }
            }

            // lumina
            this.mSheetAction = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>();
            this.mSheetItem = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Item>();
            this.mSheetMycWarResultNotebook = this.mPlugin.DataManager.Excel.GetSheet<Lumina.Excel.GeneratedSheets.MYCWarResultNotebook>();

            this.SetUpGeneralObjects();
            this.SetUpAuxiliary();

            // json - UIMap
            List<UIMap_MycItemBoxRow>? tUIMap_MycInfo = JsonSerializer.Deserialize<List<UIMap_MycItemBoxRow>>(
                        File.ReadAllText(this.mPlugin.DATA_PATHS["UIMap_LostAction.json"])
                    );
            if (tUIMap_MycInfo != null) this.SetUpUiMap(tUIMap_MycInfo);
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
        private void DataSetUpFieldNote(SQLiteCommand pCommand)
        {
            DbLoader<FieldNote>(out this.mFieldNotes, pCommand, "SELECT * FROM FieldNote;", (p, t) => new FieldNote(p, t));

            foreach (int id in this.mFieldNotes.Keys)
            {
                string iLinkCol;

                // Linking
                iLinkCol = "idFate";
                pCommand.CommandText = $@"SELECT FieldNoteToFate.{iLinkCol} 
                                            FROM FieldNoteToFate
                                            WHERE FieldNoteToFate.id = {id};";
                SQLiteDataReader tReader = pCommand.ExecuteReader();
                while (tReader.Read())
                {
                    this.mFieldNotes[id].mLinkFates.Add((int)(long)tReader[iLinkCol]);
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
            foreach (int id in this.mFieldNotes.Keys)
                this.mGeneralObjects[this.mFieldNotes[id].GetGenId()] = this.mFieldNotes[id];
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
            foreach (int id in this.mFieldNotes.Keys)
                AuxiliaryViewerSection.BindToGenObj(this.mPlugin, this.mFieldNotes[id].GetGenId());
        }
        public void SetUpUiMap(List<UIMap_MycItemBoxRow> tUIMap_MycInfo)
        {
            unsafe
            {
                if (tUIMap_MycInfo != null)
                {
                    foreach (UIMap_MycItemBoxRow iRow in tUIMap_MycInfo)
                    {
                        for (int i = 0; i < iRow.objId.Count; i++)
                        {
                            this.mLostActions[iRow.objId[i]].mUINode = new(
                                    "MYCItemBox",
                                    new List<int>(new int[] { iRow.head, iRow.head + i + 1 }),
                                    iRow.order,
                                    i + 1
                                );
                        }
                    }
                }
            }
        }
        public LoadoutListJson SerializePseudo_Loadouts()
        {
            LoadoutListJson tLoadoutJsons = new LoadoutListJson();
            foreach (Loadout iLoadout in this.mLoadouts.Values)
            {
                tLoadoutJsons.mLoadouts.Add(new LoadoutJson(iLoadout));
            }
            return tLoadoutJsons;
        }
        public void SaveLoadouts()
        {
            //string tJson = JsonSerializer.Serialize(SerializePseudo_Loadouts(), new JsonSerializerOptions { WriteIndented = true});
            //File.WriteAllText(this.mPlugin.DATA_PATHS["loadout.json"], tJson);
            this.mPlugin.Configuration.UserLoadouts = this.SerializePseudo_Loadouts();
            this.mPlugin.Configuration.Save();
        }
        public void ReloadLoadoutsPreset()
        {
            foreach (Loadout iLoadoutPreset in this.mLoadoutsPreset.Values)
            {
                if (this.mLoadouts.ContainsKey(iLoadoutPreset.mId))
                {
                    this.mLoadouts[iLoadoutPreset.mId] = iLoadoutPreset.DeepCopy();
                    PluginLog.Debug($"Updating existed loadout! ({this.mLoadouts[iLoadoutPreset.mId].mName})");
                }
                else
                {
                    BBDataManager.DynamicAddGeneralObject(this.mPlugin, iLoadoutPreset.DeepCopy(), this.mLoadouts);
                    PluginLog.Debug($"Creating new loadout! ({iLoadoutPreset.mId})");
                }
            }
            this.SaveLoadouts();
        }

        /// <summary>
        /// Does not take care of Section's idList. 
        /// Said section needs to implement RefreshIdList() if they have dynamic idList.
        /// </summary>
        public static void DynamicAddGeneralObject<T>(Plugin pPlugin, T pGenObj, Dictionary<int, T> pDataManagerItemDict) where T : GeneralObject
        {
            // Save to cache
            pDataManagerItemDict[pGenObj.mId] = pGenObj;
            pPlugin.mBBDataManager.mGeneralObjects[pGenObj.GetGenId()] = pGenObj;
            AuxiliaryViewerSection.BindToGenObj(pPlugin, pGenObj.GetGenId());
            // Write to disc
            if (typeof(T) == typeof(Loadout))
            {
                pPlugin.mBBDataManager.SaveLoadouts();
                AuxiliaryViewerSection.mTenpLoadout = null;
            }
            AuxiliaryViewerSection.mIsRefreshRequired = true;
        }
        /// <summary>
        /// Does not take care of Section's idList. 
        /// Said section needs to implement RefreshIdList() if they have dynamic idList.
        /// </summary>
        public static void DynamicRemoveGeneralObject<T>(Plugin pPlugin, T pGenObj, Dictionary<int, T> pDataManagerItemDict) where T : GeneralObject
        {
            // Remove from cache
            pDataManagerItemDict.Remove(pGenObj.mId);
            pPlugin.mBBDataManager.mGeneralObjects.Remove(pGenObj.GetGenId());
            AuxiliaryViewerSection.UnbindFromGenObj(pPlugin, pGenObj.GetGenId());
            AuxiliaryViewerSection.mTabGenIdsToDraw.Remove(pGenObj.GetGenId());
            AuxiliaryViewerSection.mIsRefreshRequired = true;
            // Write to disc
            if (typeof(T) == typeof(Loadout))
            {
                pPlugin.mBBDataManager.SaveLoadouts();
                AuxiliaryViewerSection.mTenpLoadout = null;
            }
        }

        public unsafe static MycDynamicEventData* GetMycDynamicEventArray()
        {
            // Get MycBattleAreaAgent, if possible
            AgentMycBattleAreaInfo* tAgentMycBattleInfoInfo = BBDataManager.GetAgentMycBattleAreaInfo();
            if (tAgentMycBattleInfoInfo == null) { return null; }
            MycDynamicEventData* tMycDynamicEventArray = tAgentMycBattleInfoInfo->MycDynamicEventData;
            return tMycDynamicEventArray;
        }
        public unsafe static AgentMycBattleAreaInfo* GetAgentMycBattleAreaInfo()
        {
            return (AgentMycBattleAreaInfo*)Framework.Instance()
                                                            ->GetUiModule()
                                                            ->GetAgentModule()
                                                            ->GetAgentByInternalId(AgentId.MycBattleAreaInfo);
        }
        public unsafe static AgentMycInfo* GetAgentMycInfo()
        {
            return (AgentMycInfo*)Framework.Instance()
                                                            ->GetUiModule()
                                                            ->GetAgentModule()
                                                            ->GetAgentByInternalId(AgentId.MycInfo);
        }
        public unsafe static AgentMycItemBox* GetAgentMycItemBox()
        {
            return (AgentMycItemBox*)Framework.Instance()
                                                            ->GetUiModule()
                                                            ->GetAgentModule()
                                                            ->GetAgentByInternalId(AgentId.MycItemBox);
        }
        /// <summary>
        /// Update the status of all FateCE in FateTable. 
        /// Returning false means MycBattleAreaAgent was not active.
        /// </summary>
        public unsafe static bool UpdateAllFateStatus(Plugin pPlugin)
        {
            bool tIsAgentActive = true;
            // Get MycBattleAreaAgent, if possible
            MycDynamicEventData* tMycDynamicEventArray = BBDataManager.GetMycDynamicEventArray();
            if (tMycDynamicEventArray == null) tIsAgentActive = false;

            // Check update interval, AFTER checkcing if the agent is active
            if ((DateTime.Now - BBDataManager.kFateTableLastUpdate).TotalSeconds < BBDataManager.kFateTableUpdateInterval)
            {
                return tIsAgentActive;
            }
            else BBDataManager.kFateTableLastUpdate = DateTime.Now;

            Span<MycDynamicEvent> tDeSpan = tIsAgentActive
                                    ? new Span<MycDynamicEvent>(tMycDynamicEventArray->Array, tMycDynamicEventArray->Count)
                                    : Span<MycDynamicEvent>.Empty;

            //PluginLog.LogDebug(String.Format("> IsAgentActive={0}", tIsAgentActive));

            // Reset all mCsFate to null
            foreach (int iID in pPlugin.mBBDataManager.mFates.Keys)
            {
                pPlugin.mBBDataManager.mFates[iID].mCSFate = null;
                // if MycAgentInfo is not opened, all DE is null
                if (!tIsAgentActive)
                {
                    pPlugin.mBBDataManager.mFates[iID].mDynamicEvent = null;
                    continue;
                }
                // mDE
                foreach (MycDynamicEvent iDe in tDeSpan)
                {
                    //PluginLog.LogDebug(String.Format("> DE's id={0} timeLeft={1} participantCount={2} state={3} name={4}", iDe.Id, iDe.TimeLeft, iDe.ParticipantCount, iDe.State.ToString(), iDe.Name.ToString()));
                    if (iID == iDe.Id)
                    {
                        pPlugin.mBBDataManager.mFates[iID].mDynamicEvent = iDe;
                        AlarmManager.NotifyListener("fatece", new MsgAlarmFateCe(pPlugin, iID.ToString(), AlarmFateCe.ExtraCheckOption.None));
                        break;
                    }
                    AlarmManager.RemoveMsg("fatece", new MsgAlarmFateCe(pPlugin, iID.ToString(), AlarmFateCe.ExtraCheckOption.None));
                    pPlugin.mBBDataManager.mFates[iID].mDynamicEvent = null;
                    pPlugin.mBBDataManager.mFates[iDe.Id].mLastActive = DateTime.Now;
                }
            }

            for (int ii = 0; ii < pPlugin.FateTable.Length; ii++)      // if it is null, update if available
            {
                if (pPlugin.FateTable[ii] is null) { continue; }
                // mCsFate
                int tID = pPlugin.FateTable[ii]!.FateId;
                if (!pPlugin.mBBDataManager.mFates.ContainsKey(tID)) continue;
                
                pPlugin.mBBDataManager.mFates[tID].mCSFate = pPlugin.FateTable[ii];
                pPlugin.mBBDataManager.mFates[tID].mLastActive = DateTime.Now;
            }
            return tIsAgentActive;
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
    public enum StatusId
    {
        None = 0,
        HoofingItA = 1778,
        HoofingItB = 1945,
        DutyAsAssigned = 2415
    }

    public class LoadoutListJson
    {
        public List<LoadoutJson> mLoadouts { get; set; } = new List<LoadoutJson>();
    }
    public class LoadoutJson
    {
        public string _mName = "New loadout";          // these public fields are specifically for ImGui.TextInput()
        public string _mDescription = "new description";
        public string _mGroup = "group";
        public RoleFlag _mRole = new RoleFlag(0);
        public int _mWeight = 0;
        public int mId { get; set; } = -1;  // these properties are specifically for json stuff
        public string mName 
        {
            get
            {
                return _mName;
            }
            set
            {
                _mName = value;
            }
        }
        public string mDescription
        {
            get
            {
                return _mDescription;
            }
            set
            {
                _mDescription = value;
            }
        }
        public string mGroup
        {
            get
            {
                return _mGroup;
            }
            set
            {
                _mGroup = value;
            }
        }
        public int mRoleInt
        {
            get
            {
                return RoleFlag.FlagToInt(_mRole.mRoleFlagBit);
            }
            set
            {
                _mRole.SetRoleFlagBit(RoleFlag.IntToFlag(value));
            }
        }
        public int mWeight
        {
            get
            {
                return _mWeight;
            }
            set
            {
                _mWeight = value;
            }
        }
        public Dictionary<int, int> mActionIds { get; set; } = new Dictionary<int, int>();

        public LoadoutJson()
        {
            // Specifically for json stuff
        }
        public LoadoutJson(Loadout pLoadout, bool pIsNew = false)
        {
            this.mId = pIsNew ? -1 : pLoadout.mId;
            this.mName = pLoadout.mName;
            this.mDescription = pLoadout.mDescription;
            this.mGroup = pLoadout.mGroup;
            this.mWeight = pLoadout.mWeight;
            this.mActionIds = new Dictionary<int, int>(pLoadout.mActionIds);
            this.mRoleInt = RoleFlag.FlagToInt(pLoadout.mRole.mRoleFlagBit);
        }
        public void RecalculateWeight(Plugin pPlugin)
        {
            this.mWeight = 0;
            foreach (int iActionId in this.mActionIds.Keys)
            {
                this.mWeight += pPlugin.mBBDataManager.mLostActions[iActionId].mWeight * this.mActionIds[iActionId];
            }
        }
    }
    
    public class UIMap_MycItemBoxRow
    {
        public int order { get; set; }
        public int head { get; set; }
        public List<int> objId { get; set; }
    }
}
