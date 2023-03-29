using Dalamud.Logging;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data.Alarm
{
    public class AlarmTime : Alarm
    {
        public new const string _kJsonid  = "at";
        public new static string kReprString = "Time";
        public new static string kToolTip = "Alarm for a specific time. Can only go off once.";
        public override string _mJsonId { get; set; } = AlarmTime._kJsonid;
        public AlarmTime() : base(null, null, kReprString) { }
        public AlarmTime(DateTime? pTriggerTime, int? pTriggerInt, string? pName = null)
            : base(pTriggerTime, pTriggerInt, pName ?? kReprString)
        {
            this.mIsRevivable = false;
        }
        public override void Init(DateTime? pTriggerTime, int? pTriggerInt, string pName = "", int pDuration = -1, bool pIsAlive = true, bool pIsRevivable = false, string pTriggerString = "", int pOffset = 0)
        {
            base.Init(pTriggerTime, pTriggerInt, pName, pDuration, pIsAlive, false, pTriggerString, pOffset);
        }

        public override bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<MsgAlarm>> pListeners, int pThresholdSeconds = 60, bool pIsReviving = false, Plugin? pPlugin = null)
        {
            PluginLog.LogDebug(String.Format("========== ATime has isAlive={2} | trgTime={0} | isRevivable={1} | isReviving={3} | hasBeenRezzed={6} | mOffset={4} | isInOnceMode={5}",
                                            this.mTriggerTime.HasValue ? this.mTriggerTime.ToString() : "null",
                                            this.mIsRevivable,
                                            this.mIsAlive,
                                            pIsReviving,
                                            this.mOffset,
                                            false,
                                            this.mHasBeenRevived));
            // Flags validation
            if (!this.mIsAlive || !this.mTriggerTime.HasValue)
            {
                PluginLog.LogDebug($"CHECK FAILED: Alarm is dead (isAlive={this.mIsAlive}) or not enough info (mTriggerTime={this.mTriggerTime.HasValue})!");
                PluginLog.LogDebug("ALARM KILLED: Alarm killed by flags validation!");
                this.Kill();
                return false;
            }

            double tDelta = (pCurrTime - this.mTriggerTime!.Value).TotalSeconds;
            // Check if current time has reached, or already past mTriggerTime for an amount of mOffset seconds
            if (Math.Abs(tDelta) > this.mOffset)
            {
                if (tDelta < 0)
                {
                    PluginLog.LogDebug("CHECK FAILED: Time has not reached");
                    return false;
                }
                else if (tDelta > 0)
                {
                    PluginLog.LogDebug("CHECK FAILED: Time has already gone past the threshold");
                    PluginLog.LogDebug("ALARM KILLED: Alarm killed by trgTime!");
                    this.Kill();
                    return false;
                }
            }

            PluginLog.LogDebug($"ALARM KILLED: Alarm killed by final! (tDelta={tDelta} > tOffset={this.mOffset})");
            this.Kill();

            return true;
        }
        public override void Revive(DateTime pCurrTime, Dictionary<string, List<MsgAlarm>> pListeners, Plugin? pPlugin = null)
        {
            this.mIsRevivable = false;  // Time-based alarm should not be revived. Disable the flag just in case.
        }

        public override string Serialize() 
        {
            return "";
        }
    }
}
