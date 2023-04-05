using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Data.Alarm
{
    internal class AlarmFateCe : Alarm
    {
        public new const string _kJsonid = "af";
        public new static string kReprString = "FateCe";
        public new static string kToolTip = "Alarm for a specific Fate or CE. Can go off once, or every time the FATE or CE comes around.";
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
            foreach (Dalamud.Game.ClientState.Fates.Fate iFate in pPlugin!.FateTable)
            {
                if (this.mTriggerInt == iFate.FateId)
                {
                    //PluginLog.LogDebug(String.Format("CHECK SUCCEEDED: Alarm's msgAlarm check is true! (trgInt={0} FateId={1})",
                    //                this.mTriggerInt,
                    //                iFate.FateId
                    //            ));
                    return true;
                }
            }
            //PluginLog.LogDebug(String.Format("CHECK FAILED: No msg is true. (trgInt={0}) ({1})",
            //                                this.mTriggerInt,
            //                                String.Join(", ", pPlugin!.FateTable.Select(o => o.FateId))
            //                                ));
            return false;
        }

        public override string Serialize()
        {
            return "";
        }
    }
}
