using BozjaBuddy.Utils;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BozjaBuddy.Data.Alarm
{
    internal class AlarmFateCe : Alarm
    {
        public new const string _kJsonid = "af";
        public new static string kReprString = "FateCe";
        public new static string kToolTip = "Alarm for a specific Fate or CE. Can go off once, or every time the FATE or CE comes around.";
        private static HashSet<int> _currFateIds = new();
        private static DateTime _cycleOne = DateTime.MinValue;
        public override string _mJsonId { get; set; } = AlarmFateCe._kJsonid;
        public AlarmFateCe() : base(null, null, kReprString) { }
        public AlarmFateCe(DateTime? pTriggerTime, int? pTriggerInt, string? pName = null, string? pTriggerString = null)
            : base(pTriggerTime, pTriggerInt, pName ?? kReprString, 60, true, false, pTriggerString ?? "")
        {
        }

        public override bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<MsgAlarm>> pListeners, int pThresholdSeconds = 1380, bool pIsReviving = false, Plugin? pPlugin = null)
        {
            bool tIsInOnceMode = this.mTriggerTime.HasValue && !this.mIsRevivable;
            //PluginLog.LogDebug(String.Format("========== Alarm has isAlive={2} | trgTime={0} | isRevivable={1} | isReviving={3} | hasBeenRezzed={6} | mOffset={4} | isInOnceMode={5}",
            //                                this.mTriggerTime.HasValue ? this.mTriggerTime.ToString() : "null",
            //                                this.mIsRevivable,
            //                                this.mIsAlive,
            //                                pIsReviving,
            //                                this.mOffset,
            //                                tIsInOnceMode,
            //                                this.mHasBeenRevived));
            // Flags validation
            if (!pIsReviving
                && (!this.mIsAlive
                || !this.mTriggerInt.HasValue))
            {
                //PluginLog.LogDebug($"CHECK FAILED: Alarm is dead (isAlive={this.mIsAlive}) or has no sufficient info (trgInt={this.mTriggerInt.HasValue})!");
                //PluginLog.LogDebug("ALARM KILLED: Alarm killed by flags validation!");
                this.Kill();
                return false;
            }

            // Reloading mTriggerTime for repeat-mode (only after init or after being rezzed)
            if (!tIsInOnceMode && this.mHasBeenRevived)
            {
                this.mHasBeenRevived = false;
            }

            double tDelta = (pCurrTime - this.mTriggerTime!.Value).TotalSeconds;
            // Check msg
            if (!this.CheckMsg(pListeners, tDelta, pIsReviving, pPlugin: pPlugin))
            {
                return false;
            }

            if (pIsReviving)
            {
                //PluginLog.LogDebug($"Revive failed. Either all msg is met, or not out of Offset's range yet (tDelta={tDelta} > tOffset={this.mOffset})");
            }
            else
            {
                //PluginLog.LogDebug($"ALARM KILLED: Alarm killed by final! (tDelta={tDelta} > tOffset={this.mOffset})");
                this.Kill();
            }
            return true;
        }

        private bool CheckMsg(Dictionary<string, List<MsgAlarm>> pListeners, double tDelta, bool pIsReviving = false, Plugin? pPlugin = null)
        {
            if (pPlugin == null)
            {
                //PluginLog.LogDebug("CHECK FAILED: No pPlugin given to perform the check.");
                return false;
            }

            // Check msg for CEs
            MsgAlarmFateCe tReceiverMsg = new(
                pPlugin, 
                this.mTriggerInt.HasValue ? this.mTriggerInt!.Value.ToString() : "0",
                this._getExtraOptions(), 
                pTriggerString: this.mTriggerString);

            if (AlarmManager.CheckMsg("fatece", tReceiverMsg)) return true;

            // Checking for Fate in vanilla way
            foreach (Dalamud.Game.ClientState.Fates.IFate iFate in pPlugin!.FateTable)
            {
                // Check: By Zone
                if (this._getExtraOptions().HasFlag(ExtraCheckOption.ByZone))
                {
                    if (this.mTriggerString == null) continue;
                    if (!(pPlugin.mBBDataManager.mFates.ContainsKey(iFate.FateId)
                        && pPlugin.mBBDataManager.mFates[iFate.FateId].mLocation != null
                        && UtilsGameData.kAreaAndCode[this.mTriggerString] != Location.Area.None
                        && UtilsGameData.kAreaAndCode[this.mTriggerString] == pPlugin.mBBDataManager.mFates[iFate.FateId].mLocation!.mAreaFlag))
                    {
                        continue;
                    }
                }
                // Check: Only Fate
                if (this._getExtraOptions().HasFlag(AlarmFateCe.ExtraCheckOption.AllFateCe))
                {
                    return true;
                }
                // Check: Only Fate
                else if (this._getExtraOptions().HasFlag(AlarmFateCe.ExtraCheckOption.OnlyFate)
                    && !AlarmFateCe._currFateIds.Contains(iFate.FateId))
                {
                    if ((DateTime.Now - AlarmFateCe._cycleOne).TotalSeconds > 5)
                    {
                        AlarmFateCe.UpdateCurrFateIds(pPlugin);
                        AlarmFateCe._cycleOne = DateTime.Now;
                    }
                    return true;
                }

                // Vanilla check
                if (this._getExtraOptions() == ExtraCheckOption.None
                    && this.mTriggerInt == iFate.FateId)
                {
                    return true;
                }
            }
            return false;
        }
        public override void AddExtraOption(int pOption)
        {
            this._addExtraOptions((ExtraCheckOption)pOption);
        }
        public override void RemoveExtraOption(int pOption)
        {
            this._extraOptions = (int)((ExtraCheckOption)this._extraOptions & ~(ExtraCheckOption)pOption);
        }
        public override int GetExtraOptions()
        {
            return base.GetExtraOptions();
        }
        public override bool CheckExtraOption(int pOption)
        {
            return ((ExtraCheckOption)this._extraOptions).HasFlag((ExtraCheckOption)pOption);
        }
        protected ExtraCheckOption _getExtraOptions() => (ExtraCheckOption)this.GetExtraOptions();
        protected bool _checkExtraOption(ExtraCheckOption pOption) => ((ExtraCheckOption)this._extraOptions).HasFlag(pOption);
        protected void _addExtraOptions(ExtraCheckOption pOption) => this._extraOptions = (int)((ExtraCheckOption)this._extraOptions | pOption);
        protected void _removeExtraOptions(ExtraCheckOption pOption) => this._extraOptions = (int)((ExtraCheckOption)this._extraOptions & ~(ExtraCheckOption)pOption);

        public override string Serialize()
        {
            return "";
        }

        private static void UpdateCurrFateIds(Plugin pPlugin)
        {
            AlarmFateCe._currFateIds = new HashSet<int>(pPlugin!.FateTable.Select(o => (int)o.FateId).ToList());
        }

        [Flags]
        public enum ExtraCheckOption
        {
            None = 0,
            OnlyCe = 1,
            OnlyFate = 2,
            AllFateCe = 4,
            ByZone = 8
        }
    }
}
